# S7Plus 与 Siemens Web API 技术分析

## 1. 背景

用户提出是否将 S7Plus 协议和 Siemens Web API 纳入 NewLife.Siemens 功能范畴。本文从协议本质、技术可行性、架构兼容性三个维度做深入分析，给出明确建议。

---

## 2. S7Plus 协议分析

### 2.1 协议定位

S7Plus 是西门子自 S7-1200/1500 固件 4.0+ 起引入的**新一代** PLC 通信协议，用于替代传统 S7 协议的部分功能。

| 维度 | S7（本库实现） | S7Plus |
|------|:---:|:---:|
| **传输层端口** | TCP 102 | TCP 443（ISO-on-TCP） |
| **协议家族** | S7 Classic（ISO 8073 + RFC 1006） | S7Plus（全新消息格式） |
| **消息结构** | 固定头 10~12 字节 + 参数 + 数据 | 完全不同的 TLV 结构 |
| **功能码体系** | ReadVar(0x04)/WriteVar(0x05)/Setup(0xF0) | 全新的功能码集合 |
| **握手流程** | TPKT → COTP CR/CC → S7 Setup | 不同的握手序列 |
| **向后兼容** | — | ❌ 与 S7 Classic 完全不兼容 |
| **文档化程度** | 公开协议（被大量逆向工程文档化） | 闭源协议（西门子未公开规范） |
| **社区逆向程度** | 非常成熟（S7netplus/Sharp7 等） | 初步（仅少数库部分支持） |
| **覆盖 PLC 型号** | S7-200/300/400/1200/1500/Logo | **仅** S7-1200/1500 固件 ≥4.0 |

### 2.2 技术障碍

#### 2.2.1 完全不同的传输层

```
S7 Classic:
  TCP(102) → TPKT(4字节头) → COTP(CR/CC/DT) → S7 Message(0x32头)

S7Plus:
  TCP(443) → TPKT(变体) → 不同传输层 → S7Plus Message(全新格式)
```

S7Plus 的 TPKT 头部格式与 S7 Classic 不同，COTP 帧结构也不同。这意味着：
- 现有 `TPKT.cs` / `COTP.cs` / `S7Message.cs` **完全无法复用**
- 需要从头实现一整套新的协议栈
- 代码量估计与现有 S7 协议栈相当（~3000 行）

#### 2.2.2 闭源协议，文档极度匮乏

- 西门子官方未公开 S7Plus 协议规范
- 社区逆向成果有限，仅覆盖部分功能码
- S7netplus 的 S7Plus 支持仅覆盖基本读写，且标记为实验性
- 维护成本极高：每次西门子固件更新都可能破坏兼容性

#### 2.2.3 覆盖范围狭窄

S7Plus 仅适用于 S7-1200/1500 固件 4.0+，意味着：
- S7-200/200Smart/300/400/Logo 全系列**不支持**
- 大量存量 S7-1200/1500（固件 <4.0）**不支持**
- 用户需要同时维护两套协议栈才能覆盖全系列，反而增加复杂度

### 2.3 结论：❌ 不建议纳入 NewLife.Siemens

**理由**：
1. S7Plus 是与 S7 Classic 完全不同的协议，强行纳入同一库将违反「单一职责原则」
2. 协议栈完全无法复用，相当于在同一个项目中维护两个独立产品
3. 闭源协议 + 固件绑定 = 维护噩梦
4. 需求文档 §5 已明确排除：「S7Plus 协议不在本库范畴」

**替代建议**：
如果未来确实有 S7Plus 需求，建议新建独立项目 `NewLife.S7Plus`（或 `NewLife.SiemensPlus`），与 `NewLife.Siemens` 共享上层 API 设计风格但独立维护协议栈。

---

## 3. Siemens Web API 分析

### 3.1 协议定位

Siemens Web API 是西门子官方提供的 REST/HTTPS 接口，内置于 S7-1200/1500 的 Webserver 中。

| 维度 | S7（本库实现） | Siemens Web API |
|------|:---:|:---:|
| **传输层** | TCP 102（ISO-on-TCP） | HTTPS 443 |
| **协议类型** | 二进制应用层协议 | REST/JSON |
| **寻址方式** | 地址编码（DB1.DBW0） | 符号名（Symbolic Name） |
| **PLC 要求** | GET/PUT 功能需在 TIA Portal 开启 | Webserver 需在 TIA Portal 启用 |
| **覆盖型号** | 全系列 | **仅** S7-1200/1500（部分 G2 型号） |
| **安全模型** | 无加密（局域网内使用） | HTTPS 加密 + 用户认证 |
| **官方支持** | 无（协议为第三方逆向实现） | ✅ 西门子官方提供 NuGet 包 |
| **数据格式** | 二进制大端序 | JSON |

### 3.2 技术障碍

#### 3.2.1 完全不同的协议栈

```
S7:
  TCP(102) → TPKT → COTP → S7 Message → 二进制数据

Siemens Web API:
  HTTPS(443) → HTTP → JSON → REST 资源模型
```

这是一个从 L4 到 L7 完全不同的协议栈：
- 需要引入 `HttpClient` / `ApiHttpClient` 依赖
- 需要实现 REST 资源路由和 JSON 序列化
- 需要处理 OAuth/Token 认证流程
- 现有的 TPKT/COTP/S7Message 全部不可复用

#### 3.2.2 寻址模型不兼容

- S7 协议：基于物理地址编码（DB 号 + 字节偏移 + 位偏移）
- Web API：基于符号名（TIA Portal 中定义的变量名，如 `"ConveyorSpeed"`）

这两种模型无法在同一个 `Read<T>("...")` 接口中统一——地址字符串格式完全不同。

#### 3.2.3 西门子已有官方实现

```xml
<PackageReference Include="Siemens.Simatic.S7.Webserver.API" Version="3.3.56" />
```

官方库已覆盖 Web API 的全部功能。本库重新实现的意义不大，且容易引入不一致行为。

#### 3.2.4 覆盖范围狭窄

- 仅 S7-1200/1500 部分型号
- 需 PLC 端显式开启 Webserver
- 多数工控场景 Webserver 默认关闭（安全策略）

### 3.3 结论：❌ 不建议纳入 NewLife.Siemens

**理由**：
1. HTTP/JSON 与 TCP/二进制是完全不同的技术栈，强行合一违反「单一职责」
2. 寻址模型不兼容（符号名 vs 地址编码），无法统一 API
3. 西门子已提供官方 NuGet 包，重复实现价值低
4. 需求文档 §5 已明确排除

**替代建议**：
在文档中说明：若需要符号名访问或 HTTPS 加密，可直接引用西门子官方包 `Siemens.Simatic.S7.Webserver.API`，与本库并行使用（不同协议端口，互不冲突）。

---

## 4. 综合建议

| 协议 | 纳入 NewLife.Siemens | 建议方案 |
|------|:---:|------|
| S7 Classic | ✅ 已实现 | 继续维护和增强 |
| S7Plus | ❌ | 未来若有需求，新建 `NewLife.S7Plus` 独立项目 |
| Siemens Web API | ❌ | 文档推荐用户直接使用西门子官方包 |

### 保持聚焦的理由

NewLife.Siemens 的愿景是「S7 协议专用最优库」而非「西门子所有通信方式合集」。当前库在 S7 Classic 协议上已做到功能全面领先竞品，此时分散精力去覆盖与 S7 Classic 无关的协议，将稀释核心优势。

**正确的策略**是：
- 在 S7 Classic 赛道上继续深化（性能、健壮性、文档、社区）
- 让 S7Plus 和 Web API 保持为独立的、可组合的选项

---

*分析日期：2026-06-30*
*作者：NewLife 开发团队*
