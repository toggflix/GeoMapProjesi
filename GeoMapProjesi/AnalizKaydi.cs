using System;

namespace GeoMapProjesi
{
    // Veritabanındaki bir satırın C# karşılığı
    public class AnalizKaydi
    {
        public int Id { get; set; }
        public string Tarih { get; set; } // Ekranda düzgün gözüksün diye string yaptık
        public string KonumAdi { get; set; }
        public double Enlem { get; set; }
        public double Boylam { get; set; }

        // Yüzdeler
        public double Orman { get; set; }
        public double Su { get; set; }
        public double Sehir { get; set; }
        public double Tarim { get; set; }
    }
}