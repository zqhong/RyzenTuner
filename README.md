## RyzenTuner

一个方便调节 Ryzen 处理器功率限制的工具，Fork 自项目 [Archeb/RyzenTuner](https://github.com/Archeb/RyzenTuner)。

![preview.jpg](https://s2.loli.net/2022/08/08/94YopjVSXyQNFt8.jpg)

## 添加内容

### 自动模式

根据插电/电池/夜晚/活跃时间等不同条件，设置合适的值。具体请看 `RyzenTuner.Form1::AutoModePowerLimit`。

> 备注：非特殊需求，建议使用自带的睡眠功能。

## 使用

你可以直接到 [Release](https://github.com/zqhong/RyzenTuner/releases) 下载编译好的程序使用。

## 计划

- 界面
  - [ ] 显示软件版本信息
  - [ ] 仅在支持 EnergyStar 的时候显示方框
- [ ] 预设各个模式下的配置参数
- [ ] 多语言支持
- [ ] GitHub Action 自动编译
- [ ] .NET MAUI 重写，支持 Windows 和 Linux

## 依赖

* [FlyGoat/RyzenAdj](https://github.com/FlyGoat/RyzenAdj)：Ryzen 移动处理器电源管理工具
  * 支持的 APU 列表：https://github.com/JamesCJ60/AMD-APU-Tuning-Utility#basic-apu-support-list
* [imbushuo/EnergyStar](https://github.com/imbushuo/EnergyStar)：Windows 进程管理，可能有增加续航的效果（未测试）
    * 需要 Windows 11 21H1 以上版本
* [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)：获取硬件信息
