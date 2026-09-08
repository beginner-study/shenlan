; 深蓝 DeepBlue - Inno Setup 安装脚本
; 用法：安装 Inno Setup 6（https://jrsoftware.org/isinfo.php）后，
;      用 ISCC.exe 编译本脚本生成标准安装包：
;      "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\shenlan.iss
; 生成的安装包输出到 build\ 目录。

#define MyAppName "深蓝 DeepBlue"
#define MyAppNameEn "DeepBlue"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "DeepBlue Project"
#define MyAppExeName "DeepBlue.exe"

[Setup]
AppId={{8B7C4A2E-1D3F-4E5A-9C6B-7A8D9E0F1A2B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\DeepBlue
DefaultGroupName={#MyAppName}
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputBaseFilename=DeepBlue-Setup-{#MyAppVersion}
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
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "..\build\DeepBlue.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; 卸载时清理应用目录中的临时文件（用户数据在 %APPDATA%\DeepBlue，不会自动删除）
Type: files; Name: "{app}\selftest.txt"
