import os
import cv2
import torch
import numpy as np
import segmentation_models_pytorch as smp
import albumentations as albu
from torch.utils.data import DataLoader, Dataset
from sklearn.model_selection import train_test_split
from tqdm import tqdm # Canlı ilerleme çubuğu için

# --- 1. AYARLAR ---
# --- 1. AYARLAR (TURBO MOD) ---
ENCODER = 'resnet18'      # <-- 34 yerine 18 (Hız Canavarı)
ENCODER_WEIGHTS = 'imagenet'
DEVICE = 'cuda' if torch.cuda.is_available() else 'cpu'
EPOCHS = 15               # <-- 30 yerine 15 (Bu gece bitsin)
BATCH_SIZE = 8            # <-- 4 yerine 8 (Ekran kartın kaldırırsa süre yarıya iner!)
LR = 0.0001
IMAGE_SIZE = 512

# Sınıflar
CLASSES = ['urban', 'agriculture', 'rangeland', 'forest', 'water', 'barren', 'unknown']

COLOR_MAP = {
    (0, 255, 255): 0,   # Urban (Cyan)
    (255, 255, 0): 1,   # Agriculture (Yellow)
    (255, 0, 255): 2,   # Rangeland (Purple)
    (0, 255, 0):   3,   # Forest (Green)
    (0, 0, 255):   4,   # Water (Blue)
    (255, 255, 255): 5, # Barren (White)
    (0, 0, 0):     6    # Unknown (Black)
}

# --- 2. YARDIMCI FONKSİYONLAR ---
def rgb_to_mask(rgb_image):
    # Maskeyi Float değil, int64 (Long) yapıyoruz ki hata vermesin
    mask = np.zeros((rgb_image.shape[0], rgb_image.shape[1]), dtype=np.int64)
    for color, class_id in COLOR_MAP.items():
        equality = np.equal(rgb_image, color)
        class_map = np.all(equality, axis=-1)
        mask[class_map] = class_id
    return mask

def to_tensor(x, **kwargs):
    return x.transpose(2, 0, 1).astype('float32')

# --- 3. DATASET SINIFI (GÜNCELLENDİ) ---
class DeepGlobeDataset(Dataset):
    def __init__(self, images_fps, masks_fps, augmentation=None, preprocessing=None):
        self.images_fps = images_fps
        self.masks_fps = masks_fps
        self.augmentation = augmentation
        self.preprocessing = preprocessing
    
    def __getitem__(self, i):
        # Resmi Oku
        image = cv2.imread(self.images_fps[i])
        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        
        # Maskeyi Oku
        mask_rgb = cv2.imread(self.masks_fps[i])
        mask_rgb = cv2.cvtColor(mask_rgb, cv2.COLOR_BGR2RGB)
        
        # Augmentation
        if self.augmentation:
            sample = self.augmentation(image=image, mask=mask_rgb)
            image, mask_rgb = sample['image'], sample['mask']
            
        # Maskeyi İndekse Çevir (0, 1, 2...)
        mask = rgb_to_mask(mask_rgb)
        
        # Preprocessing (Sadece resim için)
        if self.preprocessing:
            sample = self.preprocessing(image=image, mask=None)
            image = sample['image']
            
        # Kritik Nokta: Maskeyi LongTensor yapıyoruz!
        # One-Hot yapmıyoruz, onu Loss fonksiyonu halledecek.
        return image, torch.from_numpy(mask).long()
        
    def __len__(self):
        return len(self.images_fps)

# --- 4. AUGMENTATION ---
def get_training_augmentation():
    train_transform = [
        albu.Resize(IMAGE_SIZE, IMAGE_SIZE),
        albu.HorizontalFlip(p=0.5),
        albu.VerticalFlip(p=0.5),
        albu.Affine(scale=(0.9, 1.1), translate_percent=(-0.1, 0.1), rotate=(-15, 15), p=0.5),
        albu.GaussNoise(p=0.2),
        albu.Perspective(p=0.5),
        albu.OneOf([
            albu.CLAHE(p=1),
            albu.RandomBrightnessContrast(p=1),
            albu.RandomGamma(p=1),
        ], p=0.9),
    ]
    return albu.Compose(train_transform)

def get_validation_augmentation():
    return albu.Compose([albu.Resize(IMAGE_SIZE, IMAGE_SIZE)])

def get_preprocessing(preprocessing_fn):
    return albu.Compose([
        albu.Lambda(image=preprocessing_fn),
        albu.Lambda(image=to_tensor),
    ])

# --- 5. EĞİTİM MOTORU (CANLI TAKİP EKLENDİ) ---
def train_epoch(model, loader, criterion, optimizer, device):
    model.train()
    running_loss = 0.0
    running_acc = 0.0
    
    # Tqdm ile canlı çubuk
    pbar = tqdm(loader, desc="Eğitiliyor", leave=False)
    
    for images, masks in pbar:
        images = images.to(device)
        masks = masks.to(device)
        
        optimizer.zero_grad()
        outputs = model(images) # Çıktı: (Batch, Classes, H, W)
        
        loss = criterion(outputs, masks)
        loss.backward()
        optimizer.step()
        
        running_loss += loss.item()
        
        # Basit Doğruluk Hesabı (Pixel Accuracy)
        # En yüksek olasılıklı sınıfı seç (argmax) ve gerçek maskeyle kıyasla
        pred_mask = torch.argmax(outputs, dim=1)
        acc = (pred_mask == masks).float().mean().item()
        running_acc += acc
        
        # Çubuğun yanına yaz
        pbar.set_postfix({'Loss': f'{loss.item():.4f}', 'Acc': f'{acc:.2%}'})
            
    return running_loss / len(loader), running_acc / len(loader)

def validate_epoch(model, loader, criterion, device):
    model.eval()
    running_loss = 0.0
    running_acc = 0.0
    
    # Test aşaması için de çubuk
    pbar = tqdm(loader, desc="Test Ediliyor", leave=False)
    
    with torch.no_grad():
        for images, masks in pbar:
            images = images.to(device)
            masks = masks.to(device)
            
            outputs = model(images)
            loss = criterion(outputs, masks)
            
            running_loss += loss.item()
            
            pred_mask = torch.argmax(outputs, dim=1)
            acc = (pred_mask == masks).float().mean().item()
            running_acc += acc
            
            pbar.set_postfix({'Val Loss': f'{loss.item():.4f}', 'Val Acc': f'{acc:.2%}'})
            
    return running_loss / len(loader), running_acc / len(loader)

# --- 6. ANA PROGRAM ---
if __name__ == '__main__':
    print(f"🔥 Sistem: {DEVICE} hazırlanıyor...")
    
    # KLASÖR YOLLARI
    BASE_DIR = './dataset/train' 
    IMAGES_DIR = os.path.join(BASE_DIR, 'images')
    MASKS_DIR = os.path.join(BASE_DIR, 'masks')

    print("🔍 Dosyalar taranıyor...")
    
    images_fps = []
    masks_fps = []
    all_files = os.listdir(IMAGES_DIR)
    
    for f in all_files:
        if f.endswith('_sat.jpg'):
            img_path = os.path.join(IMAGES_DIR, f)
            mask_name = f.replace('_sat.jpg', '_mask.png')
            mask_path = os.path.join(MASKS_DIR, mask_name)
            if os.path.exists(mask_path):
                images_fps.append(img_path)
                masks_fps.append(mask_path)

    print(f"✅ Eşleşen Dosya: {len(images_fps)}")
    if len(images_fps) == 0:
        print("❌ HATA: Dosya bulunamadı!")
        exit()

    x_train, x_val, y_train, y_val = train_test_split(images_fps, masks_fps, test_size=0.2, random_state=42)

    model = smp.Unet(
        encoder_name=ENCODER, 
        encoder_weights=ENCODER_WEIGHTS, 
        classes=len(CLASSES), 
        activation='softmax2d',
    )
    model.to(DEVICE)
    
    preprocessing_fn = smp.encoders.get_preprocessing_fn(ENCODER, ENCODER_WEIGHTS)

    train_dataset = DeepGlobeDataset(x_train, y_train, augmentation=get_training_augmentation(), preprocessing=get_preprocessing(preprocessing_fn))
    valid_dataset = DeepGlobeDataset(x_val, y_val, augmentation=get_validation_augmentation(), preprocessing=get_preprocessing(preprocessing_fn))

    # num_workers=0 Windows için en güvenlisidir
    train_loader = DataLoader(train_dataset, batch_size=BATCH_SIZE, shuffle=True, num_workers=0)
    valid_loader = DataLoader(valid_dataset, batch_size=1, shuffle=False, num_workers=0)

    # Loss Fonksiyonu (Dice Loss artık LongTensor hedef bekliyor)
    criterion = smp.losses.DiceLoss(mode='multiclass', from_logits=False)
    optimizer = torch.optim.Adam(model.parameters(), lr=LR)

    best_loss = float('inf')
    print(f"🚀 Eğitim Başladı! ({EPOCHS} Epoch)")
    
    for epoch in range(EPOCHS):
        print(f"\n📢 Epoch: {epoch+1}/{EPOCHS}")
        
        train_loss, train_acc = train_epoch(model, train_loader, criterion, optimizer, DEVICE)
        print(f"   📉 Ortalama Train Loss: {train_loss:.4f} | Acc: {train_acc:.2%}")
        
        val_loss, val_acc = validate_epoch(model, valid_loader, criterion, DEVICE)
        print(f"   🔍 Ortalama Valid Loss: {val_loss:.4f} | Acc: {val_acc:.2%}")
        
        if val_loss < best_loss:
            best_loss = val_loss
            torch.save(model, './best_deepglobe_model.pth')
            print("   ✅ Yeni Rekor! Model Kaydedildi.")

    print("\n📦 C# için ONNX Paketleniyor...")
    best_model = torch.load('./best_deepglobe_model.pth', map_location=DEVICE)
    best_model.eval()
    dummy_input = torch.randn(1, 3, IMAGE_SIZE, IMAGE_SIZE).to(DEVICE)
    
    torch.onnx.export(
        best_model, 
        dummy_input, 
        "deepglobe_final.onnx",
        verbose=False,
        input_names=['input'],
        output_names=['output'],
        opset_version=11
    )
    print("🎉 BİTTİ! 'deepglobe_final.onnx' hazır.")