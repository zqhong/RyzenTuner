## RyzenTuner

一个方便调节 Ryzen 处理器功率限制的工具，Fork 自项目 [Archeb/RyzenTuner](https://github.com/Archeb/RyzenTuner)。

![preview.jpg](https://s2.loli.net/2022/08/08/zUrw76fEuGLVZ4i.jpg)

## 添加内容

### 自动模式

1. 将功耗限制氛围三个档位：待机、平衡、高性能，根据插电/电池/夜晚不同模式，设置不同的值。
2. 档位的切换规则
  * 默认使用平衡模式
  * 符合以下条件之一，切换到待机模式
    * 条件1：白天 && 非活跃时间超过16分钟 && CPU 占用小于 10%
    * 条件2：夜晚 && 非活跃时间超过4分钟 && CPU 占用小于 20%
  * 符合以下条件，切换到高性能模式
    * CPU 超过 60% 占用

> 备注：非特殊需求，建议使用自带的睡眠功能。

## 使用

你可以直接到 [Release](https://github.com/zqhong/RyzenTuner/releases) 下载编译好的程序使用。

## 依赖工具

* [FlyGoat/RyzenAdj](https://github.com/FlyGoat/RyzenAdj)：Ryzen 移动处理器电源管理工具
  * 支持的 APU 列表：https://github.com/JamesCJ60/AMD-APU-Tuning-Utility#basic-apu-support-list
* [imbushuo/EnergyStar](https://github.com/imbushuo/EnergyStar)：Windows 进程管理，可能有增加续航的效果（未测试）
    * 需要 Windows 11 21H1 以上版本
