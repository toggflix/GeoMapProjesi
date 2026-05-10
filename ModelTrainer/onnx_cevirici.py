
import torch
import segmentation_models_pytorch as smp

# --- AYARLAR (Eğitimdekiyle aynı olmalı) ---
ENCODER = 'resnet18'      # ResNet18 kullanmıştık
ENCODER_WEIGHTS = 'imagenet'
CLASSES = ['urban', 'agriculture', 'rangeland', 'forest', 'water', 'barren', 'unknown']
IMAGE_SIZE = 512
DEVICE = 'cuda' if torch.cuda.is_available() else 'cpu'

print(f"📦 Model {DEVICE} üzerinde yükleniyor...")

# --- KRİTİK NOKTA: weights_only=False ---
try:
    # Modeli yükle (Güvenlik uyarısını aşarak)
    best_model = torch.load('./best_deepglobe_model.pth', map_location=DEVICE, weights_only=False)
    best_model.eval()
    print("✅ Model başarıyla yüklendi!")

    print("🔄 ONNX dönüşümü başlıyor...")
    
    # Sahte bir veri oluştur (Modelin giriş boyutuyla aynı)
    dummy_input = torch.randn(1, 3, IMAGE_SIZE, IMAGE_SIZE).to(DEVICE)

    # ONNX olarak dışarı aktar
    torch.onnx.export(
        best_model, 
        dummy_input, 
        "deepglobe_final.onnx",  # Çıktı dosyasının adı
        verbose=False,
        input_names=['input'],
        output_names=['output'],
        opset_version=11
    )
    
    print("🎉 BİTTİ! 'deepglobe_final.onnx' dosyan hazır.")
    print("👉 Şimdi C# projesine geçebilirsin.")

except Exception as e:
    print(f"❌ HATA OLDU: {e}")