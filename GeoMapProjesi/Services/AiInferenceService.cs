using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace GeoMapProjesi.Services
{
    public class AiInferenceService
    {
        private InferenceSession _session;
        // Modelimiz 256x256 eğitildiği için burası 256 kalmalı
        private const int ModelInputSize = 512;

        public AiInferenceService(string modelPath)
        {
            try
            {
                if (File.Exists(modelPath))
                {
                    // CPU versiyonu ile oturum aç (GPU varsa otomatik kullanmaz, ayar gerekir)
                    _session = new InferenceSession(modelPath);
                }
                else
                {
                    MessageBox.Show($"UYARI: Model dosyası bulunamadı!\nYol: {modelPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Model Yükleme Hatası (Constructor): " + ex.Message);
            }
        }

        public float[] RunInference(string imagePath)
        {
            if (_session == null)
            {
                MessageBox.Show("HATA: Model yüklü değil (Session NULL).");
                return null;
            }

            try
            {
                // 1. Resmi Güvenli Şekilde Tensor'a Çevir
                var tensor = LoadImageAsTensor(imagePath);

                // 2. Modele Girdi Olarak Hazırla
                // DİKKAT: Python kodunda 'input_names=["input"]' demiştik. Burası da "input" olmalı.
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input", tensor)
                };

                // 3. Çalıştır
                using (var results = _session.Run(inputs))
                {
                    // İlk çıktıyı al (float dizisi olarak)
                    return results.First().AsEnumerable<float>().ToArray();
                }
            }
            catch (Exception ex)
            {
                // Hatayı detaylı görelim
                MessageBox.Show($"RunInference İçinde Hata:\n{ex.Message}\n\nDetay: {ex.StackTrace}");
                return null;
            }
        }

        private DenseTensor<float> LoadImageAsTensor(string path)
        {
            // Resmi Diskten Oku
            BitmapImage originalImage = new BitmapImage();
            originalImage.BeginInit();
            originalImage.UriSource = new Uri(path);
            originalImage.CacheOption = BitmapCacheOption.OnLoad; // Dosya kilidini hemen bırak
            originalImage.EndInit();

            // 1. Boyutlandırma (Resize) -> 256x256
            TransformedBitmap resizedBitmap = new TransformedBitmap(originalImage, new ScaleTransform(
                (double)ModelInputSize / originalImage.PixelWidth,
                (double)ModelInputSize / originalImage.PixelHeight));

            // 2. FORMAT ZORLAMA (Kritik Nokta!)
            // Resmi zorla Pbgra32 (32-bit) formatına çeviriyoruz ki piksel hesaplaması şaşmasın.
            FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap();
            formattedBitmap.BeginInit();
            formattedBitmap.Source = resizedBitmap;
            formattedBitmap.DestinationFormat = PixelFormats.Pbgra32;
            formattedBitmap.EndInit();

            // Pikselleri bayt dizisine al
            int stride = ModelInputSize * 4; // Her piksel 4 byte (B-G-R-A)
            byte[] pixelData = new byte[ModelInputSize * stride];
            formattedBitmap.CopyPixels(pixelData, stride, 0);

            // Tensor Oluştur [1, 3, 256, 256]
            var tensor = new DenseTensor<float>(new[] { 1, 3, ModelInputSize, ModelInputSize });

            // Pikselleri Tensor'a Yerleştir (Normalize: 0-1 arası)
            for (int y = 0; y < ModelInputSize; y++)
            {
                for (int x = 0; x < ModelInputSize; x++)
                {
                    int index = (y * stride) + (x * 4);

                    // Pbgra32 formatında sıra: Blue, Green, Red, Alpha
                    float b = pixelData[index] / 255.0f;
                    float g = pixelData[index + 1] / 255.0f;
                    float r = pixelData[index + 2] / 255.0f;

                    // ONNX (PyTorch) formatı: [Batch, Channel, Height, Width]
                    tensor[0, 0, y, x] = r; // Kanal 0: Kırmızı
                    tensor[0, 1, y, x] = g; // Kanal 1: Yeşil
                    tensor[0, 2, y, x] = b; // Kanal 2: Mavi
                }
            }

            return tensor;
        }
    }
}