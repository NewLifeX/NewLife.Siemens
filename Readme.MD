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

---

## 新生命项目矩阵
各项目默认支持net9.0/netstandard2.1/netstandard2.0/net4.62/net4.5，旧版（2024.0801）支持net4.0/net2.0  

|                               项目                               | 年份  | 说明                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             基础组件                             |       | 支撑其它中间件以及产品项目                                                                  |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 核心库，日志、配置、缓存、网络、序列化、APM性能追踪                                         |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 大数据中间件，单表百亿级，MySql/SQLite/SqlServer/Oracle/PostgreSql/达梦，自动分表，读写分离 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 网络库，单机千万级吞吐率（2266万tps），单机百万级连接（400万Tcp长连接）                     |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | 协议通信库，提供CS应用通信框架，支持Http/RPC通信框架，高吞吐，物联网设备低开销易接入        |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 魔方快速开发平台，集成了用户权限、SSO登录、OAuth服务端等，单表100亿级项目验证               |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 服务管理组件，把应用安装成为操作系统守护进程，Windows服务、Linux的Systemd                   |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | Zero零代脚手架，基于NewLife组件生态的项目模板NewLife.Templates，Web、WebApi、Service        |
|                              中间件                              |       | 对接知名中间件平台                                                                          |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | Redis客户端，微秒级延迟，百万级吞吐，丰富的消息队列，百亿级数据量项目验证                   |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | RocketMQ纯托管客户端，支持Apache RocketMQ和阿里云消息队列，十亿级项目验                     |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 物联网消息协议，MqttClient/MqttServer，客户端支持阿里云物联网                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | IoT标准库，定义物联网领域的各种通信协议标准规范                                             |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII，基于IoT标准库实现，支持ZeroIoT平台和IoTEdge网关            |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | 西门子PLC协议，基于IoT标准库实现，支持IoT平台和IoTEdge                                      |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | 地图组件库，封装百度地图、高德地图、腾讯地图、天地图                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | 音频编解码库，PCM/ADPCMA/G711A/G722U/WAV/AAC                                                |
|                             产品平台                             |       | 产品平台级，编译部署即用，个性化自定义                                                      |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 星尘，分布式服务平台，节点管理、APM监控中心、配置中心、注册中心、发布中心                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 蚂蚁调度，分布式大数据计算平台（实时/离线），蚂蚁搬家分片思想，万亿级数据量项目验证         |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | 企业ERP，产品管理、客户管理、销售管理、供应商管理                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 码神工具，众多开发者工具，网络、串口、加解密、正则表达式、Modbus、MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | 简易文件存储，支持分布式系统中文件集中存储。                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 产品级反向代理，NAT代理、Http代理                                                           |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | Http压力测试工具                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | Git源代码管理系统                                                                           |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 嵌入式操作系统，完全独立自主，支持ARM Cortex-M芯片架构                                      |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | 嵌入式工业计算机，物联网边缘网关，高性能.NET8主机，应用于工业、农业、交通、医疗             |
|                          FIoT物联网平台                          | 2020  | 物联网整体解决方案，建筑、环保、农业，软硬件及大数据分析一体化，单机十万级点位项目验证      |
|                        UWB高精度室内定位                         | 2020  | 厘米级（10~20cm）高精度室内定位，软硬件一体化，与其它系统联动，大型展厅项目验证             |



## 新生命开发团队
![XCode](https://newlifex.com/logo.png)  

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的80多个开源项目已被广泛应用于各行业，Nuget累计下载量高达400余万次。  
团队开发的大数据中间件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台FIoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。  

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IoT服务供应商。  

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838  
微信公众号：  
![智能大石头](https://newlifex.com/stone.jpg)  
