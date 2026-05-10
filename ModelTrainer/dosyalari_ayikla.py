import os
import shutil

# 1. AYARLAR
kaynak_klasor = "./karisik_dosyalar"  # Senin hepsini attığın yer
hedef_ana_klasor = "./dataset/train"  # Nereye gidecekleri

# Hedef klasörleri oluştur (Yoksa yaratır)
os.makedirs(os.path.join(hedef_ana_klasor, "images"), exist_ok=True)
os.makedirs(os.path.join(hedef_ana_klasor, "masks"), exist_ok=True)

print("🧹 Temizlik başlıyor...")

# 2. DOSYALARI TARA VE TAŞI
sayac_jpg = 0
sayac_png = 0

for dosya_adi in os.listdir(kaynak_klasor):
    # Tam dosya yolu
    kaynak_yol = os.path.join(kaynak_klasor, dosya_adi)
    
    # Eğer bu bir klasörse atla, sadece dosyalarla ilgilen
    if not os.path.isfile(kaynak_yol):
        continue

    # JPG ise -> images klasörüne
    if dosya_adi.endswith(".jpg"):
        hedef_yol = os.path.join(hedef_ana_klasor, "images", dosya_adi)
        shutil.move(kaynak_yol, hedef_yol)
        sayac_jpg += 1
        
    # PNG ise -> masks klasörüne
    elif dosya_adi.endswith(".png"):
        hedef_yol = os.path.join(hedef_ana_klasor, "masks", dosya_adi)
        shutil.move(kaynak_yol, hedef_yol)
        sayac_png += 1

print(f" İŞLEM TAMAM!")
print(f" {sayac_jpg} adet Uydu Görüntüsü (JPG) taşındı.")
print(f" {sayac_png} adet Maske (PNG) taşındı.")
print("Klasör yapısı eğitim için hazır!")
