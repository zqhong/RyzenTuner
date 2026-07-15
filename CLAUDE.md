# RyzenTuner

## 编译

.NET Framework 4.8 WinForms 项目，使用 MSBuild 编译：

```bash
MSBUILD_EXE="/c/Program Files/JetBrains/JetBrains Rider 2025.2.6/tools/MSBuild/Current/Bin/amd64/MSBuild.exe"
"$MSBUILD_EXE" "RyzenTuner.csproj" "//t:Rebuild" "//p:Configuration=Debug" "//nologo"
```

## 可用 Skill

- `/check-fix` — 开发/修复 bug 后，持续 code-review + 编译修复，直到零错误
