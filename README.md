# XartZan's WinManager

Modern, açık kaynak bir Windows masaüstü sistem izleme ve yönetim uygulaması.
Sistem Bilgisi ve Donanım ve Sistem ekranlarında erişilebilen gerçek verileri gösterir.
Araçlar menüsünde siyah ve beyaz tema seçilebilir. Diğer modüller arayüzde açıkça
hazırlık aşamasında olarak işaretlenir.

## Gereksinimler

- Windows 10 veya Windows 11, x64
- Geliştirme için .NET 10 SDK
- WPF (Windows Desktop bileşeni)

## Geliştirme

```powershell
dotnet restore XartZans-WinManager.sln
dotnet build XartZans-WinManager.sln -c Debug
dotnet test XartZans-WinManager.sln -c Release
```

Uygulamayı Visual Studio veya VS Code üzerinden `src/XartZans.WinManager.App`
projesiyle başlatın.

## Windows x64 yayın derlemesi

```powershell
$publishDirectory = Join-Path $env:TEMP 'XartZans-WinManager-win-x64'
dotnet publish src/XartZans.WinManager.App/XartZans.WinManager.App.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -o $publishDirectory
```

EXE, `$publishDirectory\XartZans.WinManager.exe` konumunda oluşturulur. Dil katalogları
`$publishDirectory\Languages` altında yayımlanır ve ilk çalıştırmada kullanıcı
profilindeki dil klasörüne kopyalanır. Yayın klasöründe MIT lisansı ve üçüncü taraf
bildirimleri de bulunur. Proje klasörünün yolunda `'` karakteri
bulunduğunda .NET 10 MSBuild'in yayın kopyalama adımı hata verebildiğinden çıktı
dizini geçici klasörde tutulur. GitHub Actions çıktıyı `artifacts/publish/win-x64`
altında saklar. EXE, .NET çalışma zamanını içerir.

## Kullanıcı için kolay kurulum

GitHub Releases sayfasından `XartZans-WinManager-Setup-0.1.0.exe` dosyasını indirip
çalıştırın. Kurulum yönetici izni istemeden uygulamayı kullanıcı profilinize kurar,
Masaüstü ve Başlat menüsü kısayollarını oluşturur. Kurulum bittiğinde uygulamayı
hemen açabilirsiniz. Uygulamayı daha sonra Windows **Ayarlar > Uygulamalar**
bölümünden kaldırabilirsiniz.

Sürüm etiketi (`v0.1.0` gibi) GitHub'a gönderildiğinde Windows Release iş akışı
Setup dosyasını derler ve GitHub Releases'e ekler.

## Setup dosyasını yerel derleme

Inno Setup 6 veya üstünü kurduktan sonra proje kökünde şunu çalıştırın:

```powershell
.\build-installer.ps1
```

Kurulum dosyası `artifacts/installer` klasöründe oluşturulur. Inno Setup derleyicisi
bulunamazsa [resmi Inno Setup sayfasından](https://jrsoftware.org/isdl.php) kurun.

## Ölçümler ve gizlilik

CPU, RAM, sistem diski ve ağ ölçümleri yerel Windows API'lerinden okunur. Sistem
Bilgisi ekranı CPU ve GPU yükü, RAM, disk ve ağ kullanımı ile donanım özetini içerir.
GPU yükü ve CPU/GPU/RAM sıcaklıkları varsa ekran kartı sürücüsünün sunduğu gerçek
sensörlerden okunur; desteklenmeyen veriler tahmin edilmez. Ölçümler iki saniyede
bir yenilenir. Donanım sayfası işlemci, ekran kartı, bellek modülleri, anakart, BIOS
ve fiziksel disk bilgilerini Windows WMI sınıflarından okur. Bellek modülü tablosu
parça numarasını, SMBIOS azami veri hızını (MT/s) ve ayarlanan saat hızını ayrı
gösterir. Windows sürümünün yayınlanma tarihi Microsoft'un sürüm kataloğundan
eşleştirilir. Bazı sensörler yönetici erişimi veya üretici sürücüsü isteyebilir;
uygulama kendiliğinden yükselmez ve sürücü kurmaz. Sağ üstteki dil menüsünden JSON
dil dosyaları eklenip seçilebilir. Temalar bölümünden siyah veya beyaz görünüm
seçilebilir. Donanım sayfasındaki **Sistem bilgilerini çıkart** düğmesi erişilebilen
donanım, sensör ve kullanım verilerini bir metin raporuna yazar ve dosyayı açar.
Dil dosyaları menüdeki klasör düğmesinden
açılan `%LOCALAPPDATA%\XartZans\WinManager\Languages` konumuna eklenip silinebilir;
ayrıntılar için [dil dosyası rehberine](docs/language-files.md) bakın. Uygulama
yönetici izni istemez ve Windows ayarlarını değiştiren veya dosya silen işlem
yapmaz. Hata günlükleri `%LOCALAPPDATA%\XartZans\WinManager\Logs` altında tutulur.

## Belgeler

- [Mimari ve ilk sürüm kapsamı](docs/architecture.md)
- [Dil dosyası rehberi](docs/language-files.md)
- [Katkı rehberi](CONTRIBUTING.md)
- [Güvenlik politikası](SECURITY.md)
- [Değişiklik günlüğü](CHANGELOG.md)
- [Third-party notices](THIRD-PARTY-NOTICES.md)

## Lisans

MIT lisansı. Ayrıntılar için [LICENSE](LICENSE) dosyasına bakın.

---

## English

XartZan's WinManager is an open-source Windows desktop system-monitoring and
management app. System Information and Hardware & System show available readings.
Black and white themes are available under Tools; unfinished modules are clearly marked.

Requirements: Windows 10/11 x64 and the .NET 10 SDK for development. Run the
commands above to restore, build, and test. The self-contained single-file
Windows x64 executable is published to the PowerShell `$publishDirectory`. The
GitHub Actions artifacts are stored under `artifacts/publish/win-x64` and
`artifacts/installer`. Version tags such as `v0.1.0` also publish the Setup EXE to
GitHub Releases. The publish folder contains the editable language catalogs,
Windows release date catalog, and license notices.

Users can install the app from `XartZans-WinManager-Setup-0.1.0.exe`; it installs
per-user, creates desktop and Start menu shortcuts, and offers to launch the app
when setup finishes.

CPU, memory, system-drive, and network measurements are read from local Windows
APIs and refresh every two seconds. GPU load and CPU/GPU/RAM temperatures are read
from hardware sensors when exposed by the graphics and device drivers; missing
readings are never estimated. Hardware details are read from Windows WMI classes.
Memory modules show their part number, SMBIOS maximum data rate (MT/s), and configured
clock separately. The installed Windows release date is matched from Microsoft's
release catalog. The Hardware & System export button writes all available inventory,
sensor, and usage data to a text report and opens it. The animated language menu
supports user JSON catalogs. Some sensors may require administrator access or a
vendor driver; the app does not elevate itself or install drivers. The app does not
change Windows settings or delete files.
