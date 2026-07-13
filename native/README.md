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
