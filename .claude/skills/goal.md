---
name: goal
description: 持续 code-review 和编译修复，直到没有错误
---

在修改代码后，执行以下两步循环，直到全部通过：

## 步骤 1：code-review

运行 `/code-review high`，检查当前 diff 是否有 correctness 问题。

- 如果 code-review 返回了 findings（非空），逐个修复，然后重新运行 code-review
- 重复直到 code-review 返回空结果（无 findings）

## 步骤 2：编译验证

使用 MSBuild 执行完整重新编译（`//t:Rebuild`），查看编译结果：

```
MSBUILD_EXE="/c/Program Files/JetBrains/JetBrains Rider 2025.2.6/tools/MSBuild/Current/Bin/amd64/MSBuild.exe"
"$MSBUILD_EXE" "RyzenTuner.csproj" "//t:Rebuild" "//p:Configuration=Debug" "//nologo" "//verbosity:minimal"
```

- 如果有 warning 或 error，修复后回到**步骤 1**
- 重复直到 `0 个警告 0 个错误`

## 循环终止条件

当一轮循环中 code-review 无 findings 且编译零警告零错误时，任务完成，可以 git commit。
