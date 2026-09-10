# 深蓝 DeepBlue

一款运行于 Windows 的本地语音播报软件。打开软件即自动获取日期与日程数据，一键语音播报你今天的安排与即将截止的重要事项。完全本地运行，无需联网，不采集任何数据。

## 功能特性

- **启动即就绪**：打开软件后 2 秒内自动完成日期与日程数据加载，无需手动刷新
- **语音播报**：逐句高亮显示当前播报进度，支持暂停 / 继续 / 停止；默认使用 Windows 系统语音，安装[语音增强包](#语音增强包可选)后可使用接近真人的「晓晓」自然语音
- **今日安排**：播报你今天录入的全部事项，按时间升序朗读，重要事项（P0）会加上「重要」前缀
- **截止提醒**：播报未来 N 天内（可设置 1–30 天）优先级为 P0 / P1 的截止事项，自动忽略 P2 及以下
- **重复日程**：支持每天 / 每周 / 每月三种重复规则，无需反复录入
- **个性化设置**：语音选择、语速（0.5x–2.0x）、提醒窗口天数、播报段落开关均可自定义
- **数据本地存储**：所有数据保存在 `%APPDATA%\DeepBlue` 目录下的 JSON 文件中，可随时备份或迁移
- **跨天自动刷新**：跨越零点继续使用时，自动检测数据过期并重新获取

## 系统要求

- Windows 10 / Windows 11（64 位或 32 位）
- .NET Framework 4.8+（Windows 10 1903 及以上版本系统自带，无需额外安装）
- 至少一个系统语音（中文系统默认自带；推荐在「设置 → 时间和语言 → 语音」中安装中文语音包以获得更好效果）

## 下载与安装

1. 前往本仓库的 [Releases](../../releases) 页面
2. 下载最新版本的 `DeepBlue-Setup-x.x.x.exe`
3. 双击运行安装向导，按提示完成安装（可选择是否创建桌面快捷方式）
4. 安装完成后从开始菜单或桌面启动「深蓝 DeepBlue」

卸载：通过「设置 → 应用 → 已安装的应用」或控制面板中的「深蓝 DeepBlue」条目卸载。卸载不会删除 `%APPDATA%\DeepBlue` 中的个人数据，如需彻底清理请手动删除该目录。

## 语音增强包（可选）

系统自带语音（如惠惠）较为机械。语音增强包为深蓝接入微软「晓晓」自然语音——神经网络合成，接近真人，**本地离线运行，无需联网**。

**安装**：

1. 在 [Releases](../../releases) 页面下载 `DeepBlue-Voice-x.x.x.exe`（约 56 MB）
2. 双击运行安装向导，完成即生效（与主程序先后顺序无关）
3. 启动深蓝，语音将自动默认为「晓晓」，也可在「设置 → 语音」中切换

**说明**：

- 增强包与主程序相互独立：卸载增强包后深蓝自动回退系统语音，不影响日程数据
- 增强包基于开源项目 [NaturalVoiceSAPIAdapter](https://github.com/gexgd0419/NaturalVoiceSAPIAdapter)（MIT 协议），将 Windows 讲述人的自然语音桥接为标准 SAPI5 引擎，其他支持 SAPI5 的软件同样可以使用
- 晓晓语音数据版权归属微软，仅供个人使用，请勿二次分发（详见安装目录内 `THIRD-PARTY-NOTICES.txt`）
- 安装目录 `C:\Program Files\DeepBlueVoice` 请勿移动或重命名
- 属非官方方案，极少数情况下可能随 Windows 更新失效，届时重新安装新版增强包即可

**从源码构建语音包**：运行 `installer\prep_voice.ps1`（下载适配器与语音包并组装载荷），再用 Inno Setup 编译 `installer\voice.iss`。

## 使用方法

1. 启动软件，日期卡片与数据就绪状态会自动加载
2. 点击「管理日程」录入你的安排：
   - **日程**：带具体日期（或重复规则）与时间的事项，如「周三 9:30 团队周会」
   - **截止**：带截止日期的事项，如「9 月 10 日前提交季度报告」，可设置优先级 P0–P3
   - 每条事项支持添加备注，备注内容也会被播报
3. 回到主界面，点击「开始新的一天」，深蓝将语音播报：问候语 + 日期星期 → 今日安排 → 截止提醒 → 结束语
4. 播报过程中可随时暂停、继续或停止

## 从源码构建

本项目不依赖任何第三方库或 SDK，仅使用 Windows 内置的 .NET Framework C# 编译器：

```cmd
git clone https://github.com/beginner-study/shenlan.git
cd shenlan
build.cmd
```

编译产物输出至 `build\DeepBlue.exe`，可直接运行。

生成标准安装包（可选）：安装 [Inno Setup 6](https://jrsoftware.org/isinfo.php) 后执行：

```cmd
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\shenlan.iss
```

安装包输出至 `build\DeepBlue-Setup-x.x.x.exe`（约 2 MB）。

## 运行自测

源码内置 36 项逻辑自测（覆盖问候语、时间口语化、重复规则、优先级过滤、播报稿生成、默认语音选择、存储读写等），构建脚本完成后会自动运行：

```cmd
build.cmd
type build\selftest.txt
```

## 项目结构

```
shenlan/
├── src/                  # C# 源码（WinForms，.NET Framework 4.8）
│   ├── Program.cs        # 入口：单实例检测
│   ├── MainForm.cs       # 主界面：启动自动取数、播报控制
│   ├── ScheduleForm.cs   # 日程管理：增删改查、重复规则、优先级
│   ├── SettingsForm.cs   # 设置：语音、语速、提醒窗口、段落开关
│   ├── ScriptEngine.cs   # 播报稿引擎：日期/今日/截止三段式脚本生成
│   ├── BroadcastEngine.cs# TTS 播报引擎：逐句播报、暂停/继续/停止、默认语音选择
│   ├── Models.cs         # 数据模型与 JSON 本地存储
│   ├── SelfTest.cs       # 内置自测框架
│   └── Ui.cs             # UI 样式辅助
├── assets/app.ico        # 应用图标
├── installer/
│   ├── shenlan.iss       # 主程序安装脚本
│   ├── voice.iss         # 语音增强包安装脚本
│   ├── prep_voice.ps1    # 语音增强包载荷准备脚本
│   └── THIRD-PARTY-NOTICES.txt
└── build.cmd             # 一键编译脚本
```

## 路线图

- ~~更自然的语音~~：V1.1 已提供可选语音增强包（晓晓自然语音，本地离线）
- **V1.2+**：天气数据模块（独立可选模块，接入天气数据源后播报天气段；V1.0 中设置页的天气项已预留并置灰）
- 开机自启动选项
- 更多播报场景（如晚安回顾）

## 许可证

[MIT License](./LICENSE)
