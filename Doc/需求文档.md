# NewLife.Siemens 需求文档

## 1. 愿景与目标

### 1.0 项目愿景

提供工业物联网领域**最完整、最易用的西门子 PLC S7 协议 .NET 实现**，作为 NewLife 开源生态的 IoT 标准驱动组件，让工控工程师能够用几行代码无缝接入任意型号的西门子 PLC，同时支持在 IoTEdge 工业网关中零代码配置化采集。

### 1.1 背景与痛点

工业物联网（IIoT）领域中，西门子 PLC 是全球市场占有率最高的可编程控制器之一，广泛应用于制造业、能源、交通、楼宇自动化等行业。S7 协议是西门子专有的以太网通信协议，支持 SIMATIC S7-200/300/400/1200/1500 等主流系列 PLC。

现有 .NET 生态中的 S7 协议库（如 S7NetPlus）提供了基本的读写功能，但存在以下问题：
- 未遵循统一的 IoT 驱动接口规范，难以接入现有物联网平台
- 不支持多框架（仅 netstandard2.0），无法覆盖 .NET Framework 4.5 等老旧工控系统
- 与 NewLife 生态（NewLife.IoT / Stardust / ZeroIoT）不兼容，无法在 IoTEdge 网关场景中直接使用
- 缺乏完整的异步支持与自动断线重连机制，不适合长时间运行的工控场景

### 1.2 目标

NewLife.Siemens 旨在提供一套**完整的西门子 PLC S7 协议 .NET 实现**，与 NewLife 开源生态深度集成，作为 NewLife.IoT 标准驱动对接工业现场西门子 PLC 设备，实现数据采集与控制指令下发。

**可衡量的成功标准**：

| 目标 | 度量标准 | 当前状态 |
|------|---------|--------|
| 全系列 PLC 支持 | 覆盖 S7-200/200Smart/300/400/1200/1500/Logo0BA8 | ✅ 已达成 |
| 多框架支持 | net45 / net461 / netstandard2.0 / netstandard2.1 | ✅ 已达成 |
| 响应延迟 | 局域网单次读写 < 100ms | ✅ 已达成 |
| 单元测试覆盖 | 375+ 测试用例，核心协议路径全覆盖，含 E2E 集成测试 | ✅ 已达成 |
| IoT 驱动集成 | 可作为 NewLife.IoT 标准驱动插件加载 | ✅ 已达成 |
| 纯托管实现 | 无原生依赖，跨平台部署 | ✅ 已达成 |

---

## 2. 用户角色

| 角色 | 说明 | 核心诉求 |
|------|------|---------|
| 工控/IoT 工程师 | 直接使用 S7Client 编写设备采集程序 | 简洁的 API、完整的文档、准确的地址格式 |
| 网关集成商 | 将本驱动集成进 IoTEdge 网关 | 标准 IDriver 接口、多设备共享连接、故障自恢复 |
| 上层平台开发者 | 通过 NewLife.Stardust / ZeroIoT 平台管理 PLC 设备 | 即插即用的驱动注册、点位配置化、统一错误码 |
| 自动化测试工程师 | 用 S7Server 模拟 PLC 进行集成测试 | 可离线运行的轻量 PLC 模拟器 |

---

## 3. 功能需求

### 3.1 连接管理

- **描述**：建立、维护和销毁与 PLC 的 TCP 连接，处理断线自动重连
- **用户故事**：作为工控工程师，我希望创建 S7Client 后调用 OpenAsync() 即可完成三层握手（TPKT → COTP → S7 Setup），以便无需关心底层协议细节
- **验收条件**：
  - [x] 支持 TCP 连接建立（含 KeepAlive 设置）
  - [x] 完成 COTP ConnectionRequest / ConnectionConfirmed 握手
  - [x] 完成 S7 SetupMessage 协商 PDU 大小
  - [x] 断线后下次读写自动触发重连（GetStream 内部检测）
  - [x] 支持显式 Close() 释放连接
- **优先级**：Must

### 3.2 字节级读写

- **描述**：以字节数组为基本单位，从任意 PLC 地址区域读取或写入数据
- **用户故事**：作为工控工程师，我希望调用 ReadBytes(address, count) / WriteBytes(address, data) 即可完成数据交换，以便在需要时进行自定义数据解析
- **验收条件**：
  - [x] ReadBytes 支持任意长度（超 PDU 限制时自动分多段）
  - [x] WriteBytes 支持任意长度（超 PDU 限制时自动分多段）
  - [x] 读写失败时抛出包含错误码的 ApiException
  - [x] 支持所有地址区域：DB / Input / Output / Memory / Timer / Counter
- **优先级**：Must

### 3.3 PLC 地址解析

- **描述**：将人类可读的地址字符串解析为协议所需的区域、块号、字节偏移、位偏移
- **用户故事**：作为工控工程师，我希望使用 "DB1.DBW0"、"M0.0"、"IW4" 这样的西门子标准地址格式，以便与 Step7/TIA Portal 中的地址保持一致
- **验收条件**：
  - [x] 支持 DB（DBB/DBW/DBD/DBX/STRING）地址格式
  - [x] 支持 I/E（输入）地址（IB/IW/ID/I0.0 等）
  - [x] 支持 Q/A（输出）地址（QB/QW/QD/Q0.0 等）
  - [x] 支持 M（标志区）地址（MB/MW/MD/M0.0 等）
  - [x] 支持 T（定时器）地址
  - [x] 支持 C（计数器）地址
  - [x] STRING 地址格式：DB1.STRING0.60（第三段为最大长度）
  - [x] 解析错误时抛出 InvalidDataException
- **优先级**：Must

### 3.4 数据类型模型

- **描述**：定义 PLC 侧数据类型与 .NET 侧类型的对应关系
- **用户故事**：作为工控工程师，我希望通过枚举值精确指定 PLC 数据类型，以便驱动能够正确计算字节长度和传输大小标志位
- **验收条件**：
  - [x] 支持 Bit / Byte / Word / DWord
  - [x] 支持 Int / DInt（有符号整数）
  - [x] 支持 Real / LReal（单/双精度浮点）
  - [x] 支持 String / S7String / S7WString
  - [x] 支持 Timer / Counter / DateTime
  - [x] CpuType 枚举覆盖全系列（S7-200/Smart/300/400/1200/1500/Logo0BA8）
- **优先级**：Must

### 3.5 NewLife.IoT 驱动集成

- **描述**：作为 NewLife.IoT 标准驱动，可插件化加载到 IoTEdge 网关
- **用户故事**：作为网关集成商，我希望在 IoTEdge 配置界面选择"西门子PLC"驱动，填写 IP、CPU类型、机架、插槽后即可采集数据，以便不需要编写任何代码
- **验收条件**：
  - [x] SiemensS7Driver 继承 DriverBase，注册 [Driver("SiemensPLC")]
  - [x] 支持 Open(IDevice, IDriverParameter) 创建节点
  - [x] 多个节点共享同一 S7Client 连接（连接复用）
  - [x] Read(INode, IPoint[]) 按点位地址批量读取数据
  - [x] Write(INode, IPoint, value) 写入单个点位
  - [x] SiemensParameter 包含 Address/CpuType/Rack/Slot 配置项
- **优先级**：Must

### 3.6 错误处理体系

- **描述**：完整定义协议层和应用层的错误码，提供可区分的异常类型
- **用户故事**：作为工控工程师，我希望捕获 ApiException 时能从内置错误码判断是连接错误还是地址错误，以便快速定位问题
- **验收条件**：
  - [x] ErrorCode 枚举覆盖 NoError / WrongCPU_Type / ConnectionError / WrongVarFormat 等
  - [x] ReadWriteErrorCode 枚举覆盖 S7 协议层错误码（HardwareFault / AddressOutOfRange 等）
  - [x] ApiException 携带错误码（int code）和可读消息
  - [x] 连接异常时 GetStream 自动重建连接
- **优先级**：Must

### 3.7 字符串地址 API 与类型化读写（✅ 已完成）

- **描述**：提供更高层的 API，支持传入地址字符串和目标 .NET 类型，自动完成解析、读取、转换
- **用户故事**：作为工控工程师，我希望调用 `Read<Int16>("DB1.DBW0")` 直接获得 short 值，以便减少样板代码
- **验收条件**：
  - [x] `T Read<T>(string address)` 支持 bool/byte/short/ushort/int/uint/float/double/string
  - [x] `void Write(string address, object value)` 支持自动类型匹配
  - [x] 内置 S7 字节序转换（大端 → 小端）
  - [x] 支持数组读取：`T[] ReadArray<T>(string address, int count)`
- **优先级**：Should

### 3.8 批量多变量读写（✅ 已完成）

- **描述**：在单次 PDU 报文中打包多个变量的读写请求，减少网络往返次数
- **用户故事**：作为网关集成商，我希望在一次网络请求中读取 10 个不连续的 DB 变量，以便降低采集延迟
- **验收条件**：
  - [x] `ReadMultipleVars(IList<DataItem> items)` 支持在单 PDU 中打包多个变量请求
  - [x] 超出 PDU 限制时自动拆分为多次请求
  - [x] `WriteMultipleVars(IList<DataItem> items)` 支持批量写入
  - [x] DataItem 支持 DataType / VarType / DB / StartByteAdr / Count / Value 完整属性
- **优先级**：Should

### 3.9 S7Server PLC 模拟器（✅ 已完成）

- **描述**：在测试环境中模拟 PLC 响应，支持读写操作，无需真实硬件
- **用户故事**：作为自动化测试工程师，我希望启动一个内存中的 PLC 模拟器，以便在 CI 环境中运行完整的集成测试
- **验收条件**：
  - [x] S7Server 可绑定任意端口（默认 102）启动 TCP 侦听，基于 `NewLife.Net.NetServer`
  - [x] 支持 TPKT/COTP/S7 三层握手响应（COTP CR→CC，S7 Setup 回显）
  - [x] 内置 DB / Memory / Input / Output 各区域真实内存存储（地址寻址读写）
  - [x] ReadVar 返回内存区域实际数据
  - [x] WriteVar 持久化写入内存区域
  - [x] `SetValue(string address, byte[])` / `SetValue(string, Int16)` 等便捷预置接口
- **优先级**：Should

### 3.10 SZL 诊断读取（✅ 已完成）

- **描述**：读取 PLC 系统状态列表（SZL），获取 CPU 运行状态、模块信息等诊断数据
- **用户故事**：作为上层平台开发者，我希望查询 PLC 的 CPU 型号和运行状态，以便在平台上实时显示设备健康状态
- **验收条件**：
  - [x] 发送 UserData 功能码的 SZL 读请求（SZL_ID：0x001C CPU信息、0x0111 模块信息等）
  - [x] 返回结构化的 CpuInfo（型号、序列号、固件版本）
  - [x] 返回 CPU 运行状态（Run / Stop / Halt）
- **优先级**：Could

### 3.11 PLC 时钟读写（✅ 已完成）

- **描述**：读取和设置 PLC 内部实时时钟
- **用户故事**：作为工控工程师，我希望定期同步 PLC 时钟与 NTP 时间，以便保证数据时间戳准确性
- **验收条件**：
  - [x] `Task<DateTime> ReadClockAsync()` 读取 PLC 当前时间
  - [x] `Task WriteClockAsync(DateTime dt)` 设置 PLC 时间
  - [x] 适配 S7 DATE_AND_TIME 格式（BCD编码）
- **优先级**：Could

### 3.12 PLC 启停控制（✅ 已完成）

- **描述**：远程控制 PLC 运行状态（仅 PG 模式下支持）
- **用户故事**：作为工控工程师，我希望通过程序控制 PLC 热启动、冷启动或停机，以便在维护期间无需手动操作现场设备
- **验收条件**：
  - [x] `PlcStopAsync()` 发送 PlcStop (0x29) 指令，携带 "P_PROGRAM" 服务名
  - [x] `PlcHotRestartAsync()` 发送 PlcStart (0x28) 指令，模式字节 0xFD（热重启）
  - [x] `PlcColdStartAsync()` 发送 PlcStart (0x28) 指令，模式字节 0xFF（冷启动）
  - [x] 安全门控：`AllowPlcControl = false`（默认），未设置时调用上述方法抛出 `InvalidOperationException`
  - [x] S7Server 模拟器响应 PlcStop/PlcStart 并更新 `CpuStatus` 属性
- **优先级**：Should

### 3.13 ReadStruct / WriteStruct（✅ 已完成）

- **描述**：将 PLC DB 区域的连续字节直接映射为 C# struct，支持自动 S7 字节对齐
- **用户故事**：作为工控工程师，我希望定义一个与 TIA Portal DB 结构对应的 C# struct，调用 `ReadStruct<T>("DB1")` 一次性读取并填充所有字段
- **验收条件**：
  - [x] `T ReadStruct<T>(string dbAddress, int startByte = 0)` 支持 bool/byte/sbyte/short/ushort/int/uint/float/double 类型字段
  - [x] `void WriteStruct<T>(string dbAddress, T value)` 将 struct 序列化写入
  - [x] `GetS7StructByteSize(Type)` 公开方法，可在外部预先计算字节大小
  - [x] S7 字节对齐规则：Bool/Byte=1B；Word/Int=2B（偶数对齐）；DWord/DInt/Real=4B；LReal=8B；末尾对齐到偶数
- **优先级**：Should

### 3.14 程序块管理（✅ 骨架完成）

- **描述**：列出、上传（从 PLC 下载到本地）、下载（从本地上传到 PLC）程序块
- **用户故事**：作为工控工程师，我希望通过 API 枚举 PLC 中的所有 DB 块并备份其字节内容，以便在系统升级前做灾难恢复
- **验收条件**：
  - [x] `ListBlocksAsync(S7BlockType)` 通过 SZL 0x0022 返回块列表（`S7BlockInfo[]`）
  - [x] `UploadBlockAsync(blockType, blockNumber)` 执行三步上传协议（StartUpload/Upload/EndUpload），返回原始 MC7 字节
  - [x] `DownloadBlockAsync(blockType, blockNumber, data)` 执行三步下载协议（StartDownload/Download/EndDownload）
  - [x] S7Server 模拟器支持块上传/下载响应（StartUpload/Upload/EndUpload/StartDownload/Download/EndDownload 六种处理）
- **优先级**：Could（实体协议已实现，S7Server 模拟器已完善）

### 3.15 GTM（全局类型消息）支持（✅ 已完成）

- **描述**：在 S7 Setup Communication 阶段附加 GTM（Global Type Message）扩展数据，支持 S7-1200/1500 固件 4.0+ 的强制 GTM 协商
- **用户故事**：作为工控工程师，我希望连接 S7-1200/1500 时自动完成 GTM 协商，以便无需手动配置即可与较新固件的 PLC 建立正常通信
- **验收条件**：
  - [x] S7-1200/1500 连接时自动在 SetupMessage 附加 5 字节 GTM 扩展
  - [x] SetupMessage 支持读取/写入 GTM 扩展数据（13 字节参数）
  - [x] 非 S7-1200/1500 的 PLC 不受影响（标准 8 字节参数向后兼容）
  - [x] GTM 消息序列化/反序列化往返测试覆盖
- **优先级**：Should

### 3.16 密码保护连接（✅ 已完成）

- **描述**：支持 S7-1200/1500 的 CPU 会话密码认证，通过 UserData CPU 功能组发送密码以获取操作权限
- **用户故事**：作为工控工程师，我希望连接有密码保护的 PLC 时能通过 API 提交密码，以便在安全合规的生产环境中正常读写数据
- **验收条件**：
  - [x] `SetPasswordAsync(password)` 通过 UserData（Func=0x00, SubFunc=0x01）发送密码认证
  - [x] `ClearPasswordAsync()` 清除当前会话认证状态（SubFunc=0x02）
  - [x] `ReadProtectionLevelAsync()` 通过 SZL 0x0232 读取当前保护级别
  - [x] `S7Client.Password` 属性支持在 `OpenAsync` 中自动认证
  - [x] 密码固定 8 字节 ASCII，不足右侧空格补齐
  - [x] 认证失败不阻塞连接建立，仅记录日志
  - [x] `ProtectionLevel` 枚举覆盖 NoProtection / WriteProtected / ReadWriteProtected / FullProtection
  - [x] S7Server 模拟器支持密码验证响应
- **优先级**：Should

---

## 4. 非功能需求

### 4.1 性能

- 单次读写操作（局域网内，数据 ≤ PDU 限制）响应时间 < 100ms
- 大批量读写（数据超 PDU 需分段）延迟线性增长，无额外固定开销
- 不在热点路径使用未缓存反射或复杂 LINQ

### 4.2 可靠性

- TCP KeepAlive 检测连接中断
- 网络断线后下次操作自动重连，重连失败向上层抛出异常
- 内存流、网络流使用 `using` 确保释放，无资源泄漏

### 4.3 兼容性

- 多框架：net45 / net461 / netstandard2.0 / netstandard2.1
- 多 PLC 型号：S7-200/200Smart/300/400/1200/1500/Logo0BA8
- S7-1200/1500 特别说明：需关闭 TIA Portal 中的"优化块访问"并开启 GET/PUT
- 字节顺序：S7 协议采用大端字节序，驱动层负责与 .NET 小端转换

### 4.4 安全

- 不暴露内部实现细节到异常消息中
- S7 协议本身无加密，连接在受控局域网内使用
- PLC 控制指令（启停）设置明确的安全门控，防止误触发

---

## 5. 边界与约束

### 不做的内容

- **S7Plus 协议**：西门子 S7-1200/1500 固件 4.0+ 使用全新的 S7Plus（ISO over TCP port 443），与本库使用的 S7 基础协议完全不同，不在本库范畴
- **OPC-UA**：标准物联网协议，由专用库实现
- **PROFIBUS / PROFINET IO / Modbus**：其他现场总线协议，由对应库实现
- **在线监控 / 强制变量 / 中断块**：TIA Portal 特定的高级 PG 功能，需 S7Plus 协议层支持

### 已知限制

- 单一 TCP 连接：S7 协议不支持链路复用（不同 RequestID 并发），并发请求需串行化
- S7-200 通过以太网通信时需要外置 CP243 模块或 S7-200 Smart 自带以太网口
- 某些旧型号 PLC 的 PDU 大小限制为 240 字节，驱动会自动协商并适配

---

## 6. 功能清单与迭代计划

### 迭代 1：核心协议栈（已完成）

| 编号 | 功能点 | 验收条件 | 状态 | 对应代码 |
|------|--------|---------|------|---------|
| F001 | 连接管理（TPKT/COTP/S7 三层握手、断线重连） | AC：3.1 全部 | ✅ 已完成 | `Protocols/S7Client.cs`, `Protocols/TPKT.cs`, `Protocols/COTP.cs` |
| F002 | 字节级读写（ReadBytes/WriteBytes，超PDU自动分段） | AC：3.2 全部 | ✅ 已完成 | `Protocols/S7Client.cs` (ReadBytes/WriteBytes) |
| F003 | PLCAddress 地址解析（DB/I/Q/M/T/C/STRING 全格式） | AC：3.3 全部 | ✅ 已完成 | `Protocols/PLCAddress.cs` |
| F004 | 数据类型模型（全 VarType/DataType/CpuType 枚举） | AC：3.4 全部 | ✅ 已完成 | `Models/VarType.cs`, `Models/DataType.cs`, `Models/CpuType.cs` |
| F005 | NewLife.IoT 驱动集成（SiemensS7Driver） | AC：3.5 全部 | ✅ 已完成 | `Drivers/SiemensS7Driver.cs`, `Drivers/SiemensParameter.cs`, `Drivers/SiemensNode.cs` |
| F006 | 错误处理体系（ApiException / ErrorCode / ReadWriteErrorCode） | AC：3.6 全部 | ✅ 已完成 | `Models/ErrorCode.cs`, `Messages/ReadWriteErrorCode.cs`（ApiException 来自 NewLife.Remoting） |

### 迭代 2：高级读写 API（已完成，Should 级别）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F007 | 字符串地址 API + 泛型类型化读写 | AC：3.7 全部 | F001, F002, F003 | 1.5d | ✅ 已完成 |
| F008 | 批量多变量读写（ReadMultipleVars / WriteMultipleVars） | AC：3.8 全部 | F001, F002 | 1d | ✅ 已完成 |

### 迭代 3：测试基础设施（已完成，Should 级别）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F009 | S7Server PLC 模拟器（完整实现） | AC：3.9 全部 | F001 | 1.5d | ✅ 已完成 |

### 迭代 4：诊断与高级功能（Could 级别）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F010 | SZL 诊断读取（CPU状态、模块信息） | AC：3.10 全部 | F001 | 1d | ✅ 已完成 |
| F011 | PLC 时钟读写 | AC：3.11 全部 | F001 | 0.5d | ✅ 已完成 |

### 迭代 5：PLC 控制与结构体 API（✅ 已完成）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F012 | PLC 启停控制（Stop / HotRestart / ColdStart + AllowPlcControl 门控） | AC：3.12 全部 | F001 | 1d | ✅ 已完成 |
| F013 | ReadStruct / WriteStruct（struct 映射，S7 字节对齐） | AC：3.13 全部 | F002, F003 | 1d | ✅ 已完成 |

### 迭代 6：程序块管理（骨架完成，Could 级别）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F014 | 块列表（ListBlocksAsync via SZL 0x0022） | AC：3.14 前三项 | F010 | 0.5d | ✅ 已完成 |
| F015 | 块上传（UploadBlockAsync，三步协议） | AC：3.14 | F001 | 1d | ✅ 已完成 |
| F016 | 块下载（DownloadBlockAsync，三步协议） | AC：3.14 | F015 | 1d | ✅ 已完成 |

### 迭代 7：GTM 协议增强（✅ 已完成，Should 级别）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F017 | GTM（全局类型消息）支持 | AC：3.15 全部 | F001 | 0.5d | ✅ 已完成 |

### 迭代 8：质量工程与文档（✅ 已完成，Could 级别）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F018 | BenchmarkDotNet 性能基准 | 覆盖地址解析/消息序列化/类型转换 3 类 | — | 0.5d | ✅ 已完成 |
| F019 | 英文 README（Readme.en.md） | 涵盖快速入门/API/架构/竞品对比 | — | 0.5d | ✅ 已完成 |
| F020 | coverlet 测试覆盖率采集 | XUnitTest 集成 coverlet.collector | — | 0.1d | ✅ 已完成 |

### 迭代 9：安全认证（✅ 已完成，Should 级别）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F021 | 密码保护连接（SetPassword/ClearPassword/ReadProtectionLevel + 自动认证） | AC：3.16 全部 | F001, F010 | 1d | ✅ 已完成 |

### 迭代 10：交叉兼容测试与文档（✅ 已完成）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F022 | 竞品交叉兼容测试（S7netplus ⇄ 本库 S7Server） | N2 模式：S7netplus 客户端可连接并读写 S7Server；N3 模式：自通信边界场景扩展 | F009 | 0.5d | ✅ 已完成 |
| F023 | S7Plus / Siemens Web API 技术分析文档 | 从协议本质、技术可行性、架构兼容性三个维度完成深度分析，给出明确建议 | — | 0.5d | ✅ 已完成 |
| F024 | 竞品分析报告修正 | 修正 GTM/密码保护两处过期数据，更新差距分析和行动建议 | — | 0.1d | ✅ 已完成 |

### 迭代 11：社区工程与 CI 基础设施（✅ 已完成）

| 编号 | 功能点 | 验收条件 | 前置依赖 | 预估工作量 | 状态 |
|------|--------|---------|---------|----------|------|
| F025 | S7netplus 完整 N2 交叉测试（真实 Plc 客户端调用） | S7netplus Plc 可连接 S7Server 并完成读取/写入/字节级一致性验证 | F009, F022 | 0.5d | ✅ 已完成 |
| F026 | 跨库性能对比基准（BenchmarkDotNet: 本库 vs S7netplus） | 覆盖本地操作（地址解析/消息构建）和网络操作（读写延迟）两类基准 | F018 | 0.5d | ✅ 已完成 |
| F027 | NuGet 包发布说明更新 | PackageReleaseNotes 覆盖 v1.2.2026.0630 全部变更 | — | 0.1d | ✅ 已完成 |
| F028 | Docker 开发环境示例 | Dockerfile + docker-compose.yml + README | — | 0.3d | ✅ 已完成 |
| F029 | GitHub Actions CI 流水线 | push/PR 触发 build + test（ubuntu + windows 双平台） | — | 0.3d | ✅ 已完成 |

---

## 7. 验收记录

### 功能验收

| 编号 | 功能点 | 验收条件 | 状态 | 备注 |
|------|--------|---------|------|------|
| F001 | 连接管理 | 3.1 全部 | ✅ | 150+ 单元测试通过，真机读写测试通过（2026-05-02） |
| F002 | 字节级读写 | 3.2 全部 | ✅ | 超PDU分段逻辑已验证 |
| F003 | 地址解析 | 3.3 全部 | ✅ | PLCAddressTests.cs 覆盖 60+ 地址格式 |
| F004 | 数据类型 | 3.4 全部 | ✅ | ModelTests.cs 覆盖所有枚举值 |
| F005 | IoT 驱动集成 | 3.5 全部 | ✅ | DriverTests.cs 通过 |
| F006 | 错误处理 | 3.6 全部 | ✅ | CommonTests.cs 通过 |
| F007 | 类型化读写 | 3.7 全部 | ✅ | `S7Client.HighLevel.cs`，S7HighLevelTests.cs 覆盖 49 个测试 |
| F008 | 批量读写 | 3.8 全部 | ✅ | `S7Client.Batch.cs`，S7BatchTests.cs 覆盖 21 个测试 |
| F009 | S7Server | 3.9 全部 | ✅ | 完整内存区域存储，S7IntegrationTests/S7ServerMemoryTests 覆盖 78 个测试 |
| F010 | SZL 诊断 | 3.10 全部 | ✅ | `S7Client.UserData.cs`，S7SZLTests.cs 全覆盖 |
| F011 | PLC 时钟 | 3.11 全部 | ✅ | `S7Client.UserData.cs`，S7ClockTests.cs 全覆盖 |
| F012 | PLC 启停控制 | 3.12 全部 | ✅ | `S7Client.UserData.cs`（PlcStopAsync/PlcHotRestartAsync/PlcColdStartAsync），S7PLCTests.cs 覆盖 5 个测试 |
| F013 | ReadStruct / WriteStruct | 3.13 全部 | ✅ | `S7Client.HighLevel.cs`，S7PLCTests.cs 覆盖 3 个测试 |
| F014~F016 | 程序块管理 | 3.14 | ✅ | `S7Client.BlockTransfer.cs`（协议实现完成，S7Server 模拟器支持块上传/下载全流程） |
| F017 | GTM 支持 | 3.15 全部 | ✅ | `SetupMessage.cs` GTM 扩展 + `S7Client.cs` SetupConnection 自动协商 |
| F018 | BenchmarkDotNet 基准 | — | ✅ | `BenchmarkTest/` 项目：地址解析/消息序列化/类型转换 3 类基准 |
| F019 | 英文 README | — | ✅ | `Readme.en.md`：快速入门/API/架构/竞品对比完整英文文档 |
| F020 | coverlet 覆盖率 | — | ✅ | `XUnitTest.csproj` 集成 `coverlet.collector` 6.0.4 |
| F021 | 密码保护连接 | 3.16 全部 | ✅ | `S7Client.UserData.cs`（SetPassword/ClearPassword/ReadProtectionLevel），`S7Client.cs` Password 自动认证，`S7Server.cs` 密码验证模拟 |
| F022 | 竞品交叉兼容测试 | N2+N3 模式 | ✅ | `S7CrossCompatibilityTests.cs`：S7netplus ⇄ S7Server 互操作验证，超PDU分段/位操作/多CPU类型 |
| F023 | S7Plus/WebAPI 分析 | 文档完成 | ✅ | `Doc/S7Plus与WebAPI分析.md`：结论建议不做，维护现有 S7 Classic 定位 |
| F024 | 竞品分析修正 | 数据更新 | ✅ | `Doc/竞品分析报告.md`：GTM/密码 ❌→✅，差距分析和行动建议同步更新 |
| F025 | S7netplus N2 交叉测试 | 真实 Plc 调用 | ✅ | `S7CrossCompatibilityTests.cs`：S7netplus Plc 连接→读取→写入→字节一致性 4 个完整 N2 测试 |
| F026 | 跨库性能对比基准 | 本地+网络 | ✅ | `BenchmarkTest/CrossLibBenchmarks.cs`：地址解析/消息构建/读写延迟 8 项对比基准 |
| F027 | NuGet 发布说明 | 元数据更新 | ✅ | `NewLife.Siemens.csproj` PackageReleaseNotes 覆盖全部新功能 |
| F028 | Docker 开发环境 | Dockerfile+Compose | ✅ | `Docker/`：Dockerfile + docker-compose.yml + README.md |
| F029 | GitHub Actions CI | 双平台流水线 | ✅ | `.github/workflows/dotnet.yml`：push/PR 触发 ubuntu+windows 双平台构建测试 |

---

## 8. 术语表

| 术语 | 定义 |
|------|------|
| S7 协议 | 西门子专有的 PLC 以太网通信应用层协议，基于 ISO-on-TCP（RFC 1006） |
| TPKT | ISO Transport Protocol Data Unit，4字节头（版本+保留+长度），封装在 TCP 之上 |
| COTP | Connection-Oriented Transport Protocol（ISO 8073），S7 的传输层，提供面向连接的可靠传输 |
| PDU | Protocol Data Unit，S7 协议一次请求/响应的最大数据单元，不同 PLC 型号为 240～4096 字节不等 |
| DB | Data Block（数据块），PLC 中用于存储结构化数据的存储区，按编号访问（如 DB1） |
| TSAP | Transport Service Access Point，COTP 层的连接标识，由 CPU 类型、机架号、插槽号派生 |
| SZL | System Status List，西门子 PLC 系统诊断数据列表，通过 UserData 功能码读取 |
| AMQ | Acknowledged Message Queue，S7 Setup 协商时的消息队列深度参数 |
| IoTEdge | NewLife 生态中的工业物联网网关软件，支持驱动插件化加载 |
| TIA Portal | Siemens 完全集成自动化工程软件，用于 S7-1200/1500 的编程与配置 |
| Step7 | 西门子早期编程软件，用于 S7-300/400 的编程与配置 |
| GET/PUT | S7 协议的基础数据读写服务，S7-1200/1500 默认禁用，需在 TIA Portal 安全设置中显式开启 |
