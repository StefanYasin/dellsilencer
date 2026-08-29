<div align="center">

# 🌬️ dellsilencer

[![Stars](https://img.shields.io/github/stars/StefanYasin/dellsilencer?style=flat)]
[![License](https://img.shields.io/github/license/StefanYasin/dellsilencer?style=flat)]
[![Issues](https://img.shields.io/github/issues/StefanYasin/dellsilencer?style=flat)]
[![Last commit](https://img.shields.io/github/last-commit/StefanYasin/dellsilencer?style=flat)]
[![Language](https://img.shields.io/github/languages/top/StefanYasin/dellsilencer?style=flat)]

**Fan control for Dell PowerEdge servers. Quiet your rack without cooking your drives.**

</div>

SMI-based fan control for Dell PowerEdge servers, with a hysteresis (deadband) layer that stops fan oscillation at idle and low load. Where the server's own firmware keeps the fans screaming, dellsilencer takes over and keeps them sane.

## How it works

- **SMI fan control** (`SmiFanController`) - talks to the system management interface to set fan levels directly
- **WMI fan speed reads** (`WmiFanSpeedReader`) - reads current fan speed through Windows Management Instrumentation (Dell OpenManage)
- **Hysteresis layer** (`HysteresisFanController`) - decorates any controller and stops level oscillation:
  - **Deadband** - level changes smaller than the band are ignored
  - **Debounce** - minimum interval between actual hardware writes
  - **Per-fan tracking** - each fan's state is independent

```csharp
var controller = new HysteresisFanController(new SmiFanController(), deadband: 1, minInterval: TimeSpan.FromSeconds(5));
controller.SetFanLevel(FanLevel.Medium, FanIndex.Fan1);
```

## History

Originally the DellFanManagement suite (fan tools for Dell hardware); rebuilt and focused on PowerEdge servers as `dellsilencer`.

## Tests

`cd tests/HysteresisTests && dotnet test` (7/7 passing, no hardware needed).

## License

GPL-3.0
