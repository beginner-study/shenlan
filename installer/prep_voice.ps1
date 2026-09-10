# 深蓝语音增强包 - 载荷准备脚本
# 从官方发布源下载 NaturalVoiceSAPIAdapter 与晓晓语音包，组装 Inno 编译所需的目录结构。
# 产物: build\voice-payload\{x64\..., Installer.exe, THIRD-PARTY-NOTICES.txt}
#
# 用法: powershell -ExecutionPolicy Bypass -File installer\prep_voice.ps1
# 可选: -Proxy http://127.0.0.1:7897

param(
    [string]$Proxy = "",
    [string]$AdapterVersion = "0.2.9",
    [string]$VoiceVersion = "1.0.9.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$work = Join-Path $root "build\voice-src"
$payload = Join-Path $root "build\voice-payload"
$adapterZip = "https://github.com/gexgd0419/NaturalVoiceSAPIAdapter/releases/download/v$AdapterVersion/NaturalVoiceSAPIAdapter_v${AdapterVersion}_x86_x64.zip"
$voiceMsix = "https://dl.nvdacn.com/NVDA-Addons/TTS/NaturalVoices/MicrosoftWindows.Voice.zh-CN.Xiaoxiao.1_${VoiceVersion}_x64__cw5n1h2txyewy.Msix"

function Get-File($url, $dst) {
    if (Test-Path $dst) { Write-Output "已存在: $dst"; return }
    Write-Output "下载: $url"
    if ($Proxy) {
        Invoke-WebRequest -Uri $url -OutFile $dst -UseBasicParsing -Proxy $Proxy
    } else {
        Invoke-WebRequest -Uri $url -OutFile $dst -UseBasicParsing
    }
}

New-Item -ItemType Directory -Force -Path $work, $payload | Out-Null

# 1. 适配器（MIT 协议，x64 部分）
Get-File $adapterZip (Join-Path $work "adapter.zip")
$adapterDir = Join-Path $work "adapter"
if (-not (Test-Path (Join-Path $adapterDir "x64\NaturalVoiceSAPIAdapter.dll"))) {
    Expand-Archive -Path (Join-Path $work "adapter.zip") -DestinationPath $adapterDir -Force
}

# 2. 晓晓自然语音（MSIX 按 ZIP 解压使用；版本必须为最后可用的 1.0.9.0）
Get-File $voiceMsix (Join-Path $work "xiaoxiao.msix")
$voiceDir = Join-Path $payload "x64\NarratorVoices\Xiaoxiao"
if (-not (Test-Path (Join-Path $voiceDir "AppxManifest.xml"))) {
    # Expand-Archive 仅支持 .zip 扩展名，先复制改名
    $msixZip = Join-Path $work "xiaoxiao.zip"
    Copy-Item (Join-Path $work "xiaoxiao.msix") $msixZip -Force
    Expand-Archive -Path $msixZip -DestinationPath $voiceDir -Force
}

# 3. 组装载荷：适配器 x64 运行文件
Copy-Item (Join-Path $adapterDir "x64\*") (Join-Path $payload "x64\") -Force
Copy-Item (Join-Path $adapterDir "Installer.exe") $payload -Force

# 4. 第三方声明
Copy-Item (Join-Path $PSScriptRoot "THIRD-PARTY-NOTICES.txt") $payload -Force

Write-Output ""
Write-Output "载荷组装完成: $payload"
Get-ChildItem $payload -Recurse -File | Measure-Object Length -Sum | ForEach-Object {
    Write-Output ("共 {0} 个文件, {1:N1} MB" -f $_.Count, ($_.Sum / 1MB))
}
Write-Output "下一步: ISCC.exe installer\voice.iss"
