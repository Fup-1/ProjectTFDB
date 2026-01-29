using System.Windows;
using ProjectTFDB.Services;
using ProjectTFDB.ViewModels;

namespace ProjectTFDB;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var http = new HttpService();
        var settings = new SettingsService();
        var schema = new SchemaService();
        var cache = new CacheService();
        var steam = new SteamInventoryService(http);
        var prices = new BackpackPricesService(http);
        var dashboard = new DashboardService(settings, schema, steam, prices, cache);

        DataContext = new MainViewModel(settings, schema, cache, dashboard);
    }
}

