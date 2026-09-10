; 深蓝语音增强包 - Inno Setup 安装脚本
; 内容：NaturalVoiceSAPIAdapter（MIT，x64）+ 微软晓晓自然语音（本地离线）
; 作用：安装后任何支持 SAPI5 的程序（含深蓝）都可使用晓晓神经语音
;
; 用法：先运行 installer\prep_voice.ps1 准备 build\voice-payload\ 目录，
;      再用 ISCC.exe 编译本脚本生成独立语音包：
;      "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\voice.iss

#define MyAppName "深蓝语音增强包"
#define MyAppNameEn "DeepBlueVoice"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "DeepBlue Project"
#define VoicePayload "..\build\voice-payload"

[Setup]
AppId={{6E2F9B4C-8A51-4D7E-B3C0-5F9D2E8A1C47}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
; 目录名必须为纯 ASCII 字符（适配器要求语音路径不含非 ASCII 字符）
DefaultDirName={autopf}\DeepBlueVoice
DefaultGroupName={#MyAppName}
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\Installer.exe
OutputBaseFilename=DeepBlue-Voice-{#MyAppVersion}
OutputDir=..\build
SetupIconFile=..\assets\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableProgramGroupPage=yes

[Languages]
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 适配器（含运行库 DLL 与自然语音模型目录）
Source: "{#VoicePayload}\x64\*"; DestDir: "{app}\x64"; Flags: ignoreversion recursesubdirs createallsubdirs
; 适配器自带配置界面（更改在线语音等设置时使用，无需管理员）
Source: "{#VoicePayload}\Installer.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#VoicePayload}\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
; 关闭 Edge 在线语音，保持完全离线；同时显式指定本地语音路径（与默认值一致）
Root: HKCU; Subkey: "Software\NaturalVoiceSAPIAdapter\Enumerator"; ValueType: dword; ValueName: "NoEdgeVoices"; ValueData: "1"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\NaturalVoiceSAPIAdapter\Enumerator"; ValueType: string; ValueName: "NarratorVoicePath"; ValueData: "{app}\x64\NarratorVoices"; Flags: uninsdeletevalue

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\Installer.exe"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\Installer.exe"; Tasks: desktopicon

[Run]
; 注册 SAPI5 语音引擎（64 位）
Filename: "{sys}\regsvr32.exe"; Parameters: "/s ""{app}\x64\NaturalVoiceSAPIAdapter.dll"""; Flags: runhidden
Filename: "{app}\Installer.exe"; Description: "打开语音设置（可选）"; Flags: nowait postinstall skipifsilent unchecked

[UninstallRun]
Filename: "{sys}\regsvr32.exe"; Parameters: "/u /s ""{app}\x64\NaturalVoiceSAPIAdapter.dll"""; Flags: runhidden; RunOnceId: "UnregAdapter"
