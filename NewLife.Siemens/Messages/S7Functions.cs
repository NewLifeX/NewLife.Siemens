namespace NewLife.Siemens.Messages;

/// <summary>S7参数类型</summary>
public enum S7Functions : Byte
{
    /// <summary>设置通信</summary>
    Setup = 0xF0,

    /// <summary>读取变量</summary>
    ReadVar = 0x04,

    /// <summary>写入变量</summary>
    WriteVar = 0x05,

    /// <summary>PLC启动 / PI服务调用（HotRestart/ColdRestart）</summary>
    PlcStart = 0x28,

    /// <summary>PLC停机</summary>
    PlcStop = 0x29,

    /// <summary>块上传启动</summary>
    StartUpload = 0x1D,

    /// <summary>块上传数据帧</summary>
    Upload = 0x1E,

    /// <summary>块上传结束</summary>
    EndUpload = 0x1F,

    /// <summary>块下载启动</summary>
    StartDownload = 0x1A,

    /// <summary>块下载数据帧</summary>
    Download = 0x1B,

    /// <summary>块下载结束</summary>
    EndDownload = 0x1C,
}
