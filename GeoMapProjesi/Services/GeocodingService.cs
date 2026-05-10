using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GeoMapProjesi.Services
{
    public class GeocodingService
    {
        private const string BaseUrl = "https://nominatim.openstreetmap.org/reverse";

        public async Task<string> AdresGetir(double lat, double lon)
        {
            try
            {
                // zoom=10 yapıyoruz ki okyanusta bile olsa en azından ülke/deniz adı yakalamaya çalışsın
                string url = $"{BaseUrl}?format=json&lat={lat.ToString().Replace(',', '.')}&lon={lon.ToString().Replace(',', '.')}&zoom=10&addressdetails=1";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "GeoMapProjesi/1.0 (tr-TR)");
                    client.Timeout = TimeSpan.FromSeconds(5); // 5 saniyede cevap gelmezse pes et

                    string jsonVerisi = await client.GetStringAsync(url);
                    JObject veri = JObject.Parse(jsonVerisi);

                    // --- KRİTİK KORUMA BURASI ---
                    // Eğer "error" varsa veya "address" kısmı boşsa, burası okyanustur.
                    if (veri["error"] != null || veri["address"] == null)
                    {
                        return "🌊 Açık Deniz / Okyanus (Yerleşim Yok)";
                    }

                    JToken adres = veri["address"];

                    // Tek tek kontrol ederek al (null gelirse "Bilinmiyor" yaz)
                    string ulke = (string)adres["country"] ?? "";
                    string sehir = (string)adres["province"] ?? (string)adres["city"] ?? (string)adres["state"] ?? "";
                    string ilce = (string)adres["town"] ?? (string)adres["district"] ?? (string)adres["county"] ?? "";

                    // Eğer şehir ve ilçe yoksa ama ülke varsa (Örn: Rusya'nın ıssız bozkırları)
                    if (string.IsNullOrEmpty(sehir) && string.IsNullOrEmpty(ilce))
                    {
                        if (!string.IsNullOrEmpty(ulke)) return $"📍 {ulke} (Kırsal/Issız Bölge)";
                        return "🌊 Açık Deniz / Okyanus";
                    }

                    // Temiz bir adres formatı oluştur
                    string sonuc = "📍 ";
                    if (!string.IsNullOrEmpty(ilce)) sonuc += ilce + " / ";
                    if (!string.IsNullOrEmpty(sehir)) sonuc += sehir;
                    if (!string.IsNullOrEmpty(ulke)) sonuc += $" ({ulke})";

                    return sonuc;
                }
            }
            catch
            {
                // İnternet kopuksa veya başka bir hata varsa
                return "⚠️ Konum Bilgisi Alınamadı";
            }
        }
    }
}