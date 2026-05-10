using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Media.Imaging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;

namespace GeoMapProjesi.Services
{
    public class AiResult
    {
        public BitmapSource BoyaliResim { get; set; }
        public BitmapSource DerinlikHaritasi { get; set; }
        public float[] HamVeri { get; set; }
    }

    public class AiEngine
    {
        private InferenceSession _session;
        private string _modelPath = "deepglobe_final.onnx";

        private readonly Vec3b[] _classColors = new Vec3b[]
        {
            new Vec3b(0, 255, 255),   // 0: Urban
            new Vec3b(0, 165, 255),   // 1: Agriculture
            new Vec3b(144, 238, 144), // 2: Rangeland
            new Vec3b(34, 139, 34),   // 3: Forest
            new Vec3b(255, 0, 0),     // 4: Water
            new Vec3b(192, 192, 192), // 5: Barren
            new Vec3b(0, 0, 0)        // 6: Unknown
        };

        // Yükseklik Değerleri (0=Çukur, 255=Tpe)
        private readonly byte[] _classHeights = new byte[]
        {
            250, // 0: Urban (Yüksek)
            50,  // 1: Agriculture (Alçak)
            80,  // 2: Rangeland
            180, // 3: Forest (Orta-Yüksek)
            0,   // 4: Water (En Dip)
            100, // 5: Barren
            0    // 6: Unknown
        };

        public AiEngine()
        {
            var options = new SessionOptions();
            try { options.AppendExecutionProvider_CUDA(0); } catch { }
            _session = new InferenceSession(_modelPath, options);
        }

        public AiResult AnalyzeImage(Bitmap originalImage)
        {
            // 1. Hazırlık
            using var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(originalImage);
            using var resizedMat = new Mat();
            Cv2.Resize(mat, resizedMat, new OpenCvSharp.Size(512, 512));
            using var rgbMat = new Mat();
            Cv2.CvtColor(resizedMat, rgbMat, ColorConversionCodes.BGR2RGB);

            var inputTensor = new DenseTensor<float>(new[] { 1, 3, 512, 512 });
            unsafe
            {
                byte* ptr = (byte*)rgbMat.DataPointer;
                for (int y = 0; y < 512; y++)
                {
                    for (int x = 0; x < 512; x++)
                    {
                        inputTensor[0, 0, y, x] = (ptr[0] / 255.0f - 0.485f) / 0.229f;
                        inputTensor[0, 1, y, x] = (ptr[1] / 255.0f - 0.456f) / 0.224f;
                        inputTensor[0, 2, y, x] = (ptr[2] / 255.0f - 0.406f) / 0.225f;
                        ptr += 3;
                    }
                }
            }

            // 2. Model Çalıştır
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", inputTensor) };
            using var results = _session.Run(inputs);
            var outputTensor = results.First().AsTensor<float>();
            float[] hamVeri = outputTensor.ToArray();

            // 3. Haritaları Oluştur
            using var resultMat = new Mat(512, 512, MatType.CV_8UC4);
            using var depthMat = new Mat(512, 512, MatType.CV_8UC1);

            unsafe
            {
                byte* resPtr = (byte*)resultMat.DataPointer;
                byte* depthPtr = (byte*)depthMat.DataPointer;

                for (int y = 0; y < 512; y++)
                {
                    for (int x = 0; x < 512; x++)
                    {
                        int bestClass = 0;
                        float maxScore = -9999f;
                        for (int c = 0; c < 7; c++)
                        {
                            if (outputTensor[0, c, y, x] > maxScore) { maxScore = outputTensor[0, c, y, x]; bestClass = c; }
                        }

                        // Renkli Boyama
                        Vec3b color = _classColors[bestClass];
                        resPtr[0] = color.Item0; resPtr[1] = color.Item1; resPtr[2] = color.Item2; resPtr[3] = 255;
                        resPtr += 4;

                        // Derinlik Boyama
                        *depthPtr = _classHeights[bestClass];
                        depthPtr++;
                    }
                }
            }

            // 4. Temizlik (Kare Fırça)
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            using var cleanedMat = new Mat();
            Cv2.MorphologyEx(resultMat, cleanedMat, MorphTypes.Close, kernel);

            using var cleanedDepth = new Mat();
            Cv2.MorphologyEx(depthMat, cleanedDepth, MorphTypes.Close, kernel);

            // 5. RENKLENDİRME (DÜZELTİLEN YER BURASI 👇)
            // 'Colormap' yerine 'ColormapTypes' kullanıyoruz!
            using var coloredDepth = new Mat();
            Cv2.ApplyColorMap(cleanedDepth, coloredDepth, ColormapTypes.Magma);

            // WPF Dönüşümleri
            var wpfColor = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(cleanedMat);
            var wpfDepth = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(coloredDepth);

            wpfColor.Freeze();
            wpfDepth.Freeze();

            return new AiResult
            {
                BoyaliResim = wpfColor,
                DerinlikHaritasi = wpfDepth,
                HamVeri = hamVeri
            };
        }
    }
}