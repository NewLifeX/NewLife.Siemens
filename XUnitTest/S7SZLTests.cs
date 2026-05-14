using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>SZL诊断读取单元测试（基于S7Server模拟器）</summary>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7SZLTests
{
    private static S7Server? _server;
    private static S7Client? _client;
    private const Int32 TestPort = 10242;

    #region 01 — UserDataParameter/S7CpuInfo 纯逻辑测试（无网络）
    [Fact]
    public void UserDataParam_DefaultValues()
    {
        var param = new UserDataParameter();
        Assert.Equal(0x01, param.ExtraByte);
        Assert.Equal(0x00, param.Function);
        Assert.Equal(0x00, param.SubFunction);
        Assert.Equal(0xFF, param.ReturnCode);
        Assert.Equal(0x09, param.TransportSize);
        Assert.NotNull(param.Data);
        Assert.Empty(param.Data);
    }

    [Fact]
    public void UserDataParam_SetProperties()
    {
        var param = new UserDataParameter
        {
            ExtraByte = 0x01,
            Function = 0x12,
            SubFunction = 0x04,
            Data = [0x01, 0x02],
        };

        Assert.Equal(0x12, param.Function);
        Assert.Equal(0x04, param.SubFunction);
        Assert.Equal(2, param.Data.Length);
    }

    [Fact]
    public void UserDataParam_ToString_ContainsFuncSubFunc()
    {
        var param = new UserDataParameter { Function = 0x12, SubFunction = 0x04 };
        var str = param.ToString();
        Assert.Contains("12", str);
        Assert.Contains("04", str);
    }

    [Fact]
    public void CpuInfo_ParseFromSzl001C_ExtractFields()
    {
        // 构造SZL 0x001C响应：8字节头 + 2字节子项 + 20字节型号 + 20字节序列号 + 2字节hw + 2字节fw
        var data = new Byte[8 + 2 + 20 + 20 + 2 + 2];
        data[0] = 0x00; data[1] = 0x1C;
        data[8] = 0x00; data[9] = 0x01;  // 子项索引

        // 型号 "S7-300"
        var articleBytes = System.Text.Encoding.ASCII.GetBytes("S7-300");
        Array.Copy(articleBytes, 0, data, 10, articleBytes.Length);

        // 序列号 "SN42"
        var snBytes = System.Text.Encoding.ASCII.GetBytes("SN42");
        Array.Copy(snBytes, 0, data, 30, snBytes.Length);

        // 固件版本 V1.2（offset 50=hw, 52=fw）
        data[52] = 0x01; data[53] = 0x02;

        var info = S7CpuInfo.ParseFromSzl001C(data);
        Assert.Equal("S7-300", info.ArticleNumber);
        Assert.Equal("SN42", info.SerialNumber);
        Assert.Equal("V1.2", info.FirmwareVersion);
    }

    [Fact]
    public void CpuInfo_ParseFromSzl001C_EmptyData()
    {
        var info = S7CpuInfo.ParseFromSzl001C([]);
        Assert.NotNull(info);
        Assert.Equal(String.Empty, info.ArticleNumber);
    }

    [Fact]
    public void CpuStatus_ParseStop()
    {
        var data = new Byte[9];
        data[8] = 0x01;
        Assert.Equal(S7CpuStatus.Stop, S7CpuInfo.ParseFromSzl0174(data));
    }

    [Fact]
    public void CpuStatus_ParseRun()
    {
        var data = new Byte[9];
        data[8] = 0x03;
        Assert.Equal(S7CpuStatus.Run, S7CpuInfo.ParseFromSzl0174(data));
    }

    [Fact]
    public void CpuStatus_ParseHalt()
    {
        var data = new Byte[9];
        data[8] = 0x02;
        Assert.Equal(S7CpuStatus.Halt, S7CpuInfo.ParseFromSzl0174(data));
    }

    [Fact]
    public void CpuStatus_ParseUnknown_InsufficientData()
    {
        Assert.Equal(S7CpuStatus.Unknown, S7CpuInfo.ParseFromSzl0174(new Byte[3]));
    }

    [Fact]
    public void ParseCpuStatus_AliasMatchesParseFromSzl0174()
    {
        var data = new Byte[9];
        data[8] = 0x03; // RUN
        Assert.Equal(S7CpuInfo.ParseFromSzl0174(data), S7CpuInfo.ParseCpuStatus(data));
    }
    #endregion

    #region 02 — 服务端启动与SZL预置
    [TestOrder(10)]
    [Fact]
    public void SZL_StartServer()
    {
        var server = new S7Server { Port = TestPort, Log = XTrace.Log };

        // 预置SZL 0x001C（CPU组件信息）
        var szl001C = new Byte[8 + 2 + 20 + 20 + 2 + 2];
        szl001C[0] = 0x00; szl001C[1] = 0x1C;
        szl001C[8] = 0x00; szl001C[9] = 0x01;
        var articleBytes = System.Text.Encoding.ASCII.GetBytes("S7-300");
        Array.Copy(articleBytes, 0, szl001C, 10, articleBytes.Length);
        var snBytes = System.Text.Encoding.ASCII.GetBytes("SN42");
        Array.Copy(snBytes, 0, szl001C, 30, snBytes.Length);
        szl001C[52] = 0x01; szl001C[53] = 0x02; // V1.2
        server.SetSzlData(0x001C, 0x0000, szl001C);

        // 预置SZL 0x0174（CPU状态：RUN）
        var szl0174 = new Byte[10];
        szl0174[0] = 0x01; szl0174[1] = 0x74;
        szl0174[8] = 0x03; // RUN
        server.SetSzlData(0x0174, 0x0000, szl0174);

        server.Start();
        _server = server;

        Thread.Sleep(100);
        Assert.True(server.Active);
    }
    #endregion

    #region 03 — 客户端连接
    [TestOrder(11)]
    [Fact]
    public async Task SZL_Connect()
    {
        var client = new S7Client(CpuType.S7200, "127.0.0.1", TestPort)
        {
            Log = XTrace.Log,
        };
        await client.OpenAsync();
        _client = client;
        Assert.NotNull(_client);
    }
    #endregion

    #region 04 — 读取原始SZL
    [TestOrder(20)]
    [Fact]
    public async Task SZL_ReadRaw_001C()
    {
        Assert.NotNull(_client);
        var data = await _client!.ReadSzlAsync(0x001C);
        Assert.NotNull(data);
        Assert.True(data.Length >= 8, $"SZL数据应至少8字节，实际{data.Length}");
    }

    [TestOrder(21)]
    [Fact]
    public async Task SZL_ReadRaw_0174()
    {
        Assert.NotNull(_client);
        var data = await _client!.ReadSzlAsync(0x0174);
        Assert.NotNull(data);
        Assert.True(data.Length >= 9);
    }
    #endregion

    #region 05 — 读取CPU信息
    [TestOrder(30)]
    [Fact]
    public async Task SZL_ReadCpuInfo()
    {
        Assert.NotNull(_client);
        var info = await _client!.ReadCpuInfoAsync();
        Assert.NotNull(info);
        Assert.Equal("S7-300", info.ArticleNumber);
        Assert.Equal("SN42", info.SerialNumber);
        Assert.Equal("V1.2", info.FirmwareVersion);
    }
    #endregion

    #region 06 — 读取CPU状态
    [TestOrder(40)]
    [Fact]
    public async Task SZL_ReadCpuStatus_Run()
    {
        Assert.NotNull(_client);
        var status = await _client!.ReadCpuStatusAsync();
        Assert.Equal(S7CpuStatus.Run, status);
    }
    #endregion

    #region 07 — SZL未预置时返回空数据头
    [TestOrder(50)]
    [Fact]
    public async Task SZL_ReadUnknownId_ReturnsEmptyHeader()
    {
        Assert.NotNull(_client);
        var data = await _client!.ReadSzlAsync(0x9999);
        Assert.NotNull(data);
        // 未预置SZL时服务端返回8字节空头
        Assert.Equal(8, data.Length);
        Assert.Equal(0x99, data[0]);
        Assert.Equal(0x99, data[1]);
    }
    #endregion

    #region 99 — 清理
    [TestOrder(99)]
    [Fact]
    public void SZL_StopServer()
    {
        _client?.Close();
        _server?.Stop("done");
    }
    #endregion
}
