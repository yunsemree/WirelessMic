# WirelessMic publish script
# Usage: .\scripts\publish.ps1 [-Windows] [-Android] [-All]

param(
    [switch]$Windows,
    [switch]$Android,
    [switch]$All
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\WirelessMic.App\WirelessMic.App.csproj"
$outputRoot = Join-Path $root "artifacts\publish"

if ($All -or (-not $Windows -and -not $Android)) {
    $Windows = $true
    $Android = $true
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

if ($Windows) {
    Write-Host "Windows EXE derleniyor..." -ForegroundColor Cyan

    $windowsOut = Join-Path $outputRoot "windows"

    # Onceki ciktilari temizle (eski artiklar kalmasin).
    if (Test-Path $windowsOut) {
        Remove-Item -Path $windowsOut -Recurse -Force
    }

    # NOT: Framework-dependent publish. WinUI3 (WindowsAppSDK) uygulamalari icin
    # tek-dosya (PublishSingleFile) VE self-contained tek-EXE guvenilir DEGILDIR:
    #   - Self-contained + single-file  -> acilis aninda COMException 0x80040111
    #     "ClassFactory cannot supply requested class" ile coker (native XAML
    #     sinif fabrikasi tek EXE icinden aktive edilemiyor).
    #   - Framework-dependent + single-file -> NETSDK1176 ve WindowsAppRuntime
    #     taban dizini bulunamamasi nedeniyle calisma aninda coker.
    # Bu yuzden duz framework-dependent publish kullaniyoruz; hedef makinede
    # .NET 10 Desktop Runtime + Windows App Runtime kurulu olmalidir.
    dotnet publish $project `
        -f net10.0-windows10.0.19041.0 `
        -c Release `
        -o $windowsOut

    $exe = Get-ChildItem -Path $windowsOut -Filter "WirelessMic.App.exe" -Recurse | Select-Object -First 1
    if ($exe) {
        Write-Host "EXE: $($exe.FullName)" -ForegroundColor Green
    }
}

if ($Android) {
    Write-Host "Android APK derleniyor..." -ForegroundColor Cyan

    dotnet publish $project `
        -f net10.0-android `
        -c Release `
        -p:AndroidPackageFormat=apk

    $apkSource = Join-Path $root "src\WirelessMic.App\bin\Release\net10.0-android"
    $apk = Get-ChildItem -Path $apkSource -Filter "*.apk" -Recurse | Select-Object -First 1

    if ($apk) {
        $apkDest = Join-Path $outputRoot "android\WirelessMic.apk"
        New-Item -ItemType Directory -Force -Path (Split-Path $apkDest) | Out-Null
        Copy-Item $apk.FullName $apkDest -Force
        Write-Host "APK: $apkDest" -ForegroundColor Green
    }
    else {
        Write-Warning "APK bulunamadi. Android SDK kurulumunu kontrol edin."
    }
}

Write-Host "`nCikti klasoru: $outputRoot" -ForegroundColor Yellow
