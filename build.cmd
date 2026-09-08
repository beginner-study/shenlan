@echo off
rem 深蓝 DeepBlue 编译脚本 - 使用 Windows 内置 .NET Framework 编译器，无需安装任何 SDK
setlocal
cd /d "%~dp0"

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SPEECH=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\System.Speech.dll
if not exist "%CSC%" (
  set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
  set SPEECH=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\WPF\System.Speech.dll
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

echo [1/2] 编译中...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /out:build\DeepBlue.exe ^
  /win32icon:assets\app.ico ^
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

echo [2/2] 完成: build\DeepBlue.exe
endlocal
