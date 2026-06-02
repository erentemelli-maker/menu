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

## Personel Yonetimi

- Personel hesaplari SQL Server uzerinde saklanir.
- Parolalar PBKDF2 tabanli ozet olarak kaydedilir; duz metin parola tutulmaz.
- Roller: `Yonetici`, `Garson`, `Mutfak`
- Pasif hesaplar sisteme giris yapamaz.
- Son aktif yonetici pasife alinamaz veya rolu degistirilemez.

## Coklu Sube

- Mevcut veriler otomatik olarak `Merkez Sube` altinda korunur.
- Urunler, masalar, siparisler ve personeller subeye gore ayrilir.
- Yonetici ust menudeki seciciden aktif subeyi degistirebilir.
- QR menu adresleri `branchId` tasir; ayni masa numarasi farkli subelerde kullanilabilir.
- Secili veya son aktif sube yanlislikla pasife alinamaz.

## Masa Hesabi ve Odeme

- Teslim edilen siparisler masa hesabinda odeme bekler.
- Garson panelinden nakit veya kart odemesi alinabilir.
- Odeme kaydi SQL Server uzerinde saklanir.
- Tahsilat tamamlandiginda masa otomatik olarak `Bos` durumuna gecer.
- Satis raporlari teslim edilen degil, tahsil edilen siparisleri kullanir.
- Hesap tek seferde, serbest tutarla veya urun satirlari secilerek tahsil edilebilir.
- Ayni hesapta nakit ve kart odemeleri birlikte kullanilabilir.

## Rol Ayrimi

- `Yonetici`: panel, siparis merkezi, kasa, raporlar ve yonetim ekranlari
- `Garson`: yalnizca servise hazir siparisler ve salon durumu
- `Mutfak`: yalnizca mutfak siparis kuyrugu ve hazirlama aksiyonlari
- Odeme alma ve masa kapatma islemleri sadece yonetici kasasinda bulunur.
