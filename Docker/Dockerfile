# NewLife.Siemens Docker 镜像
# 基于 .NET 8.0，运行 S7Server 模拟器

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 恢复 NuGet 依赖
COPY NewLife.Siemens/*.csproj NewLife.Siemens/
RUN dotnet restore "NewLife.Siemens/NewLife.Siemens.csproj"

# 复制源码并发布
COPY . .
RUN dotnet publish "NewLife.Siemens/NewLife.Siemens.csproj" -c Release -o /app

# 运行阶段
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .

# S7 默认端口 102
EXPOSE 102

# 默认启动命令（由 docker-compose 覆盖或直接运行测试）
CMD ["dotnet", "test", "XUnitTest/XUnitTest.csproj", "--filter", "Category=CrossCompatibility"]
