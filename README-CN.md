## RyzenTuner

[English](README.md) | 简体中文

RyzenTuner 提供一个 GUI 界面，可以方便调节 Ryzen 移动处理器的功率限制，也支持调整 Windows 进程的 QoS 等级和优先级，从而提升电池使用时间和减少风扇噪声。

![preview.jpg](https://s2.loli.net/2022/08/25/YTA9yf8jqOtUEwn.jpg)

## 添加内容

### 自动模式

根据插电/电池/夜晚/活跃时间等不同条件，自动选择不同的工作模式（待机/平衡/性能）。

工作模式说明：

* 待机模式（SleepMode）：处于最低能耗状态，只能应对超低负载的工作
* 平衡模式（BalancedMode）：能耗比最高的状态，应对突发的任务，比如打开网页、Photoshop
* 性能模式（PerformanceMode）：应对需要高性能的场景，比如编译软件、玩大型游戏、渲染等

## 使用

你可以直接到 [Release](https://github.com/zqhong/RyzenTuner/releases) 下载编译好的程序使用。

## FAQ

### 如何修改待机模式等其他模式的功率

关闭 `RyzenTuner`。打开 `RyzenTuner.exe.config` 文件，修改对应的参数。比如 `SleepMode` 就是 睡眠模式，修改其中的 value 值。

示例：将睡眠模式从 1 W 改为 2 W

修改前

```xml

<setting name="SleepMode" serializeAs="String">
    <value>1</value>
</setting>
```

修改后

```xml

<setting name="SleepMode" serializeAs="String">
    <value>2</value>
</setting>
```

## 计划

- [x] 多语言支持
- [x] GitHub action
- [ ] 添加设置面板
- [ ] 添加是否允许修改电源计划的选项，默认关闭

## Give a Star! ⭐

如果你喜欢或正在使用这个项目，请给我们一个 star。一个小小的 star 是作者更新项目/回答问题的动力！🤝

[![Star History Chart](https://api.star-history.com/svg?repos=zqhong/RyzenTuner&type=Date)](#RyzenTuner)

## 依赖的项目

* [Archeb/RyzenTuner](https://github.com/Archeb/RyzenTuner)：基于该项目开发
* [FlyGoat/RyzenAdj](https://github.com/FlyGoat/RyzenAdj)：Ryzen 移动处理器电源管理工具
* [imbushuo/EnergyStar](https://github.com/imbushuo/EnergyStar)：Windows 进程调度，可能有增加续航的效果（未测试）
* [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)：获取硬件信息
* [dahall/Vanara](https://github.com/dahall/Vanara)：一套 .NET 库，包含了许多 Windows 原生 API 的 PInvoke 调用的封装

感谢以上名单的项目和作者。

## License

RyzenTuner 使用 [MIT](LICENSE.md) 开源协议。