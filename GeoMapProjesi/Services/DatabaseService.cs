using System;
using System.Data.SQLite; // SQLite kütüphanesi
using System.IO;
using System.Collections.Generic;

namespace GeoMapProjesi.Services
{
    public class DatabaseService
    {
        // Veritabanı dosyasının yolu (Programın çalıştığı klasörde oluşur)
        private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "analizler.db");
        private static string connectionString = $"Data Source={dbPath};";

        public DatabaseService()
        {
            TabloyuOlustur();
        }

        // 1. Tabloyu Yoksa Oluştur (İlk kurulum)
        private void TabloyuOlustur()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS AnalizSonuclari (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Tarih DATETIME DEFAULT CURRENT_TIMESTAMP,
                        KonumAdi TEXT,
                        Enlem REAL,
                        Boylam REAL,
                        SehirOrani REAL,
                        TarimOrani REAL,
                        OrmanOrani REAL,
                        SuOrani REAL,
                        RaporMetni TEXT
                    )";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 2. Yeni Kayıt Ekle
        public void Kaydet(string konumAdi, double enlem, double boylam, float[] modelSonuclari, string rapor)
        {
            // Yüzdeleri hesapla (Kaydederken lazım olacak)
            var oranlar = YuzdeleriHesapla(modelSonuclari);

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    INSERT INTO AnalizSonuclari (Tarih, KonumAdi, Enlem, Boylam, SehirOrani, TarimOrani, OrmanOrani, SuOrani, RaporMetni)
                    VALUES (@tarih, @konum, @lat, @lng, @sehir, @tarim, @orman, @su, @rapor)";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tarih", DateTime.Now);
                    cmd.Parameters.AddWithValue("@konum", konumAdi);
                    cmd.Parameters.AddWithValue("@lat", enlem);
                    cmd.Parameters.AddWithValue("@lng", boylam);

                    // Oranlar dizisi: 0:Şehir, 1:Tarım, 3:Orman, 4:Su
                    cmd.Parameters.AddWithValue("@sehir", oranlar[0]);
                    cmd.Parameters.AddWithValue("@tarim", oranlar[1]);
                    cmd.Parameters.AddWithValue("@orman", oranlar[3]);
                    cmd.Parameters.AddWithValue("@su", oranlar[4]);

                    cmd.Parameters.AddWithValue("@rapor", rapor);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Yardımcı: Sadece yüzdeleri döndürür
        private double[] YuzdeleriHesapla(float[] output)
        {
            double[] yuzdeler = new double[7];
            int[] sayaclar = new int[7];
            int toplamPiksel = 512 * 512;

            for (int i = 0; i < toplamPiksel; i++)
            {
                float maxVal = -float.MaxValue;
                int maxClass = 0;
                for (int c = 0; c < 7; c++)
                {
                    int idx = (c * toplamPiksel) + i;
                    if (output[idx] > maxVal) { maxVal = output[idx]; maxClass = c; }
                }
                sayaclar[maxClass]++;
            }

            for (int k = 0; k < 7; k++)
                yuzdeler[k] = (double)sayaclar[k] / toplamPiksel * 100.0;

            return yuzdeler;
        }

        public List<AnalizKaydi> GecmisiGetir()
        {
            var liste = new List<AnalizKaydi>();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                // En yeniden en eskiye doğru sırala
                string sql = "SELECT * FROM AnalizSonuclari ORDER BY Id DESC";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            liste.Add(new AnalizKaydi
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Tarih = Convert.ToDateTime(reader["Tarih"]).ToString("dd.MM.yyyy HH:mm"),
                                KonumAdi = reader["KonumAdi"].ToString(),
                                Enlem = Convert.ToDouble(reader["Enlem"]),
                                Boylam = Convert.ToDouble(reader["Boylam"]),
                                Orman = Convert.ToDouble(reader["OrmanOrani"]),
                                Su = Convert.ToDouble(reader["SuOrani"]),
                                Sehir = Convert.ToDouble(reader["SehirOrani"]),
                                Tarim = Convert.ToDouble(reader["TarimOrani"])
                            });
                        }
                    }
                }
            }
            return liste;
        }
    }
}