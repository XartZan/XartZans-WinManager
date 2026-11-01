# Harici dil dosyaları

Uygulama ilk çalıştığında yanında yayımlanan `Languages/tr.json`, `Languages/en.json`
ve `Languages/README.md` dosyalarını kullanıcıya ait şu klasöre kopyalar:

`%LOCALAPPDATA%\XartZans\WinManager\Languages`

Dil menüsündeki klasör düğmesi bu konumu açar. Yeni dil için aynı klasöre bir JSON
dosyası ekleyin; örneğin `es.json`. Uygulama menüsü her açıldığında dosyaları tekrar
okur. Bir dil dosyasını silmek, o dili listeden kaldırır. Uygulamanın bir katalogla
açılabilmesi için en az bir geçerli dosyanın kalması gerekir.

Katalog biçimi:

```json
{
  "code": "es",
  "name": "Español",
  "strings": {
    "WindowTitle": "XartZan's WinManager",
    "NavDashboard": "Panel",
    "DashboardTitle": "Panel",
    "HardwareTitle": "Hardware y sistema"
  }
}
```

Dosya adı ile `code` alanı aynı olmalıdır. `name` menüde, kullanıcının yazdığı
biçimde görünür. `strings` sözlüğündeki anahtarlar kullanıcı arayüzü metinlerini
karşılar. Eksik anahtarlar İngilizce kataloğundan tamamlanır. Geçerli kataloglarda
uygulama kodu çalıştırılmaz; kataloglar yalnızca metin içerir. Geçerli anahtarların
tam listesi uygulama içindeki `Languages/tr.json` dosyasındadır.
