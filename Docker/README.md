# NewLife.Siemens Docker 开发环境

## 快速启动 S7Server 模拟器

```bash
# 构建镜像
docker build -f Dockerfile -t newlife-siemens .

# 启动 S7Server（端口 102）
docker run -d -p 102:102 --name s7server newlife-siemens

# 查看日志
docker logs -f s7server
```

## 使用 Docker Compose

```bash
docker-compose up -d
```

## 连接测试

S7Server 启动后，可用任何 S7 客户端连接 `localhost:102`：
```csharp
var client = new S7Client(CpuType.S71200, "127.0.0.1");
await client.OpenAsync();
var value = client.Read<Int16>("DB1.DBW0");
```
