请将 RyzenAdj 所需的原生运行时文件放到这个目录。

需要包含的文件：
- libryzenadj.dll
- inpoutx64.dll
- WinRing0x64.dll
- WinRing0x64.sys

本仓库当前使用的来源：
- https://github.com/FlyGoat/RyzenAdj/actions/runs/21075792534
- 编译时间：2026/1/27

使用这个构建产物的原因：
- 官方 release 目前只有 v0.17.0。
- 本项目需要比公开 release 更新的 libryzenadj，因此改用上述 GitHub Actions 产物。

MSBuild 会把 `native/` 目录下的 `.dll` 和 `.sys` 自动复制到编译输出目录。
常见输出目录：
- bin/Debug/
- bin/Release/
