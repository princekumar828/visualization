# Frontend Projects

## Projects Overview

| Project | Render Mode | Port | Purpose |
|---------|-------------|------|---------|
| ChartTestFramework.Server | Server-Side (SSR) | 5000 | SignalR-based rendering |
| ChartTestFramework.Wasm | Client-Side (CSR) | 5001 | WebAssembly rendering |
| ChartTestFramework.Shared | N/A | N/A | Shared components |

## Build All Projects
```bash
dotnet build ChartTestFramework.sln
```

## Run Tests Side by Side
```bash
# Terminal 1
cd ChartTestFramework.Server && dotnet run --urls "http://localhost:5000"

# Terminal 2
cd ChartTestFramework.Wasm && dotnet run --urls "http://localhost:5001"
```

## SSR vs CSR Comparison

| Aspect | Server (SSR) | WASM (CSR) |
|--------|--------------|------------|
| Initial Load | Fast | Slower (download .NET runtime) |
| Interactivity | SignalR latency | Immediate |
| Memory Usage | Server | Client browser |
| Offline Support | ❌ | ✅ |
| Best For | Complex processing | Responsive UI |
