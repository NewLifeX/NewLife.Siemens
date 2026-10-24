# NewLife.Siemens 西门子PLC

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Siemens?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Siemens?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Siemens?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Siemens?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Siemens?label=dev%20nuget&logo=nuget)

NewLife.Siemens 是基于 NewLife.IoT 标准驱动接口实现的西门子 PLC S7 协议 .NET 库，纯托管代码、无原生依赖，支持 S7-200/300/400/1200/1500 全系列 PLC 的以太网数据采集与控制。  

源码： https://github.com/NewLifeX/NewLife.Siemens  
Nuget：NewLife.Siemens  

---

## 项目介绍

西门子 S7 协议是工业自动化领域最广泛使用的 PLC 以太网通信协议，基于 ISO-on-TCP（端口 102），采用 TPKT → COTP → S7 三层协议栈。NewLife.Siemens 完整实现了该协议栈，提供简洁的 C# API，可直接读写 PLC 的 DB、输入、输出、标志区等各类数据区域。

本库同时作为 [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) 标准驱动，可无缝集成到 IoTEdge 工业物联网网关，实现配置化点位采集，无需编写额外代码。

## 功能实现状态

| 功能模块 | 状态 | 说明 |
|---------|------|------|
| 连接管理（TPKT/COTP/S7 三层握手） | ✅ 已完成 | 含断线自动重连、KeepAlive |
| 字节级读写（ReadBytes/WriteBytes） | ✅ 已完成 | 超 PDU 自动分段 |
| PLC 地址解析（DB/I/Q/M/T/C/STRING） | ✅ 已完成 | 全格式字符串解析 |
| 数据类型模型（VarType/DataType/CpuType） | ✅ 已完成 | 全系列 PLC 封装 |
| NewLife.IoT 驱动集成（SiemensS7Driver） | ✅ 已完成 | 支持 IoTEdge 插件化加载 |
| 错误处理体系（ErrorCode/ApiException） | ✅ 已完成 | 分类异常，异常后自动重连 |
| S7Server PLC 模拟器 | ✅ 已完成 | 含全区域内存存储，支持读写往返 |
| 类型化读写 API（Read\<T\>/Write） | ✅ 已完成 | 自动字节序转换，支持数组与字符串 |
| 批量多变量读写（ReadMultipleVars） | ✅ 已完成 | 单次 PDU 打包多个变量，自动分批 |
| SZL 诊断读取（CPU 状态、模块信息） | ✅ 已完成 | ReadSzlAsync / ReadCpuInfoAsync |
| PLC 时钟读写 | ✅ 已完成 | ReadClockAsync / WriteClockAsync |
| GTM 全局类型消息支持（S7-1200/1500） | ✅ 已完成 | 全局类型消息（GTM）读写 |
| 密码保护连接与认证 | ✅ 已完成 | ProtectionLevel 模型，S7Client/S7Server 均支持 |

## 主要特点

- **完整协议栈**：TPKT / COTP / S7 三层协议完整实现，含 CR/CC 握手与 PDU 大小协商
- **全系列 PLC 支持**：S7-200、S7-200 Smart、S7-300、S7-400、S7-1200、S7-1500、Siemens Logo 0BA8
- **全地址格式**：DB（DBB/DBW/DBD/DBX/STRING）、输入（I/E）、输出（Q/A）、标志区（M）、定时器（T）、计数器（C）
- **超 PDU 自动分段**：读写数据超出 PDU 限制时自动拆分为多次请求，对调用方透明
- **断线自动重连**：内置 TCP KeepAlive 检测，操作时自动重建连接
- **全异步 API**：`OpenAsync` 等异步方法，适合高并发 IoT 采集场景
- **多框架支持**：net45 / net461 / netstandard2.0 / netstandard2.1，覆盖老旧工控系统
- **IoT 驱动集成**：`SiemensS7Driver` 实现 `DriverBase`，可直接加载到 IoTEdge 网关

## 支持的 PLC 型号与地址格式

### CPU 型号

| 枚举值 | 对应型号 | Rack | Slot |
|--------|---------|------|------|
| S7200 | SIMATIC S7-200（需 CP243 模块） | 0 | 0 |
| S7200Smart | SIMATIC S7-200 Smart | 0 | 0 |
| S7300 | SIMATIC S7-300 | 0 | 2 |
| S7400 | SIMATIC S7-400 | 0 | 2 |
| S71200 | SIMATIC S7-1200 | 0 | 0 |
| S71500 | SIMATIC S7-1500 | 0 | 0 |
| Logo0BA8 | Siemens LOGO! 0BA8 | 0 | 0 |

> **S7-1200/1500 注意**：需在 TIA Portal 中关闭 DB "优化块访问"，并在 CPU 保护设置中开启"允许 PUT/GET"。

### 地址格式

| 格式示例 | 说明 |
|---------|------|
| `DB1.DBB0` | DB1 第0字节（Byte） |
| `DB1.DBW10` | DB1 第10字节起的 Word（2字节） |
| `DB1.DBD20` | DB1 第20字节起的 DWord（4字节） |
| `DB1.DBX0.3` | DB1 第0字节第3位（Bit） |
| `DB1.STRING0.60` | DB1 第0字节起的字符串（最大60字符） |
| `IB0` / `IW2` / `ID4` | 输入字节/字/双字 |
| `QB0` / `QW2` / `QD4` | 输出字节/字/双字 |
| `MB0` / `MW4` / `MD8` | 标志区字节/字/双字 |
| `M0.5` | 标志区第0字节第5位 |
| `T10` | 定时器 T10 |
| `C5` | 计数器 C5 |

## 快速入门

### 安装

```shell
dotnet add package NewLife.Siemens
```

### 直接使用 S7Client

```csharp
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;

// 创建客户端（CPU类型、IP地址、端口、机架、插槽）
var client = new S7Client(CpuType.S71200, "192.168.1.100", 102, rack: 0, slot: 0);

// 建立连接（完成 TPKT/COTP/S7 三层握手）
await client.OpenAsync();

// 读取字节数据（DB1 第0字节起读10个字节）
var address = new PLCAddress("DB1.DBB0");
var data = client.ReadBytes(address, 10);
Console.WriteLine(data.ToHex());

// 读取一个 Word（2字节，DB1.DBW20）
var wordAddr = new PLCAddress("DB1.DBW20");
var wordData = client.ReadBytes(wordAddr, 2);
var wordValue = (ushort)((wordData[0] << 8) | wordData[1]);  // S7大端转换

// 写入字节数据
var writeAddr = new PLCAddress("DB1.DBW20");
client.WriteBytes(writeAddr, new byte[] { 0x00, 0x64 });  // 写入100（大端）

// 读取标志区（M0.0 起读1字节）
var mAddr = new PLCAddress("MB0");
var flags = client.ReadBytes(mAddr, 1);

// 关闭连接
client.Close();
```

### 通过 IoT 驱动（IoTEdge 集成）

```csharp
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Siemens.Drivers;
using NewLife.Siemens.Models;

// 实例化驱动
var driver = new SiemensS7Driver();

// 配置参数
var param = new SiemensParameter
{
    Address = "192.168.1.100:102",
    CpuType = CpuType.S71200,
    Rack = 0,
    Slot = 0
};

// 打开节点
var node = driver.Open(device, param);

// 定义点位并读取
var points = new IPoint[]
{
    new PointModel { Name = "温度", Address = "DB1.DBW0" },
    new PointModel { Name = "压力", Address = "DB1.DBD4" },
    new PointModel { Name = "运行标志", Address = "M0.0" },
};

var result = driver.Read(node, points);
Console.WriteLine($"温度={result["温度"]}, 压力={result["压力"]}");
```

### 类型化读写 API

```csharp
// 泛型读取（自动大端→小端转换）
var temp = client.Read<Int16>("DB1.DBW0");          // 读 INT
var pressure = client.Read<Single>("DB1.DBD4");     // 读 REAL
var flag = client.Read<Boolean>("M0.0");            // 读 BOOL 位

// 读字符串（STRING 含2字节头）
var name = client.ReadString("DB1.STRING0.60");

// 泛型写入
client.Write("DB1.DBW0", (Int16)150);
client.Write("M0.0", true);

// 读取数组（连续变量）
var temps = client.ReadArray<Int16>("DB1.DBW0", 10);  // 读10个 INT
```

### 批量多变量读写

```csharp
using NewLife.Siemens.Messages;

// 构造多个变量描述
var items = new List<DataItem>
{
    DataItem.FromAddress("DB1.DBW0",  VarType.Int),
    DataItem.FromAddress("DB1.DBD4",  VarType.Real),
    DataItem.FromAddress("M0.0",      VarType.Bit),
};

// 一次 PDU 批量读取（超限自动拆分）
client.ReadMultipleVars(items);
Console.WriteLine($"温度={items[0].Value}, 压力={items[1].Value}, 运行={items[2].Value}");

// 批量写入
items[0].Value = (Int16)200;
client.Write(items.ToArray());
```

### SZL 诊断读取与时钟读写

```csharp
// 读取 CPU 信息
var info = await client.ReadCpuInfoAsync();
Console.WriteLine($"型号={info.ArticleNumber}, 序列号={info.SerialNumber}");

// 读取 CPU 运行状态（Run / Stop / Halt）
var status = await client.ReadCpuStatusAsync();
Console.WriteLine($"CPU状态={status}");

// 读取 PLC 时钟
var plcTime = await client.ReadClockAsync();
Console.WriteLine($"PLC时间={plcTime:yyyy-MM-dd HH:mm:ss}");

// 同步 PLC 时钟为当前系统时间
await client.WriteClockAsync(DateTime.Now);
```

### PLC 控制（安全开关）

```csharp
client.AllowPlcControl = true; // 必须显式开启

await client.PlcStopAsync();        // 停机
await client.PlcHotRestartAsync();  // 热启动
await client.PlcColdStartAsync();   // 冷启动
```

### 结构体映射

```csharp
// 定义与 TIA Portal DB 布局一致的结构体
struct MotorData
{
    public Boolean Running;
    public Single Speed;
    public Int16 Temperature;
}

// 一行代码读回
var motor = client.ReadStruct<MotorData>("DB1");

// 一行代码写入
client.WriteStruct("DB1", motor);
```

### 内置 PLC 模拟器（用于测试）

```csharp
var server = new S7Server { Port = 0 };
server.Start();

// 预置测试数据
server.SetValue("DB1.DBW0", (Int16)12345);
server.SetValue("M0.0", true);

var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port);
await client.OpenAsync();

var value = client.Read<Int16>("DB1.DBW0"); // 12345
```

---

## 性能基准

本地运行基准测试：

```shell
dotnet run -c Release --project BenchmarkTest
```

基准套件覆盖：

- **PLCAddress 解析** — 地址字符串解析吞吐
- **S7Message 序列化** — 消息构建/解析速度（含 GTM）
- **类型化 Read&lt;T&gt;/Write** — 配合 S7Server 的端到端类型转换

## 与同类库对比

| 特性 | NewLife.Siemens | S7netplus | Sharp7 | HslCommunication |
|---------|:---:|:---:|:---:|:---:|
| PLC 系列覆盖 | ✅ 全部 | ✅ 全部 | ⚠️ 有限 | ✅ 全部 |
| 自动重连 | ✅ | ❌ | ❌ | ✅ |
| 全异步 API | ✅ | ⚠️ 部分 | ❌ | ✅ |
| IoT 驱动接口 | ✅ | ❌ | ❌ | ❌ |
| 内置 PLC 模拟器 | ✅ | ❌ | ❌ | ❌ |
| ReadStruct&lt;T&gt; | ✅ | ❌ | ❌ | ✅ |
| GTM 支持 | ✅ | ✅ | ❌ | ✅ |
| SZL 诊断 | ✅ | ❌ | ❌ | ❌ |
| PLC 时钟读写 | ✅ | ❌ | ❌ | ✅ |
| 程序块上传/下载 | ✅ | ❌ | ❌ | ❌ |
| 许可证 | MIT | MIT | MIT | 商业授权 |
| 多目标框架 | 4 | 2 | 2 | 7+ |
| 包体积 | 427 KB | 372 KB | 64 KB | 9.5 MB |

> 详细竞品分析见 `Doc/竞品分析报告.md`。

---

## 架构

```
┌─────────────────────────────────────────────────────┐
│              Layer 5: IoT Driver                    │
│  SiemensS7Driver / SiemensParameter / SiemensNode   │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│              Layer 4: S7 Client                     │
│  S7Client (OpenAsync / ReadBytes / WriteBytes)      │
└──────┬───────────────────────┬──────────────────────┘
       │                       │
┌──────▼──────┐   ┌────────────▼──────────────────────┐
│ Layer 3:    │   │       Layer 2: COTP               │
│ S7Message   │   │  COTP (CR/CC/DT frames)           │
│ SetupMessage│   │  TsapAddress                      │
│ ReadRequest │   └────────────┬──────────────────────┘
│ WriteRequest│                │
└─────────────┘    ┌───────────▼──────────────────────┐
                   │       Layer 1: TPKT              │
                   │  TPKT (ISO-on-TCP 4-byte header) │
                   │  TPKTCodec (stream framing)      │
                   └───────────┬──────────────────────┘
                               │
                      TCP Socket (port 102)
```

---

## 生态版图

NewLife 生态按依赖分层，各项目独立版本、独立发版。下面的版图在所有 NewLife 仓库中逐字一致；每个项目链接到代码仓库与主 NuGet 包。

| 项目 | 一句话定位 | 下载量 |
| --- | --- | --- |
| **基础底座** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | 日志、配置、缓存、网络、序列化、定时调度与 APM 追踪。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 数据中间件：单表百亿级、自动分表、读写分离，支持 10 多种数据库。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 高性能网络库：单机 177 万 pps、14.5 Gbps（TCP 1KB，2026-10 实测），400 万 Tcp 长连接。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 协议通信与 RPC 框架，面向 CS 应用与物联网设备。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 把 .NET 应用安装为操作系统守护进程：Windows 服务与 Linux systemd。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` 项目模板：控制台、Web、WebApi、服务、数据与物联网。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **数据存储与消息** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 客户端：微秒级延迟、百万级吞吐，内置消息队列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 纯托管 RocketMQ 客户端，支持 Apache RocketMQ 与阿里云消息队列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 客户端与服务端，客户端支持阿里云物联网。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 混合数据库：嵌入/网络双模，关系型、时序、消息队列与 KV 于一体。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql 驱动与数据访问辅助层，基于 NewLife 数据栈实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web、AI 与分布式** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 魔方快速开发平台：用户权限、SSO、OAuth 服务端与 CRUD 脚手架。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 星尘分布式服务平台：节点管理、APM、配置中心、注册中心与发布中心。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | 蚂蚁调度：分布式任务与实时数据平台，支持分片、重试与回溯补算。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 统一接入 46 家大模型服务商，附带开箱即用的 ChatAI Web 对话应用。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **工业与物联网** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT 标准库，定义物联网通信协议契约与设备抽象。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 西门子 PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | 欧姆龙 PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | 三菱 Melsec PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC 客户端，用于工业数据采集。 | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet 楼宇自控协议栈。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | 开源轻量级 IoT 平台，含网关与设备管理。 | — |
| **车联网** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 车辆定位协议：70+ 消息体，覆盖 2011/2013/2019 三个版本。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 部标平台互联协议，面向运输管理部门。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 车载视频：RTP 解析、FLV/fMP4 转封装与直播推送。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **领域组件** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | 一个包覆盖 Excel、Word、PPT、PDF 等 15+ 文档格式。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 跨平台音频：编解码、设备 IO、DSP、容器与流式处理。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | 视频编解码、封装格式、帧处理与流媒体。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 百度/高德/腾讯/天地图统一接口，支持地理编码与坐标转换。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 归属地：离线内存映射库 + 在线 API 双源。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **工具与产品** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 码神工具：网络、串口、加解密、正则、Modbus 与 MQTT。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 自托管 Git 源代码管理系统。 | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 从低版本宿主启动时，自动安装匹配的 .NET 运行时。 | — |

> 徽章取各项目主包；主题包、扩展包与卫星包见对应 NuGet 页面。未在表中列出的 30 多个包（FTP、LoRa、Office 扩展、各类协议驱动等）见 [NewLifeX 组织](https://github.com/NewLifeX) 与 [newlifex.com](https://newlifex.com)。



## 新生命开发团队

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

新生命团队成立于 2002 年，二十余年专注 .NET 与物联网技术生态，主导开源项目近百个（生态版图收录 34 个主干项目），为电力、物流、医疗、工控、互联网等行业提供软硬件一体化解决方案；以“学无先后达者为师”为信条。

开源组件在 NuGet 累计下载 <!--nuget-total:start-->6.48M<!--nuget-total:end--> 次（全生态口径，截至 2026-10）。

网站：https://newlifex.com · 开源组织：https://github.com/NewLifeX

<details>
<summary>微信公众号：智能大石头（扫码关注）</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## 许可证

MIT — 可自由用于商业闭源应用。

## 社区

- 网站：https://newlifex.com
- GitHub：https://github.com/NewLifeX
- NuGet：https://www.nuget.org/profiles/NewLifeX
- QQ 群：1600800 / 1600838

---

*新生命开发团队 — 自 2002 年构建物联网基础设施*
