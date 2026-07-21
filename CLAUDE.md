# CLAUDE.md

## 构建

.NET Framework 4.8 WinForms 项目，使用 MSBuild 构建（NuGet 包通过 `packages.config` 管理，非 PackageReference）：

```bash
# MSBuild 路径：若使用 Rider，可在 Settings → Build、Execution、Deployment → Toolset 查看路径
# 也可使用 Visual Studio Developer Command Prompt 中的 msbuild 命令
MSBUILD_EXE="/c/Program Files/JetBrains/JetBrains Rider 2026.1.4/tools/MSBuild/Current/Bin/amd64/MSBuild.exe"
nuget restore RyzenTuner.sln
"$MSBUILD_EXE" "RyzenTuner.csproj" "//t:Rebuild" "//p:Configuration=Debug" "//nologo" "//verbosity:minimal"
```

Release 构建：
```bash
nuget restore RyzenTuner.sln
"$MSBUILD_EXE" "RyzenTuner.csproj" "//t:Rebuild" "//p:Configuration=Release" "//nologo" "//verbosity:minimal"
```

发布打包（生成 zip）：
```bash
# 手动打包 bin\Release 目录下的所有文件（含 native DLL）
cd bin/Release
zip -r ../../RyzenTuner-{version}.zip .
```

CI：`.github/workflows/debug_build.yml` — GitHub Actions 使用 `microsoft/setup-msbuild` + `nuget restore`，手动触发时可选日志级别。

## 项目架构

### 概述

RyzenTuner 是 AMD Ryzen 移动处理器的功耗管理 GUI 工具。它通过调用 `libryzenadj.dll` 调整 CPU TDP 限制，并结合 Windows 进程 QoS/优先级调整（EnergyStar）来优化续航和降低风扇噪音。

### 核心循环

`MainForm` 的 `mainFormTimer_Tick` 每 ~2 秒驱动（Timer Interval = 2048ms），其中：

1. **硬件监控** — `HardwareMonitor` 轮询 CPU 占用、封装功耗、温度、频率、核显 3D 占用
2. **功耗控制** — `DoPowerLimit()` 通过 ryzenadj API 设置 STAPM/Slow/Fast PPT 限制和 Tctl/Tdie 温度上限。每 tick 检查是否达到 `PowerLimitUpdateInterval`（默认 4 秒）间隔，达到时才执行
3. **进程管理** — `DoProcessManage()` 实现 EnergyStar：前台进程提升 QoS，后台进程降级至 EcoQoS + BelowNormal 优先级

### 目录结构

```
RyzenTuner/
├── Program.cs                  # 应用程序入口点（Application.Run）
├── app.manifest                # 管理员权限声明（requireAdministrator）
├── Common/                     # 核心业务逻辑
│   ├── Benchmark/              # 能效分析跑分引擎
│   │   ├── BenchmarkEngine.cs    # 跑分控制循环（逐档 TDP → 采集 → 统计）
│   │   ├── BenchmarkWorkload.cs  # CPU 密集负载（xorshift64，零内存分配）
│   │   └── BenchmarkModels.cs    # 配置、结果模型
│   ├── Container/              # 轻量 DI 容器（基于 Microsoft MinIoC）
│   │   ├── AppContainer.cs       # 静态服务定位器
│   │   └── Container.cs          # MinIoC 容器基类
│   ├── EnergyStar/             # Windows 进程 QoS/优先级管理
│   │   ├── EnergyManager.cs      # 进程 QoS/优先级控制核心
│   │   └── Interop/              # Win32 P/Invoke 声明
│   │       └── Win32Api.cs
│   ├── Hardware/               # 硬件传感器监控（LibreHardwareMonitorLib 封装）
│   │   ├── HardwareMonitor.cs    # 传感器轮询核心
│   │   └── UpdateVisitor.cs      # 传感器数据更新访问器
│   ├── Logger/                 # 日志系统
│   │   ├── LogModels.cs          # 日志条目模型（LogEntry）
│   │   └── SqliteLogger.cs       # SQLite 日志器
│   ├── Processor/              # RyzenAdj P/Invoke 绑定 + AMDProcessor 封装
│   │   ├── AMDProcessor.cs       # ryzenadj 调用封装
│   │   └── RyzenAdj.cs           # P/Invoke 声明
│   ├── Settings/               # SQLite 设置存储
│   │   ├── AppSettings.cs        # 静态 API：Get<T>() / Set()，缓存 + 即时持久化
│   │   └── SettingsDatabase.cs   # DB 路径、迁移、连接字符串（含 WAL + BusyTimeout）
│   ├── Theme/                  # 深色模式支持
│   │   └── ThemeManager.cs       # 主题切换（深色/浅色）
│   ├── Awake.cs                # 阻止系统睡眠（SetThreadExecutionState）
│   ├── PowerConfig.cs          # Windows 电源计划管理（CPU Boost 开关）
│   └── StartupTaskScheduler.cs
├── UI/                         # WinForms 界面
│   ├── MainForm.cs              # 主窗口 + 托盘图标 + 定时器驱动核心（内嵌设置页、跑分页、关于页）
│   ├── MainForm.Designer.cs     # 设计器布局代码
│   ├── MainForm.resx            # 表单布局资源
│   └── MainForm.en.resx         # 英文表单布局资源
├── Utils/
│   ├── RyzenTunerUtils.cs       # 功率值解析、空闲检测（GetLastInputInfo）、通知文本、模式名本地化
│   ├── RyzenAdjUtils.cs         # 功率计算、温度限制读取
│   └── CommonUtils.cs           # 锁屏检测、夜间判断、字体检查
├── Properties/
│   ├── AssemblyInfo.cs           # 程序集元数据
│   ├── Resources.resx            # 图标、图片等二进制资源
│   ├── Resources.en.resx         # 英文回退
│   ├── Resources.Designer.cs     # 资源代码隐藏
│   ├── Strings.resx              # 中文 UI 字符串
│   ├── Strings.en.resx           # 英文字符串回退
│   └── Strings.Designer.cs       # 字符串代码隐藏
├── Resources/
│   └── phoenix-96.ico            # 应用程序图标
└── native/                     # 原生 DLL（构建时自动复制到输出目录根目录）
    ├── libryzenadj.dll         # AMD SMU 通信库（v0.19.0）
    ├── inpoutx64.dll           # 端口 I/O
    ├── WinRing0x64.dll/.sys    # 硬件驱动
    └── SQLite.Interop.dll      # System.Data.SQLite 原生运行时（备份参考，实际由 NuGet 包部署）
```

### 关键依赖

- **libryzenadj** (v0.19.0) — 通过 SMU 设置 AMD CPU TDP 限制。所有 `set_*` / `get_*` 方法通过 P/Invoke 绑定在 `RyzenAdj.cs`
- **LibreHardwareMonitorLib** (0.9.0) — 读取 CPU/GPU 传感器数据
- **Vanara** (3.4.6) — 封装 Windows 电源管理 API（PowrProf、Kernel32）
- **System.Data.SQLite** (1.0.119) — SQLite 数据库引擎（设置存储 + 日志）

### 功耗模式

| 模式 | 说明 |
|---|---|
| PowerSaveMode | 省电模式（16W 默认） |
| BalancedMode | 平衡模式（26W 默认） |
| PerformanceMode | 性能模式（45W 默认） |

所有模式的功率值可通过设置页自定义（1W–100W）。

### DI 容器

`AppContainer` 是静态服务定位器，持有 5 个单例服务：
- `HardwareMonitor` — 硬件传感器读取（LibreHardwareMonitorLib）
- `PowerConfig` — Windows 电源方案管理（CPU Boost 开关）
- `AmdProcessor` — ryzenadj 调用封装，含 CPU 家族检测和 TDP 设置
- `EnergyManager` — 进程 QoS/优先级管理（EcoQoS + BelowNormal）
- `SqliteLogger` — SQLite 日志（替代了 SimpleLogger 文件日志）

容器基于 Microsoft MinIoC，支持单例和 per-scope 注册。

### 错误恢复机制

`MainForm.DoPowerLimit()` 有重入保护和错误恢复：
- `_isApplyingPowerLimit` 防止重入
- 错误后 15 秒冷却期（跳过全部 I/O 操作）
- 持续错误时自动切到 PerformanceMode 作为安全回退
- 恢复后自动还原用户原始模式

### 能效分析跑分（Benchmark）

`BenchmarkEngine` 逐档遍历 TDP 范围（如 10W→45W，步进 5W），每档：
1. 设置 PPT 限制（STAPM/Slow/Fast = 同一值）
2. 等待系统稳定（用户设定的秒数）
3. 运行 `BenchmarkWorkload`（xorshift64 PRNG 循环，零分配纯 ALU 密集型负载，AboveNormal 优先级）
4. 同步采集传感器数据（功耗/温度/频率，500ms 间隔）
5. 计算统计量（均值/中位数/最大/最小）和能效比（Score/PowerAvg）

支持单核/多核测试。跑分结果自动缩放（控制在五位数以内）以确保可读性。结果可导出 CSV。

### EnergyStar 进程管理

`EnergyManager` 通过 Win32 `SetProcessInformation`（ProcessPowerThrottling）和 `SetPriorityClass` 控制进程：

- **前台进程** → 取消节流（HighQoS + Normal Priority）
- **后台进程** → 节流（EcoQoS + BelowNormal Priority）
- **UWP 应用** → 特殊处理：检测 `ApplicationFrameHost.exe`，找到其子窗口对应的真实进程
- **白名单** — `_bypassProcessList` 包含系统进程、IDE、编辑器、浏览器等，可通过 `EnergyStarBypassProcessList` 设置扩展

### 本地化

通过 .resx 文件实现中英文双语言。语言选择优先级：用户设置 > 系统语言检测。用户可在设置页选择语言（保存后即时生效），未设置时自动检测：`zh-*` 文化使用中文，其余使用英文。

.resx 文件命名规则：
- `Resources.resx` + `Resources.en.resx`（图标、图片等二进制资源）
- `Strings.resx` + `Strings.en.resx`（全局字符串）
- `MainForm.resx` + `MainForm.en.resx`（窗口布局）

### 命令行参数

- `-hide` — 启动时隐藏主窗口到托盘（用于开机自启）

### 可用 Skill

- `/check-fix` — 开发/修复 bug 后，持续 code-review + 编译修复，直到零错误

## 构建说明

- 需要 .NET Framework 4.8 SDK 或对应 MSBuild
- NuGet 包通过 `packages.config` 管理（不是 PackageReference）
- C# 9.0 + Nullable enabled
- 运行需要管理员权限（`requireAdministrator`），因为需要 SMU 通信和修改进程优先级
- 原生 DLL 位于 `native/` 目录，构建时 MSBuild 自动复制到输出目录根目录（`<Link>` 展平）
- 系统要求：Microsoft Visual C++ 2015-2022 Redistributable (x64)
