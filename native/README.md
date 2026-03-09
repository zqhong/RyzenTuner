Place the required native RyzenAdj runtime files in this directory.

Expected files:
- libryzenadj.dll
- inpoutx64.dll
- WinRing0x64.dll
- WinRing0x64.sys

MSBuild copies any `.dll` and `.sys` files under `native/` to the build output directory.
Typical output folders:
- bin/Debug/
- bin/Release/
