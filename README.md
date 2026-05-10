# GeoMapProjesi

GeoMapProjesi, .NET 8 WPF tabanli cografi analiz uygulamasidir. Harita, mekansal veri, hava durumu ve AI destekli cikarim bilesenlerini birlestirir.

## Ozellikler

- WPF tabanli harita arayuzu
- Mekansal veri modelleri ve analiz kayitlari
- Hava durumu servisi entegrasyonu
- Geocoding servisi
- ONNX tabanli AI inference (`deepglobe_final.onnx`)
- Gecmis ve analiz ekranlari

## Proje Yapisı

- `GeoMapProjesi`: Ana WPF uygulamasi
- `GeoMapProjesi/Services`: Is servisleri (hava durumu, geocoding, AI, veritabani)
- `GeoMapProjesi/Models`: Domain modelleri
- `ModelTrainer`: Model egitimi/cevirimi icin Python araclari ve veri seti

## Gereksinimler

- .NET 8 SDK
- Windows (WPF icin)
- Python 3.x (ModelTrainer araclari icin, opsiyonel)

## Calistirma

1. Cozumu acin: `GeoMapProjesi.slnx`
2. Uygulamayi calistirin:

```bash
dotnet run --project GeoMapProjesi/GeoMapProjesi.csproj
```

## Notlar

- Model dosyalari (`*.onnx`) ve genis veri setleri repoda oldugu icin depo boyutu yuksek olabilir.
- `bin/` ve `obj/` klasorlerinin `.gitignore` ile dislanmasi onerilir.
