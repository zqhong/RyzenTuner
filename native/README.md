Place the required native RyzenAdj runtime files in this directory.

Expected files:
- libryzenadj.dll
- inpoutx64.dll
- WinRing0x64.dll
- WinRing0x64.sys

Source used in this repository:
- https://github.com/FlyGoat/RyzenAdj/actions/runs/21075792534
- Build time: 2025/1/17

Why this build is used:
- The official releases currently only provide v0.17.0.
- This project requires a newer libryzenadj build than the public release provides, so it uses the artifact from the workflow run above.

MSBuild copies any `.dll` and `.sys` files under `native/` to the build output directory.
Typical output folders:
- bin/Debug/
- bin/Release/
