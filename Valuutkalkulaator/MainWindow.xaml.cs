using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using System.Text.Json;



namespace Valuutkalkulaator
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _http = new HttpClient();
        private Dictionary<string, decimal> _rates = new Dictionary<string, decimal>();

        public MainWindow()
        {
            InitializeComponent();
            _ = LoadCurrenciesAsync();
        }

        private async Task LoadCurrenciesAsync()
        {
            try
            {
                ResultText.Text = "Laen andmeid...";
                FromCurrency.IsEnabled = false;
                ToCurrency.IsEnabled = false;

                string url = "https://open.er-api.com/v6/latest/USD";
                var response = await _http.GetStringAsync(url);
                var doc = JsonDocument.Parse(response);

                if (doc.RootElement.GetProperty("result").GetString() != "success")
                {
                    MessageBox.Show("API viga: ei saanud kursse.");
                    return;
                }

                var rates = doc.RootElement.GetProperty("rates");
                _rates.Clear();

                foreach (var item in rates.EnumerateObject())
                {
                    _rates[item.Name] = item.Value.GetDecimal();
                }

                var currencyList = new List<string>(_rates.Keys);
                currencyList.Sort();

                FromCurrency.ItemsSource = currencyList;
                ToCurrency.ItemsSource = currencyList;

                FromCurrency.SelectedItem = "USD";
                ToCurrency.SelectedItem = "EUR";

                ResultText.Text = "Andmed laetud.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Viga andmete laadimisel: {ex.Message}");
                ResultText.Text = "Viga laadimisel.";
            }
            finally
            {
                FromCurrency.IsEnabled = true;
                ToCurrency.IsEnabled = true;
            }
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountBox.Text, out decimal amount))
            {
                string from = FromCurrency.SelectedItem?.ToString();
                string to = ToCurrency.SelectedItem?.ToString();

                if (from == null || to == null)
                {
                    MessageBox.Show("Palun vali valuutad!");
                    return;
                }

                ConvertButton.IsEnabled = false;
                ConvertButton.Content = "Arvutan...";
                ResultText.Text = "Laen andmeid...";

                decimal result = await GetExchangeResult(amount, from, to);

                ResultText.Text = $"{amount} {from} = {result:F2} {to}";
                ConvertButton.IsEnabled = true;
                ConvertButton.Content = "Arvuta";
            }
            else
            {
                MessageBox.Show("Sisesta korrektne summa!");
            }
        }

        private async Task<decimal> GetExchangeResult(decimal amount, string from, string to)
        {
            try
            {
                string url = $"https://open.er-api.com/v6/latest/{from}";
                var response = await _http.GetStringAsync(url);
                var doc = JsonDocument.Parse(response);

                if (doc.RootElement.GetProperty("result").GetString() != "success")
                {
                    return 0;
                }

                decimal rate = doc.RootElement.GetProperty("rates").GetProperty(to).GetDecimal();
                return amount * rate;
            }
            catch
            {
                MessageBox.Show("Viga API ühendamisel!");
                return 0;
            }
        }
    }
}
