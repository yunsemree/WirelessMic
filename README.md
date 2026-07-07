# WirelessMic

Android veya iOS telefonunuzu yerel ağ üzerinden Windows bilgisayarınız için kablosuz mikrofon olarak kullanmanızı sağlayan .NET MAUI uygulaması.

## Gereksinimler

- .NET 10 SDK
- .NET MAUI workload (Android, iOS, Windows)
- Visual Studio 2026 veya Rider

## Solution Yapısı

```
WirelessMic.sln
src/
  WirelessMic.App          # MAUI sunum katmanı
  WirelessMic.Application  # Use case'ler ve arayüzler
  WirelessMic.Domain       # Domain modelleri ve enum'lar
  WirelessMic.Infrastructure # Altyapı implementasyonları
  WirelessMic.Shared       # Paylaşılan sabitler
tests/
  WirelessMic.Tests
```

## Mimari

Clean Architecture prensiplerine uygun katmanlı yapı:

- **Domain** — İş kuralları, modeller, enum'lar
- **Application** — Arayüzler, DTO'lar, use case'ler
- **Infrastructure** — Serilog, ayarlar, platform servisleri
- **App** — MAUI UI, ViewModel'ler, navigasyon

## Çalıştırma (geliştirme)

### Windows

```bash
dotnet run --project src/WirelessMic.App/WirelessMic.App.csproj -f net10.0-windows10.0.19041.0
```

### Android

Visual Studio'da hedef `net10.0-android` + telefon/emülatör seçip F5.

## Dağıtım (APK + EXE)

Tek komutla her iki platformu derlemek için:

```powershell
.\scripts\publish.ps1 -All
```

### Windows EXE

```powershell
dotnet publish src\WirelessMic.App\WirelessMic.App.csproj `
  -f net10.0-windows10.0.19041.0 `
  -c Release `
  -o artifacts\publish\windows
```

Çıktı: `artifacts\publish\windows\WirelessMic.App.exe` (self-contained, tek dosya, ~250 MB)

### Android APK

```powershell
dotnet publish src\WirelessMic.App\WirelessMic.App.csproj `
  -f net10.0-android `
  -c Release `
  -p:AndroidPackageFormat=apk
```

Çıktı: `src\WirelessMic.App\bin\Release\net10.0-android\com.companyname.wirelessmic.app.apk`

Kopyalanmış sürüm: `artifacts\publish\android\WirelessMic.apk`

APK'yı telefona yüklemek için dosyayı telefona aktarıp açın (bilinmeyen kaynaklara izin gerekebilir).

## Geliştirme Durumu

| Faz | Durum | Açıklama |
|-----|-------|----------|
| 1 | Tamamlandı | Proje kurulumu, DI, Logging, MVVM, Navigasyon, Yapılandırma |
| 2 | Tamamlandı | Platform tespiti (`IDeviceRoleService`) |
| 3 | Tamamlandı | UDP Broadcast Discovery |
| 4 | Tamamlandı | Connection Manager |
| 5 | Bekliyor | Mikrofon yakalama |
| 6+ | Bekliyor | Ses akışı, buffer, çıkış, sıkıştırma... |

## Testler

```bash
dotnet test
```
