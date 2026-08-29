<div align="center">

[![Stars](https://img.shields.io/github/stars/StefanYasin/dellsilencer?style=flat)]
[![License](https://img.shields.io/github/license/StefanYasin/dellsilencer?style=flat)]
[![Issues](https://img.shields.io/github/issues/StefanYasin/dellsilencer?style=flat)]
[![Last commit](https://img.shields.io/github/last-commit/StefanYasin/dellsilencer?style=flat)]
[![Language](https://img.shields.io/github/languages/top/StefanYasin/dellsilencer?style=flat)]

</div>

# DellFanManagement
A suite of tools for managing the fans in many Dell laptops.

See this NotebookReview thread for information about the tools contained in this project.
[DellFanManagement & DellFanKeepAlive – Tools for managing the fan speed in Dell laptops _ NotebookReview](https://web.archive.org/web/20220125130519fw_/http://forum.notebookreview.com/threads/dellfanmanagement-dellfankeepalive-%E2%80%93-tools-for-managing-the-fan-speed-in-dell-laptops.833340/)

## 🌬️ dellsilencer (formerly DellFanManagement)

**Fan control for Dell laptops** — with a new hysteresis layer (community roadmap #1):
the  decorator wraps any fan controller and stops
level oscillation:
- **Deadband** — level changes smaller than the band are ignored
- **Debounce** — minimum interval between actual hardware writes
- **Per-fan tracking** — each fan's state is independent

```csharp
var controller = new HysteresisFanController(new SmiFanController(), deadband: 1, minInterval: TimeSpan.FromSeconds(5));
controller.SetFanLevel(FanLevel.Medium, FanIndex.Fan1);
```

Tests: `cd tests/HysteresisTests && dotnet test` (7/7 passing, no hardware needed).
