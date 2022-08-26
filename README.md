## RyzenTuner

一个方便调节 Ryzen 移动处理器功率限制的工具。

![preview.jpg](https://s2.loli.net/2022/08/25/YTA9yf8jqOtUEwn.jpg)

## 添加内容

### 自动模式

根据插电/电池/夜晚/活跃时间等不同条件，选择不同的工作模式（待机/平衡/性能）。

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

- [ ] 多语言支持
- [ ] 添加设置面板
- [ ] 添加是否允许修改电源计划的选项，默认关闭

## 感谢

* [Archeb/RyzenTuner](https://github.com/Archeb/RyzenTuner)：基于该项目开发
* [FlyGoat/RyzenAdj](https://github.com/FlyGoat/RyzenAdj)：Ryzen 移动处理器电源管理工具
* [imbushuo/EnergyStar](https://github.com/imbushuo/EnergyStar)：Windows 进程调度，可能有增加续航的效果（未测试）
* [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)：获取硬件信息
* [dahall/Vanara](https://github.com/dahall/Vanara)：一套 .NET 库，包含了许多 Windows 原生 API 的 PInvoke 调用的封装
