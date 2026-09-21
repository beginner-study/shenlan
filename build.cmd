@echo off
rem 深蓝 DeepBlue 编译脚本 - 使用 Windows 内置 .NET Framework 编译器，无需安装任何 SDK
rem 2026-09-16：3D 翻页动画已移除，不再引用任何 WPF 组件（System.Speech 仍在 WPF 目录下）
setlocal
cd /d "%~dp0"

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set WPFDIR=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF
set SPEECH=%WPFDIR%\System.Speech.dll
if not exist "%CSC%" (
  set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
  set WPFDIR=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\WPF
  set SPEECH=%WPFDIR%\System.Speech.dll
)
if not exist "%CSC%" (
  echo [ERROR] 未找到 .NET Framework C# 编译器，请确认系统为 Windows 10/11。
  exit /b 1
)
if not exist "%SPEECH%" (
  echo [ERROR] 未找到 System.Speech.dll，请确认系统为 Windows 10/11。
  exit /b 1
)

if not exist build mkdir build
if not exist build\assets mkdir build\assets

echo [1/2] 编译中...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /out:build\DeepBlue.exe ^
  /win32icon:assets\app.ico ^
  /win32manifest:app.manifest ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll ^
  /r:System.Windows.Forms.dll ^
  /r:"%SPEECH%" ^
  /r:System.Web.Extensions.dll ^
  src\*.cs
if errorlevel 1 (
  echo [ERROR] 编译失败。
  exit /b 1
)

copy /y assets\app.ico build\app.ico >nul
copy /y assets\cover-forest.jpg build\assets\cover-forest.jpg >nul
copy /y assets\JFZSKSealScript-V2.5.ttf build\assets\JFZSKSealScript-V2.5.ttf >nul

echo [2/2] 完成: build\DeepBlue.exe
endlocal
