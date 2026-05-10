using GeoMapProjesi.Services;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace GeoMapProjesi
{
    public partial class MainWindow : Window
    {
        // --- SERVİSLER ---
       
        private AiEngine _aiEngine = new AiEngine(); // YENİ MOTOR
        private DatabaseService _dbService = new DatabaseService();
        private WeatherService _weatherService = new WeatherService();
        private GeocodingService _geoService = new GeocodingService();
        private bool _isEnglish = false;
        // --- HARİTA ARAÇLARI ---
        private List<PointLatLng> _olcumNoktalari = new List<PointLatLng>();
        private GMapRoute _olcumCizgisi;

        public MainWindow()
        {
            InitializeComponent();
            HaritaAyarlari();
        }
        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbLanguage.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string lang = item.Tag.ToString();
                _isEnglish = (lang == "EN");

                // Arayüzü Güncelle
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (_isEnglish)
            {
                // --- ENGLISH TRANSLATION ---
                Title = "GeoMap Project - Control Panel";

                LblMainHeader.Text = "🌍 GeoMap Control";
                LblToolsTitle.Text = "MAP TOOLS";
                BtnClear.Content = "🧹 Clear Map";
                BtnHistory.Content = "📜 History";

                LblMeasureTitle.Text = "📏 DISTANCE MEASURE";
                LblMeasureHint.Text = "Double-click on map to add points.";

                LblAnalyzeTitle.Text = "AI ANALYSIS OPS";
                BtnAnalyze.Content = "🚀 ANALYZE ZONE";

                LblStatusTitle.Text = "SYSTEM STATUS";
                TxtStatus.Text = "System Ready.";

                // Loading Ekranı
                LblLoadingTitle.Text = "Processing Image...";
                LblLoadingDesc.Text = "Please wait, AI is analyzing the terrain.";
            }
            else
            {
                // --- TÜRKÇE ---
                Title = "GeoMap Projesi - Ana Kontrol Paneli";

                LblMainHeader.Text = "🌍 GeoMap Kontrol";
                LblToolsTitle.Text = "HARİTA ARAÇLARI";
                BtnClear.Content = "🧹 Temizle";
                BtnHistory.Content = "📜 Geçmiş";

                LblMeasureTitle.Text = "📏 MESAFE ÖLÇÜMÜ";
                LblMeasureHint.Text = "Haritaya çift tıklayarak nokta ekleyin.";

                LblAnalyzeTitle.Text = "ANALİZ İŞLEMLERİ";
                BtnAnalyze.Content = "🚀 BÖLGEYİ ANALİZ ET";

                LblStatusTitle.Text = "DURUM BİLGİSİ";
                TxtStatus.Text = "Sistem Hazır.";

                LblLoadingTitle.Text = "Görüntü İşleniyor...";
                LblLoadingDesc.Text = "Lütfen bekleyiniz, yapay zeka analiz yapıyor.";
            }
        }

        private void HaritaAyarlari()
        {
            try
            {
                GMaps.Instance.Mode = AccessMode.ServerAndCache;
                MainMap.MapProvider = GMapProviders.GoogleSatelliteMap;
                MainMap.Position = new PointLatLng(39.8468, 32.8664); // Ankara
                MainMap.ShowCenter = false;
                MainMap.DragButton = System.Windows.Input.MouseButton.Left;
                MainMap.MouseDoubleClick += MainMap_MouseDoubleClick;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Harita Hatası: " + ex.Message);
            }
        }

        // --- ANA ANALİZ BUTONU (GÜNCELLENMİŞ HALİ) ---
        private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            // Arayüz Mesajları
            LoadingOverlay.Visibility = Visibility.Visible;
            TxtStatus.Text = _isEnglish ? "Scanning Region..." : "Harita taranıyor...";

            System.Drawing.Bitmap haritaBitmap = GetSnapshot(MainMap);
            if (haritaBitmap == null)
            {
                MessageBox.Show(_isEnglish ? "Error capturing map!" : "Harita görüntüsü alınamadı!");
                LoadingOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            TxtStatus.Text = _isEnglish ? "Processing AI & Environmental Data..." : "Yapay Zeka ve Çevresel Veriler İşleniyor...";

            await Task.Run(() =>
            {
                try
                {
                    var aiSonuc = _aiEngine.AnalyzeImage(haritaBitmap);

                    // Koordinat Alma
                    double lat = 0, lng = 0;
                    Dispatcher.Invoke(() => { lat = MainMap.Position.Lat; lng = MainMap.Position.Lng; });

                    // Servisler (Not: Servislerin içini çevirmiyoruz, onlar API'den ne gelirse onu yazar)
                    var taskHava = _weatherService.HavaDurumuGetir(lat, lng, _isEnglish);
                    var taskAdres = _geoService.AdresGetir(lat, lng);
                    Task.WaitAll(taskHava, taskAdres);

                    string havaRaporu = taskHava.Result;
                    string tamAdres = taskAdres.Result;

                    Dispatcher.Invoke(() =>
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        TxtStatus.Text = _isEnglish ? "Analysis Complete." : "Analiz Tamamlandı.";

                        if (aiSonuc != null)
                        {
                            // 1. İSTATİSTİK RAPORU (Dile Göre)
                            string istatistikRapor = HesaplaVeRaporla(aiSonuc.HamVeri);

                            // 2. MİMAR YORUMU (Dile Göre)
                            string mimarYorumu = MimariYorumGetir(aiSonuc.HamVeri);

                            // 3. BAŞLIKLAR (Dile Göre)
                            string baslikHava = _isEnglish ? "🌍 WEATHER INFO: " : "🌍 HAVA DURUMU: ";
                            string baslikKonum = _isEnglish ? "📍 LOCATION: " : "📍 KONUM: ";

                            // HEPSİNİ BİRLEŞTİR
                            string tamRapor = $"{istatistikRapor}\n" +
                                              $"{mimarYorumu}\n\n" +
                                              $"{baslikHava}{havaRaporu}\n" +
                                              $"{baslikKonum}{tamAdres}";

                            // PENCEREYİ AÇ
                            var orjinalWpf = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(
                                OpenCvSharp.Extensions.BitmapConverter.ToMat(haritaBitmap)
                            );

                            AnalysisWindow yeniPencere = new AnalysisWindow(
                                orjinalWpf,
                                aiSonuc.BoyaliResim,
                                aiSonuc.DerinlikHaritasi,
                                aiSonuc.HamVeri,
                                tamRapor,
                                _isEnglish // Dil bilgisini gönderiyoruz
                            );
                            yeniPencere.Show();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        MessageBox.Show("Error: " + ex.Message);
                    });
                }
            });
        }


        // --- SENİN ESKİ HESAPLAMA FONKSİYONLARIN (GERİ EKLENDİ) ---

        // --- GÜNCELLENMİŞ GLOBAL RAPORLAMA ---

        private string HesaplaVeRaporla(float[] modelOutput)
        {
            if (modelOutput == null) return _isEnglish ? "ERROR: No Data" : "HATA: Veri Yok";

            int pixelCount = 512 * 512;
            int[] counts = new int[7];

            for (int i = 0; i < pixelCount; i++)
            {
                float maxVal = -float.MaxValue;
                int maxClass = 0;
                for (int c = 0; c < 7; c++)
                {
                    int idx = (c * pixelCount) + i;
                    if (modelOutput[idx] > maxVal) { maxVal = modelOutput[idx]; maxClass = c; }
                }
                counts[maxClass]++;
            }

            string rapor = _isEnglish ? "📊 LAND DISTRIBUTION:\n" : "📊 ARAZİ DAĞILIMI:\n";

            // İsimler (TR / EN)
            string[] namesTR = { "🏙️ Şehir", "🌾 Tarım", "🌱 Mera", "🌲 Orman", "💧 Su", "🏜️ Çorak", "❓ Bilinmeyen" };
            string[] namesEN = { "🏙️ Urban", "🌾 Agri", "🌱 Range", "🌲 Forest", "💧 Water", "🏜️ Barren", "❓ Unknown" };

            string[] names = _isEnglish ? namesEN : namesTR;

            for (int k = 0; k < 7; k++)
            {
                double yuzde = (double)counts[k] / pixelCount * 100.0;
                if (yuzde > 1.0)
                    rapor += $"{names[k]}: %{yuzde:F1}\n";
            }
            return rapor;
        }

        private string MimariYorumGetir(float[] modelOutput)
        {
            int pixelCount = 512 * 512;
            int su = 0, orman = 0, sehir = 0, tarim = 0;

            for (int i = 0; i < pixelCount; i++)
            {
                float maxVal = -1000; int maxIdx = 0;
                for (int c = 0; c < 7; c++)
                {
                    if (modelOutput[c * pixelCount + i] > maxVal) { maxVal = modelOutput[c * pixelCount + i]; maxIdx = c; }
                }
                if (maxIdx == 4) su++;
                else if (maxIdx == 3) orman++;
                else if (maxIdx == 0) sehir++;
                else if (maxIdx == 1) tarim++;
            }

            double suOrani = (double)su / pixelCount * 100.0;
            double ormanOrani = (double)orman / pixelCount * 100.0;
            double sehirOrani = (double)sehir / pixelCount * 100.0;
            double tarimOrani = (double)tarim / pixelCount * 100.0;

            // --- İNGİLİZCE YORUMLAR ---

            if (_isEnglish)
            {
                string comment = "🏗️ ARCHITECT'S INSIGHT:\n";
                if (suOrani > 40) comment += "⚠️ High Water Risk: Expensive drainage required. Not suitable for standard housing.";
                else if (ormanOrani > 45) comment += "🌲 Protected Area: Heavy vegetation detected. Environmental clearance needed.";
                else if (sehirOrani > 50) comment += "🏙️ Urban Zone: High density area. Suitable for urban renewal projects.";
                else if (tarimOrani > 40) comment += "🌾 Agricultural Land: Fertile soil. Check zoning laws before construction.";
                else comment += "✅ PRIME INVESTMENT: No major obstacles detected. Ideal for residential or commercial development.";
                return comment;
            }
            // --- TÜRKÇE YORUMLAR ---
            else
            {
                string yorum = "🏗️ MİMAR GÖRÜŞÜ:\n";
                if (suOrani > 40) yorum += "⚠️ Sulak Zemin: Drenaj ve zemin iyileştirme maliyeti yüksek olabilir.";
                else if (ormanOrani > 45) yorum += "🌲 Orman Alanı: İmar izni ve ÇED raporu gerektirebilir.";
                else if (sehirOrani > 50) yorum += "🏙️ Yerleşim: Kentsel dönüşüm veya kat karşılığı projeye uygun.";
                else if (tarimOrani > 40) yorum += "🌾 Tarım Arazisi: Tarım dışı kullanım izni alınmalı.";
                else yorum += "✅ YATIRIMA UYGUN: Engelleyici büyük bir unsur görülmedi. Konut/Ticari proje geliştirilebilir.";
                return yorum;
            }
            
        }

        // --- YARDIMCI: HARİTA RESMİ ÇEKME ---
        private System.Drawing.Bitmap GetSnapshot(System.Windows.UIElement source)
        {
            double actualHeight = source.RenderSize.Height;
            double actualWidth = source.RenderSize.Width;
            if (actualHeight == 0 || actualWidth == 0) return null;

            RenderTargetBitmap renderTarget = new RenderTargetBitmap(
                (int)actualWidth, (int)actualHeight, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(source);

            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                encoder.Save(stream);
                return new System.Drawing.Bitmap(stream);
            }
        }

        // --- ÖLÇÜM ARAÇLARI (Dokunmadım) ---
        private void MainMap_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 1. Tıklanan noktanın koordinatını al
            Point clickPoint = e.GetPosition(MainMap);
            PointLatLng point = MainMap.FromLocalToLatLng((int)clickPoint.X, (int)clickPoint.Y);

            // 2. Listeye ekle
            _olcumNoktalari.Add(point);

            // 3. Ekrana Nokta (Marker) Koy
            GMapMarker marker = new GMapMarker(point);
            marker.Shape = new System.Windows.Shapes.Ellipse
            {
                Width = 10,
                Height = 10,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = Brushes.Yellow
            };
            MainMap.Markers.Add(marker);

            // 4. ÇİZGİ VE HESAPLAMA KISMI
            if (_olcumNoktalari.Count > 1)
            {
                // Eski çizgiyi sil ki üst üste binmesin
                if (_olcumCizgisi != null) MainMap.Markers.Remove(_olcumCizgisi);

                // Yeni çizgiyi çiz
                _olcumCizgisi = new GMapRoute(_olcumNoktalari);
                _olcumCizgisi.Shape = new System.Windows.Shapes.Path
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 3
                };
                MainMap.Markers.Add(_olcumCizgisi);

                // --- MESAFE HESAPLAMA MOTORU ---
                double distanceKm = 0;
                for (int i = 0; i < _olcumNoktalari.Count - 1; i++)
                {
                    // GMap'in kendi matematik kütüphanesini kullanıyoruz
                    distanceKm += MainMap.MapProvider.Projection.GetDistance(_olcumNoktalari[i], _olcumNoktalari[i + 1]);
                }

                // 5. SONUCU KUTUYA YAZ (TxtDistance)
                // Eğer arayüzde TxtDistance yoksa hata verir, XAML'da eklediğinden emin ol.
                TxtDistance.Text = $"{distanceKm:F2} km";

                // Durum çubuğuna da yazalım
                TxtStatus.Text = _isEnglish
                    ? $"New point added. Total Distance: {distanceKm:F2} km"
                    : $"Yeni nokta eklendi. Toplam Mesafe: {distanceKm:F2} km";
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _olcumNoktalari.Clear();
            MainMap.Markers.Clear();

            // Bunu ekle:
            TxtDistance.Text = "0.00 km";

            TxtStatus.Text = _isEnglish ? "Map Cleared." : "Harita temizlendi.";
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
             HistoryWindow history = new HistoryWindow();
             history.ShowDialog();
        }
    }
}