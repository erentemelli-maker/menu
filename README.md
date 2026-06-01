# Dijital Menu MVP

QR kodlu restoran menusu ve siparis sistemi icin calisan ilk prototip.

## Katmanlar

- `DijitalMenu.Domain`: Masa, kategori, urun ve siparis modelleri
- `DijitalMenu.Application`: Servis sozlesmeleri ve is kurallari
- `DijitalMenu.Persistence`: SQL Server veritabani ve EF Core repository
- `DijitalMenu.WebUI`: Mobil uyumlu musteri menusu, sepet ve siparis takip paneli

## Tasarim Sistemi

Arayuzler `Minoa` markasi altinda mobil oncelikli bir tasarim sistemi kullanir.
Yeni ekranlar `wwwroot/css/site.css` icindeki renk tokenlari, kartlar, formlar,
butonlar ve navigasyon kaliplarini tekrar kullanmalidir.

- Musteri deneyimi: sade, hizli ve menuyu one cikaran mobil akış
- Isletme deneyimi: ortak yonetim kabugu, tutarli paneller ve belirgin aksiyonlar
- Ana renkler: koyu yesil yuzeyler, yumusak ada cayi tonlari ve turuncu vurgu

## Calistirma

```powershell
dotnet run --project src\DijitalMenu.WebUI\DijitalMenu.WebUI.csproj
```

Varsayilan musteri menusu Masa 5 icin acilir. Isletme ekranlarina ust menudeki
`Siparis Takibi`, `Katalog`, `Masalar` ve `QR Kodlar` baglantilarindan
ulasilabilir.

## Siradaki Adim

Uygulama SQL Server uzerinde `DijitalMenuDb` veritabanini ilk calistirmada
otomatik olusturur. Yerel gelistirme ayari `.\SQLEXPRESS` instance'ini,
Windows kimlik dogrulamasini ve yerel ortam icin `Encrypt=False` ayarini
kullanir. Uretim ortaminda sifreli SQL Server baglantisi yapilandirilmalidir.
Siradaki
adim detayli operasyon ekranlari ve satis raporlamasidir.

## Demo Girisleri

- `admin / admin123`: tum isletme ekranlari
- `garson / garson123`: siparis merkezi
- `mutfak / mutfak123`: siparis merkezi

Siparis merkezi SignalR ile canli guncellenir.

## Operasyon Ekranlari

- `Mutfak`: aktif siparis kuyrugu, sure sayaci ve tek dokunusla durum ilerletme
- `Garson`: hazir siparisler, teslim aksiyonu ve anlik masa durum panosu
- Masa durumlari: `Bos`, `Dolu`, `Servis Bekliyor`
