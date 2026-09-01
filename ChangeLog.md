# NewLife.Siemens 版本更新记录

## v1.3.2026.0902 (2026-09-02)

### 协议与功能
- **S7 GTM 全局类型消息支持**：支持 S7-1200/1500 的全局类型消息（GTM）扩展，完善 `SetupMessage` 与 `S7Client` 的消息处理
- **密码保护连接与认证**：新增 `ProtectionLevel` 模型，支持 S7-1200/1500 的密码保护连接与认证，`S7Client`/`S7Server` 均支持，并配套 `S7PasswordTests`

### 测试与质量
- **竞品交叉兼容测试**：新增 `S7CrossCompatibilityTests`，验证与 S7netplus 的互操作性（N2 读取/写入/字节一致性/批量读写，N3 分段/位操作/TSAP 协商）
- **跨库性能对比基准**：新增 `CrossLibBenchmarks` 及地址/消息/类型转换基准，对比本库与 S7netplus 在解析、构建、读写延迟方面的性能
- **BenchmarkDotNet 基准测试**：新增 `PlcAddressBenchmarks`、`S7MessageBenchmarks`、`S7TypeConversionBenchmarks` 性能基准

### 基础设施
- **GitHub Actions CI**：新增 `.github/workflows/dotnet.yml`，push/PR 触发 ubuntu + windows 双平台构建与测试
- **Docker 开发环境**：新增 `Docker/` 目录，含 Dockerfile、docker-compose.yml 及 README

### 文档
- **英文 README**：新增 `Readme.en.md`
- **S7Plus 与 Siemens Web API 技术分析**：新增 `Doc/S7Plus与WebAPI分析.md`，深度分析后建议不做，保持 S7 Classic 定位

### Bug 修复
- **[fix]** 修复 S7netplus 交叉测试端口冲突与类型转换问题

---

## v1.2.2026.0630 (2026-06-30)

### 测试与质量
- **竞品交叉兼容测试**：新增 `S7CrossCompatibilityTests`（8 个用例），验证 NewLife.Siemens S7Server 与 S7netplus 的互操作性
  - N2 模式（S7netplus → 本库 S7Server）：连接读取、写入回读、字节级一致性、批量读写 4 个完整交叉测试
  - N3 模式（自通信）：超 PDU 分段、位操作、多 CPU 类型 TSAP 协商
- **跨库性能对比基准**：新增 `CrossLibBenchmarks`（8 项基准），对比本库与 S7netplus 在地址解析、消息构建、读写延迟方面的性能
- **S7netplus 交叉验证**：引入 S7netplus 0.20.0 作为测试与基准依赖

### 基础设施
- **GitHub Actions CI**：新增 `.github/workflows/dotnet.yml`，push/PR 触发 ubuntu + windows 双平台构建与测试
- **Docker 开发环境**：新增 `Docker/` 目录，含 Dockerfile、docker-compose.yml 及 README

### 文档
- **S7Plus 与 Siemens Web API 技术分析**：新增 `Doc/S7Plus与WebAPI分析.md`，深度分析后建议不做，保持 S7 Classic 定位
- **竞品分析报告更新**：修正 GTM 和密码保护两处过期数据（❌ → ✅），更新差距分析和行动建议
- **NuGet 包发布说明更新**：`PackageReleaseNotes` 覆盖全部新增功能

---

## v1.2.2026.0502 (2026-05-02)

### 协议与架构
- **重构 S7 协议架构**：重构底层协议实现，简化序列化与包处理逻辑，提升可维护性与可扩展性
- **修复 COTP ReadPacket 错误**：修复 COTP 包解析时的边界处理，避免读包异常

### 驱动与兼容性
- **支持字符串类型地址读取/写入**：新增对 DB 中 STRING 类型地址的读写支持（示例：DB1015.STRING0.60）
- **兼容最新 IPacket 架构与多目标框架**：适配最新 IoT/IPacket 接口，增强与 NewLife.IoT 的兼容性；支持更多目标框架发布

### 测试与质量
- **补充大量单元测试**：新增并完善单元测试覆盖，保证协议读写、TPKT/COTP/TPKTCodec 等核心模块稳定性；多条真机读写测试通过

### 其他优化
- **依赖更新与构建改进**：升级若干 NuGet 包，改善 CI 构建流程和包元数据

### Bug 修复
- **修复 WinForm UI 死锁问题**：解决同步调用异步导致的 UI 卡死问题

---
