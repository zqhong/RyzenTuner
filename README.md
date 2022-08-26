## RyzenTuner

English | [简体中文](README-CN.md)

A handy tool for adjusting the power limit of Ryzen mobile processors.

![preview.jpg](https://s2.loli.net/2022/08/25/YTA9yf8jqOtUEwn.jpg)

## Feature

### Automatic Mode

Select different working modes (standby/balance/performance) according to different conditions such as
plug-in/battery/night/active time.

Description of working mode:

* Standby mode: in the lowest energy consumption state, can only deal with ultra-low load work
* Balanced Mode: The state with the highest energy consumption ratio, to deal with sudden tasks, such as opening web
  pages, Photoshop
* Performance Mode: for scenarios that require high performance, such as compiling software, playing large games,
  rendering, etc.

## Usage

You can directly go to [Release](https://github.com/zqhong/RyzenTuner/releases) to download the compiled program and use
it.

## FAQ

### How to modify the power of other modes such as standby mode

Close `RyzenTuner`. Open the `RyzenTuner.exe.config` file and modify the corresponding parameters. For example, `SleepMode` is the sleep mode, modify the value in it.

Example: Change sleep mode from 1 W to 2 W

Before update

```xml

<setting name="SleepMode" serializeAs="String">
    <value>1</value>
</setting>
```

After update

```xml

<setting name="SleepMode" serializeAs="String">
    <value>2</value>
</setting>
```

## Give a Star! ⭐

If you like or are using this project, please give it a star. Thanks!

[![Star History Chart](https://api.star-history.com/svg?repos=zqhong/RyzenTuner&type=Date)](#RyzenTuner)

## Grateful

* [Archeb/RyzenTuner](https://github.com/Archeb/RyzenTuner): Developed based on this project
* [FlyGoat/RyzenAdj](https://github.com/FlyGoat/RyzenAdj): Ryzen mobile processor power management tool
* [imbushuo/EnergyStar](https://github.com/imbushuo/EnergyStar): Windows process scheduling, may have the effect of increasing battery life (not tested)
* [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor): Get hardware information
* [dahall/Vanara](https://github.com/dahall/Vanara): A set of .NET libraries that wraps many PInvoke calls to Windows native APIs