# Katkı rehberi

Katkılarınızı bekliyoruz. Büyük bir özellik veya mimari değişiklik için önce bir
issue açarak kapsamı tartışın. Küçük hata düzeltmeleri için doğrudan pull request
hazırlayabilirsiniz.

## Geliştirme ortamı

- Windows 10/11 x64 ve .NET 10 SDK kullanın.
- WPF arayüzünü `src/XartZans.WinManager.App`, platformdan bağımsız hesaplamaları
  `src/XartZans.WinManager.Core` altında tutun.
- Görüntülenemeyen sistem verilerini tahmin etmeyin; erişim yoksa açıkça belirtin.
- Sistem değişikliği yapan özelliklerde önizleme, açık kullanıcı onayı, iptal ve
  hata çıktısı gereklidir.
- Yeni NuGet paketleri eklemeden önce lisans ve gereksinimi belgeleyin.

## Kontrol

```powershell
dotnet restore XartZans-WinManager.sln
dotnet build XartZans-WinManager.sln -c Release
dotnet test XartZans-WinManager.sln -c Release
```

Pull request açıklamasında kullanıcıya görünen değişiklikleri, yapılan kontrolleri
ve henüz karşılanmayan noktaları belirtin.
