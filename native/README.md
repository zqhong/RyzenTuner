请将 RyzenAdj 所需的原生运行时文件放到这个目录。

需要包含的文件：
- libryzenadj.dll
- inpoutx64.dll
- WinRing0x64.dll
- WinRing0x64.sys

本仓库当前使用的来源：
- https://github.com/FlyGoat/RyzenAdj/releases/tag/v0.19.0
- 编译时间：2026/5/13

MSBuild 会把 `native/` 目录下的 `.dll` 和 `.sys` 自动复制到编译输出目录。
常见输出目录：
- bin/Debug/
- bin/Release/
