## RyzenTuner

一个方便调节 Ryzen 移动处理器功率限制的工具。

![preview.jpg](https://s2.loli.net/2022/08/09/6lcTD5U93g2y4Am.jpg)

## 添加内容

### 自动模式

根据插电/电池/夜晚/活跃时间等不同条件，选择不同的工作模式（待机/省电/平衡/性能）。

工作模式说明：

* 待机模式（SleepMode）：处于最低能耗状态，只能应对超低负载的工作
* 省电模式（PowerSaveMode）：应对轻松的工作，比如文字输入、网页浏览
* 平衡模式（BalancedMode）：能耗比最高的状态，应对突发的任务，比如打开网页、Photoshop
* 性能模式（PerformanceMode）：应对需要高性能的场景，比如编译软件、玩大型游戏、渲染等
* 手动模式（CustomMode）：手动配置

## 使用

你可以直接到 [Release](https://github.com/zqhong/RyzenTuner/releases) 下载编译好的程序使用。

## 计划

- [ ] 多语言支持
- [ ] GitHub Action 自动编译
- [ ] .NET MAUI 重写，支持 Windows 和 Linux

## 感谢

* [Archeb/RyzenTuner](https://github.com/Archeb/RyzenTuner)：基于该项目开发
* [FlyGoat/RyzenAdj](https://github.com/FlyGoat/RyzenAdj)：Ryzen 移动处理器电源管理工具
  * 使用版本：v0.11.0
* [imbushuo/EnergyStar](https://github.com/imbushuo/EnergyStar)：Windows 进程管理，可能有增加续航的效果（未测试）
  * 需要 Windows 11 21H1 以上版本
  * 使用版本：v1.0.0（https://github.com/JasonWei512/EnergyStar/actions/runs/2773330296）
* [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)：获取硬件信息