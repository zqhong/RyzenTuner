请将 RyzenAdj 所需的原生运行时文件放到此目录。

需要包含的文件：
- `libryzenadj.dll`
- `inpoutx64.dll`
- `WinRing0x64.dll`
- `WinRing0x64.sys`

> **重要**：这些文件在构建后会被 MSBuild 复制到输出目录的根目录（与 `RyzenTuner.exe` 同级），而不是 `native/` 子目录。如果手动部署，请确保它们与 exe 在同一目录，否则运行时会找不到 DLL。

系统要求：
- **Microsoft Visual C++ 2015-2022 Redistributable (x64)**（`libryzenadj.dll` 依赖的运行时）。如果启动时提示「加载 libryzenadj.dll 失败」或「找不到指定的模块」，请安装：
  https://aka.ms/vs/17/release/vc_redist.x64.exe

本仓库当前使用的来源：
- https://github.com/FlyGoat/RyzenAdj/releases/tag/v0.19.0
- 编译时间：2026/5/13

---

### SQLite.Interop.dll

此目录中的 `SQLite.Interop.dll` 是 `System.Data.SQLite` 的原生运行时库。

- **实际部署依赖 NuGet 包**：`Stub.System.Data.SQLite.Core.NetFramework` 包的 MSBuild `.targets` 会将其自己的 `SQLite.Interop.dll` 复制到输出目录的 `x64/` 子目录。`System.Data.SQLite` 从该子目录加载原生库。
- **此目录中的副本仅供参考/备份**：MSBuild `<Content>` 通配符会将其扁平复制到输出根目录，但 `System.Data.SQLite` 并不会从根目录加载它。
- 如需更新版本，请同时更新 NuGet 包与此文件（保持 x64 架构一致）。
