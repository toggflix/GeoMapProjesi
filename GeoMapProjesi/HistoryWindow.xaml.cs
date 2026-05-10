using System.Windows;
using GeoMapProjesi.Services;

namespace GeoMapProjesi
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow()
        {
            InitializeComponent();
            VerileriYukle();
        }

        private void VerileriYukle()
        {
            DatabaseService db = new DatabaseService();
            // DataGrid'in kaynağını veritabanından gelen listeye bağla
            GridGecmis.ItemsSource = db.GecmisiGetir();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}