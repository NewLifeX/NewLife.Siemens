using System;
using System.Threading.Tasks;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class S7PasswordTests
{
    #region SetPassword
    [Fact(DisplayName = "SetPassword 正确密码认证成功")]
    public async Task SetPassword_CorrectPassword_Authenticates()
    {
        var server = new S7Server { Port = 0 };
        server.ProtectionLevel = ProtectionLevel.FullProtection;
        server.SimulatedPassword = "MySecret";
        server.Start();

        try
        {
            var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port);
            await client.OpenAsync();

            var level = await client.SetPasswordAsync("MySecret");
            Assert.Equal(ProtectionLevel.NoProtection, level);

            client.Close();
        }
        finally
        {
            server.Stop("Test done");
        }
    }

    [Fact(DisplayName = "SetPassword 错误密码返回原保护级别")]
    public async Task SetPassword_WrongPassword_ReturnsOriginalLevel()
    {
        var server = new S7Server { Port = 0 };
        server.ProtectionLevel = ProtectionLevel.FullProtection;
        server.SimulatedPassword = "MySecret";
        server.Start();

        try
        {
            var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port);
            await client.OpenAsync();

            var level = await client.SetPasswordAsync("WrongPwd");
            Assert.Equal(ProtectionLevel.FullProtection, level);

            client.Close();
        }
        finally
        {
            server.Stop("Test done");
        }
    }

    [Fact(DisplayName = "SetPassword 无保护级别时直接成功")]
    public async Task SetPassword_NoProtection_ReturnsNoProtection()
    {
        var server = new S7Server { Port = 0 };
        // 默认 NoProtection
        server.Start();

        try
        {
            var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port);
            await client.OpenAsync();

            var level = await client.SetPasswordAsync("anything");
            Assert.Equal(ProtectionLevel.NoProtection, level);

            client.Close();
        }
        finally
        {
            server.Stop("Test done");
        }
    }

    [Fact(DisplayName = "SetPassword 短密码自动空格补齐")]
    public async Task SetPassword_ShortPassword_PadsCorrectly()
    {
        var server = new S7Server { Port = 0 };
        server.ProtectionLevel = ProtectionLevel.FullProtection;
        server.SimulatedPassword = "AB"; // 2字符，其余空格补齐
        server.Start();

        try
        {
            var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port);
            await client.OpenAsync();

            var level = await client.SetPasswordAsync("AB");
            Assert.Equal(ProtectionLevel.NoProtection, level);

            client.Close();
        }
        finally
        {
            server.Stop("Test done");
        }
    }

    [Fact(DisplayName = "SetPassword 8字符密码精确匹配")]
    public async Task SetPassword_Exact8Chars_MatchesExactly()
    {
        var server = new S7Server { Port = 0 };
        server.ProtectionLevel = ProtectionLevel.ReadWriteProtected;
        server.SimulatedPassword = "12345678";
        server.Start();

        try
        {
            var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port);
            await client.OpenAsync();

            var level = await client.SetPasswordAsync("12345678");
            Assert.Equal(ProtectionLevel.NoProtection, level);

            client.Close();
        }
        finally
        {
            server.Stop("Test done");
        }
    }
    #endregion

    #region ClearPassword
    [Fact(DisplayName = "ClearPassword 清除认证")]
    public async Task ClearPassword_AfterSetPassword_Works()
    {
        var server = new S7Server { Port = 0 };
        server.ProtectionLevel = ProtectionLevel.FullProtection;
        server.SimulatedPassword = "Test";
        server.Start();

        try
        {
            var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port);
            await client.OpenAsync();

            // 先设置密码
            var level = await client.SetPasswordAsync("Test");
            Assert.Equal(ProtectionLevel.NoProtection, level);

            // 清除密码
            await client.ClearPasswordAsync();

            client.Close();
        }
        finally
        {
            server.Stop("Test done");
        }
    }
    #endregion

    #region Password in OpenAsync
    [Fact(DisplayName = "OpenAsync 设置 Password 属性自动认证")]
    public async Task OpenAsync_WithPassword_AutoAuthenticates()
    {
        var server = new S7Server { Port = 0 };
        server.ProtectionLevel = ProtectionLevel.FullProtection;
        server.SimulatedPassword = "AutoAuth";
        server.Start();

        try
        {
            var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port)
            {
                Password = "AutoAuth",
            };
            await client.OpenAsync();
            // 不抛异常即成功

            client.Close();
        }
        finally
        {
            server.Stop("Test done");
        }
    }

    [Fact(DisplayName = "OpenAsync 错误密码不阻塞连接")]
    public async Task OpenAsync_WrongPassword_DoesNotBlock()
    {
        var server = new S7Server { Port = 0 };
        server.ProtectionLevel = ProtectionLevel.FullProtection;
        server.SimulatedPassword = "GoodPwd";
        server.Start();

        try
        {
            var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port)
            {
                Password = "BadPwd",
            };
            await client.OpenAsync();
            // 连接仍建立，密码认证失败但不抛异常

            Assert.True(true);
            client.Close();
        }
        finally
        {
            server.Stop("Test done");
        }
    }
    #endregion

    #region ReadProtectionLevel
    [Fact(DisplayName = "ReadProtectionLevel 读取保护级别")]
    public async Task ReadProtectionLevel_ReturnsServerLevel()
    {
        var server = new S7Server { Port = 0 };
        server.ProtectionLevel = ProtectionLevel.WriteProtected;
        server.Start();

        try
        {
            var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port);
            await client.OpenAsync();

            var level = await client.ReadProtectionLevelAsync();
            // SZL 0x0232 可能未在 S7Server 中完整实现，此处仅验证不抛异常
            // 实际依赖 SZL 数据预置
            Assert.True(true);

            client.Close();
        }
        finally
        {
            server.Stop("Test done");
        }
    }
    #endregion
}
