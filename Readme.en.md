# NewLife.Siemens — Siemens PLC S7 Protocol for .NET

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Siemens?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Siemens?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Siemens?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Siemens?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Siemens?label=dev%20nuget&logo=nuget)

**NewLife.Siemens** is a pure managed .NET library implementing the Siemens S7 Ethernet protocol for direct communication with SIMATIC PLCs. It supports the full S7-200/300/400/1200/1500/Logo series across net45, net461, netstandard2.0, and netstandard2.1.

Source: https://github.com/NewLifeX/NewLife.Siemens  
NuGet: [NewLife.Siemens](https://www.nuget.org/packages/NewLife.Siemens)

---

## Overview

The Siemens S7 protocol is the most widely used PLC Ethernet communication protocol in industrial automation. It operates on ISO-on-TCP (port 102) with a three-layer stack: **TPKT → COTP → S7**.

NewLife.Siemens provides a complete implementation of this protocol stack with a clean C# API for reading and writing all PLC data areas (DB, Input, Output, Memory, Timer, Counter). It also serves as a standard driver for [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT), enabling zero-code configuration-based data acquisition through the IoTEdge industrial gateway.

## Feature Status

| Feature | Status | Notes |
|---------|--------|-------|
| Connection management (TPKT/COTP/S7 handshake) | ✅ Complete | Auto-reconnect, TCP KeepAlive |
| Byte-level Read/Write (ReadBytes/WriteBytes) | ✅ Complete | Auto PDU segmentation |
| PLC address parsing (DB/I/Q/M/T/C/STRING) | ✅ Complete | Full format string parsing |
| Data type model (VarType/DataType/CpuType) | ✅ Complete | All PLC series |
| NewLife.IoT driver integration (SiemensS7Driver) | ✅ Complete | IoTEdge plugin support |
| Error handling (ErrorCode/ApiException) | ✅ Complete | Categorized exceptions |
| S7Server PLC simulator | ✅ Complete | In-memory storage, full read/write |
| Typed Read<T>/Write API | ✅ Complete | Auto endianness conversion |
| Batch multi-variable read/write | ✅ Complete | Single PDU packing, auto batching |
| SZL diagnostics (CPU status, module info) | ✅ Complete | ReadSzlAsync / ReadCpuInfoAsync |
| PLC clock read/write | ✅ Complete | ReadClockAsync / WriteClockAsync |
| PLC start/stop control | ✅ Complete | Safety gating via AllowPlcControl |
| ReadStruct<T>/WriteStruct<T> | ✅ Complete | C# struct ↔ PLC DB mapping |
| Block upload/download | ✅ Complete | StartUpload/Upload/EndUpload protocol |
| GTM (Global Type Message) | ✅ Complete | S7-1200/1500 firmware 4.0+ auto-negotiation |

## Key Features

- **Complete protocol stack**: TPKT / COTP / S7 full implementation with CR/CC handshake and PDU size negotiation
- **All PLC series**: S7-200, S7-200 Smart, S7-300, S7-400, S7-1200, S7-1500, Siemens Logo 0BA8
- **All address formats**: DB (DBB/DBW/DBD/DBX/STRING), Input (I/E), Output (Q/A), Memory (M), Timer (T), Counter (C)
- **Auto PDU segmentation**: Transparent chunking when data exceeds PDU limits
- **Auto-reconnect**: Built-in TCP KeepAlive with automatic reconnection on next operation
- **Fully async API**: `OpenAsync` and async methods for high-concurrency IoT scenarios
- **Multi-target**: net45 / net461 / netstandard2.0 / netstandard2.1, covering legacy Win7 to modern Linux
- **IoT driver ready**: `SiemensS7Driver` implements `DriverBase` for direct IoTEdge loading
- **Built-in PLC simulator**: `S7Server` for offline integration testing in CI
- **GTM support**: Automatic Global Type Message negotiation for S7-1200/1500 firmware 4.0+

## Supported PLC Models

| Enum Value | Model | Rack | Slot |
|-----------|-------|------|------|
| `S7200` | SIMATIC S7-200 (requires CP243) | 0 | 0 |
| `S7200Smart` | SIMATIC S7-200 Smart | 0 | 0 |
| `S7300` | SIMATIC S7-300 | 0 | 2 |
| `S7400` | SIMATIC S7-400 | 0 | 2 |
| `S71200` | SIMATIC S7-1200 | 0 | 0 |
| `S71500` | SIMATIC S7-1500 | 0 | 0 |
| `Logo0BA8` | Siemens LOGO! 0BA8 | 0 | 0 |

> **S7-1200/1500 note**: You must disable "Optimized block access" for DBs in TIA Portal and enable "Permit access with PUT/GET" in CPU protection settings.

## Address Format Reference

| Format | Description |
|--------|-------------|
| `DB1.DBB0` | DB1 byte 0 |
| `DB1.DBW10` | DB1 word at byte 10 (2 bytes) |
| `DB1.DBD20` | DB1 double word at byte 20 (4 bytes) |
| `DB1.DBX0.3` | DB1 byte 0 bit 3 |
| `DB1.STRING0.60` | DB1 string at byte 0 (max 60 chars) |
| `IB0` / `IW2` / `ID4` | Input byte/word/dword |
| `QB0` / `QW2` / `QD4` | Output byte/word/dword |
| `MB0` / `MW4` / `MD8` | Memory (Merker) byte/word/dword |
| `M0.5` | Memory byte 0 bit 5 |
| `T10` | Timer T10 |
| `C5` | Counter C5 |

## Quick Start

### Installation

```shell
dotnet add package NewLife.Siemens
```

### Direct S7Client Usage

```csharp
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;

// Create client (CPU type, IP, port, rack, slot)
var client = new S7Client(CpuType.S71200, "192.168.1.100", 102, rack: 0, slot: 0);

// Establish connection (TPKT/COTP/S7 handshake)
await client.OpenAsync();

// Read bytes (DB1, starting at byte 0, 10 bytes)
var address = new PLCAddress("DB1.DBB0");
var data = client.ReadBytes(address, 10);
Console.WriteLine(data.ToHex());

// Read a Word (DB1.DBW20)
var wordAddr = new PLCAddress("DB1.DBW20");
var wordData = client.ReadBytes(wordAddr, 2);
var wordValue = (ushort)((wordData[0] << 8) | wordData[1]);

// Write bytes
var writeAddr = new PLCAddress("DB1.DBW20");
client.WriteBytes(writeAddr, new byte[] { 0x00, 0x64 }); // Write 100 (big-endian)

// Close connection
client.Close();
```

### Typed Read/Write API

```csharp
// Generic read (auto big-endian → little-endian conversion)
var temp = client.Read<Int16>("DB1.DBW0");       // Read INT
var pressure = client.Read<Single>("DB1.DBD4");   // Read REAL
var flag = client.Read<Boolean>("M0.0");          // Read BOOL bit

// Read string (includes 2-byte S7 header)
var name = client.ReadString("DB1.STRING0.60");

// Generic write
client.Write("DB1.DBW0", (Int16)150);
client.Write("M0.0", true);

// Read array (consecutive variables)
var temps = client.ReadArray<Int16>("DB1.DBW0", 10); // Read 10 INTs
```

### Batch Multi-Variable Read/Write

```csharp
using NewLife.Siemens.Messages;

var items = new List<DataItem>
{
    DataItem.FromAddress("DB1.DBW0",  VarType.Int),
    DataItem.FromAddress("DB1.DBD4",  VarType.Real),
    DataItem.FromAddress("M0.0",      VarType.Bit),
};

// Single PDU batch read (auto split if exceeds PDU limit)
client.ReadMultipleVars(items);
Console.WriteLine($"Temp={items[0].Value}, Pressure={items[1].Value}, Run={items[2].Value}");

// Batch write
items[0].Value = (Int16)200;
client.Write(items.ToArray());
```

### SZL Diagnostics & Clock

```csharp
// Read CPU info
var info = await client.ReadCpuInfoAsync();
Console.WriteLine($"Model={info.ArticleNumber}, S/N={info.SerialNumber}");

// Read CPU status (Run / Stop / Halt)
var status = await client.ReadCpuStatusAsync();

// Read PLC clock
var plcTime = await client.ReadClockAsync();

// Sync PLC clock to system time
await client.WriteClockAsync(DateTime.Now);
```

### PLC Control (safety-gated)

```csharp
client.AllowPlcControl = true; // Must explicitly enable

await client.PlcStopAsync();        // Stop PLC
await client.PlcHotRestartAsync();  // Hot restart
await client.PlcColdStartAsync();   // Cold start
```

### Struct Mapping

```csharp
// Define a struct matching your TIA Portal DB layout
struct MotorData
{
    public Boolean Running;
    public Single Speed;
    public Int16 Temperature;
}

// One-line read from PLC
var motor = client.ReadStruct<MotorData>("DB1");

// One-line write to PLC
client.WriteStruct("DB1", motor);
```

### Built-in PLC Simulator (for testing)

```csharp
var server = new S7Server { Port = 0 };
server.Start();

// Pre-set test data
server.SetValue("DB1.DBW0", (Int16)12345);
server.SetValue("M0.0", true);

var client = new S7Client(CpuType.S71200, "127.0.0.1", server.Port);
await client.OpenAsync();

var value = client.Read<Int16>("DB1.DBW0"); // 12345
```

### IoTEdge Driver Integration

```csharp
var driver = new SiemensS7Driver();
var param = new SiemensParameter
{
    Address = "192.168.1.100:102",
    CpuType = CpuType.S71200,
};
var node = driver.Open(device, param);

var points = new IPoint[]
{
    new PointModel { Name = "Temperature", Address = "DB1.DBW0" },
    new PointModel { Name = "Pressure",    Address = "DB1.DBD4" },
};
var result = driver.Read(node, points);
```

## Performance Benchmarks

Run benchmarks locally:

```shell
dotnet run -c Release --project BenchmarkTest
```

The benchmark suite covers:
- **PLCAddress parsing** — throughput of address string parsing
- **S7Message serialization** — message build/parse speed (including GTM)
- **Typed Read<T>/Write** — end-to-end type conversion with S7Server

## Comparison with Alternatives

| Feature | NewLife.Siemens | S7netplus | Sharp7 | HslCommunication |
|---------|:---:|:---:|:---:|:---:|
| PLC series coverage | ✅ All | ✅ All | ⚠️ Limited | ✅ All |
| Auto-reconnect | ✅ | ❌ | ❌ | ✅ |
| Fully async API | ✅ | ⚠️ Partial | ❌ | ✅ |
| IoT driver interface | ✅ | ❌ | ❌ | ❌ |
| Built-in PLC simulator | ✅ | ❌ | ❌ | ❌ |
| ReadStruct<T> | ✅ | ❌ | ❌ | ✅ |
| GTM support | ✅ | ✅ | ❌ | ✅ |
| SZL diagnostics | ✅ | ❌ | ❌ | ❌ |
| PLC clock read/write | ✅ | ❌ | ❌ | ✅ |
| Block upload/download | ✅ | ❌ | ❌ | ❌ |
| License | MIT | MIT | MIT | Commercial |
| Multi-target TFMs | 4 | 2 | 2 | 7+ |
| Package size | 413 KB | 372 KB | 64 KB | 9.5 MB |

> See `Doc/竞品分析报告.md` for detailed competitive analysis (Chinese).

---

## Architecture

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

## License

MIT — use freely in commercial closed-source applications.

## Community

- Website: https://newlifex.com
- GitHub: https://github.com/NewLifeX
- NuGet: https://www.nuget.org/profiles/NewLifeX
- QQ Group: 1600800 / 1600838 (Chinese)

---

*NewLife Team — Building IoT infrastructure since 2002*
