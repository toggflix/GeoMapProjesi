using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GeoMapProjesi
{
    public partial class AnalysisWindow : Window
    {
        public AnalysisWindow(BitmapSource original, BitmapSource aiResult, BitmapSource depthMap, float[] hamVeri, string tamRapor, bool isEnglish)
        {
            InitializeComponent();

            // Resimler
            ImgOriginal.Source = original;
            ImgResult.Source = aiResult;
            ImgDepth.Source = depthMap;

            // Metinler
            TxtTarih.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            TxtMimarYorum.Text = tamRapor;

            // İstatistikleri Hesapla
            HesaplaVeDoldur(hamVeri);

            // DİL AYARI YAP
            DilAyarlariniUygula(isEnglish);
        }

        private void DilAyarlariniUygula(bool isEn)
        {
            if (isEn)
            {
                // PENCERE BAŞLIKLARI (İNGİLİZCE)
                Title = "GeoMap - 3D Topological Analysis Report";
                LblMainTitle.Text = "🌍 GEO-SPATIAL 3D ANALYSIS DASHBOARD";

                LblImgOriginal.Text = "🛰️ SATELLITE (RGB)";
                LblImgResult.Text = "🧠 AI SEGMENTATION";
                LblImgDepth.Text = "⛰️ 3D ELEVATION MAP";

                LblStatsTitle.Text = "📊 LAND DISTRIBUTION";
                LblForest.Text = "🌲 Forest";
                LblWater.Text = "💧 Water Body";
                LblCity.Text = "🏙️ Urban/City";
                LblAgri.Text = "🌾 Agriculture";

                LblReportTitle.Text = "🏗️ ARCHITECTURAL INSIGHT & INVESTMENT ANALYSIS";
                BtnClose.Content = "CLOSE";
            }
            else
            {
                // PENCERE BAŞLIKLARI (TÜRKÇE)
                Title = "GeoMap - 3D Topoğrafik Analiz Raporu";
                LblMainTitle.Text = "🌍 GEO-SPATIAL 3D ANALİZ PANELİ";

                LblImgOriginal.Text = "🛰️ UYDU (RGB)";
                LblImgResult.Text = "🧠 SINIFLANDIRMA";
                LblImgDepth.Text = "⛰️ 3D TOPOĞRAFYA";

                LblStatsTitle.Text = "📊 ARAZİ DAĞILIMI";
                LblForest.Text = "🌲 Orman";
                LblWater.Text = "💧 Su Kütlesi";
                LblCity.Text = "🏙️ Yerleşim";
                LblAgri.Text = "🌾 Tarım";

                LblReportTitle.Text = "🏗️ MİMARİ RAPOR & YATIRIM ANALİZİ";
                BtnClose.Content = "KAPAT";
            }
        }

        private void HesaplaVeDoldur(float[] data)
        {
            if (data == null) return;
            int pixelCount = 512 * 512;
            int[] counts = new int[7];

            for (int i = 0; i < pixelCount; i++)
            {
                float maxVal = -9999; int maxIdx = 0;
                for (int c = 0; c < 7; c++)
                {
                    if (data[c * pixelCount + i] > maxVal) { maxVal = data[c * pixelCount + i]; maxIdx = c; }
                }
                counts[maxIdx]++;
            }

            double orman = (double)counts[3] / pixelCount * 100.0;
            double su = (double)counts[4] / pixelCount * 100.0;
            double sehir = (double)counts[0] / pixelCount * 100.0;
            double tarim = (double)counts[1] / pixelCount * 100.0;

            PbOrman.Value = orman; TxtOrmanYuzde.Text = $"%{orman:F1}";
            PbSu.Value = su; TxtSuYuzde.Text = $"%{su:F1}";
            PbSehir.Value = sehir; TxtSehirYuzde.Text = $"%{sehir:F1}";
            PbTarim.Value = tarim; TxtTarimYuzde.Text = $"%{tarim:F1}";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}