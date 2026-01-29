using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ProjectTFDB.Helpers;
using ProjectTFDB.Models;
using ProjectTFDB.Services;

namespace ProjectTFDB.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly SchemaService _schemaService;
    private readonly CacheService _cacheService;
    private readonly DashboardService _dashboardService;

    private string _steamId64 = "";
    private string _steamApiKey = "";
    private string _backpackApiKey = "";

    private string _steamStatus = "";
    private string _pricesStatus = "";
    private string _schemaStatus = "";
    private string _schemaPath = "";
    private int _schemaCount;

    private bool _isBusy;
    private string _lastError = "";

    private double _keyRef = 60;
    private double _totalRef;
    private int _totalValueKeysPart;
    private double _totalValueRefPart;
    private int _totalStacks;
    private int _totalItems;
    private int _pricedStacks;
    private int _dealsCount;
    private int _keysCount;
    private int _refinedCount;

    public MainViewModel(
        SettingsService settingsService,
        SchemaService schemaService,
        CacheService cacheService,
        DashboardService dashboardService)
    {
        _settingsService = settingsService;
        _schemaService = schemaService;
        _cacheService = cacheService;
        _dashboardService = dashboardService;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync, () => !IsBusy);
        ImportSchemaCommand = new AsyncRelayCommand(ImportSchemaAsync, () => !IsBusy);
        OpenCacheFolderCommand = new RelayCommand(() => _cacheService.OpenCacheFolder());

        _ = LoadOnStartupAsync();
    }

    public string SteamId64
    {
        get => _steamId64;
        set => SetProperty(ref _steamId64, value);
    }

    public string SteamApiKey
    {
        get => _steamApiKey;
        set => SetProperty(ref _steamApiKey, value);
    }

    public string BackpackApiKey
    {
        get => _backpackApiKey;
        set => SetProperty(ref _backpackApiKey, value);
    }

    public string SteamStatus
    {
        get => _steamStatus;
        private set => SetProperty(ref _steamStatus, value);
    }

    public string PricesStatus
    {
        get => _pricesStatus;
        private set => SetProperty(ref _pricesStatus, value);
    }

    public string SchemaStatus
    {
        get => _schemaStatus;
        private set => SetProperty(ref _schemaStatus, value);
    }

    public string SchemaPath
    {
        get => _schemaPath;
        private set => SetProperty(ref _schemaPath, value);
    }

    public int SchemaCount
    {
        get => _schemaCount;
        private set => SetProperty(ref _schemaCount, value);
    }

    public double KeyRef
    {
        get => _keyRef;
        private set => SetProperty(ref _keyRef, value);
    }

    public double TotalRef
    {
        get => _totalRef;
        private set => SetProperty(ref _totalRef, value);
    }

    public int TotalValueKeysPart
    {
        get => _totalValueKeysPart;
        private set => SetProperty(ref _totalValueKeysPart, value);
    }

    public double TotalValueRefPart
    {
        get => _totalValueRefPart;
        private set => SetProperty(ref _totalValueRefPart, value);
    }

    public int TotalStacks
    {
        get => _totalStacks;
        private set => SetProperty(ref _totalStacks, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        private set => SetProperty(ref _totalItems, value);
    }

    public int PricedStacks
    {
        get => _pricedStacks;
        private set => SetProperty(ref _pricedStacks, value);
    }

    public int DealsCount
    {
        get => _dealsCount;
        private set => SetProperty(ref _dealsCount, value);
    }

    public int KeysCount
    {
        get => _keysCount;
        private set => SetProperty(ref _keysCount, value);
    }

    public int RefinedCount
    {
        get => _refinedCount;
        private set => SetProperty(ref _refinedCount, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RefreshCommand.RaiseCanExecuteChanged();
                SaveSettingsCommand.RaiseCanExecuteChanged();
                ImportSchemaCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string LastError
    {
        get => _lastError;
        private set => SetProperty(ref _lastError, value);
    }

    public ObservableCollection<EnrichedItem> Items { get; } = new();
    public ObservableCollection<DealItem> Deals { get; } = new();

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand SaveSettingsCommand { get; }
    public AsyncRelayCommand ImportSchemaCommand { get; }
    public RelayCommand OpenCacheFolderCommand { get; }

    private async Task LoadOnStartupAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            SteamId64 = settings.SteamId64;
            SteamApiKey = settings.SteamApiKey;
            BackpackApiKey = settings.BackpackTfApiKey;

            var cached = await _cacheService.ReadDashboardAsync().ConfigureAwait(true);
            if (cached is not null)
            {
                ApplySnapshot(cached);
            }

            var schemaIndex = await _schemaService.LoadIndexAsync().ConfigureAwait(true);
            SchemaStatus = schemaIndex is null ? "schema missing (import schema_items.json)" : "schema loaded";
            SchemaPath = schemaIndex?.Path ?? "";
            SchemaCount = schemaIndex?.Map.Count ?? 0;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            IsBusy = true;
            LastError = "";

            var settings = new AppSettings
            {
                SteamId64 = (SteamId64 ?? "").Trim(),
                SteamApiKey = (SteamApiKey ?? "").Trim(),
                BackpackTfApiKey = (BackpackApiKey ?? "").Trim()
            };
            await _settingsService.SaveAsync(settings).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ImportSchemaAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select schema_items.json",
            Filter = "schema_items.json|schema_items.json|JSON files|*.json|All files|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog(Application.Current?.MainWindow) != true) return;

        try
        {
            IsBusy = true;
            LastError = "";

            await _schemaService.ImportSchemaFromFileAsync(dlg.FileName).ConfigureAwait(true);

            var schemaIndex = await _schemaService.LoadIndexAsync().ConfigureAwait(true);
            SchemaStatus = schemaIndex is null ? "schema missing (import schema_items.json)" : "schema loaded";
            SchemaPath = schemaIndex?.Path ?? "";
            SchemaCount = schemaIndex?.Map.Count ?? 0;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            LastError = "";

            var settings = new AppSettings
            {
                SteamId64 = (SteamId64 ?? "").Trim(),
                SteamApiKey = (SteamApiKey ?? "").Trim(),
                BackpackTfApiKey = (BackpackApiKey ?? "").Trim()
            };
            await _settingsService.SaveAsync(settings).ConfigureAwait(true);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var snapshot = await _dashboardService.RefreshAsync(cts.Token).ConfigureAwait(true);
            ApplySnapshot(snapshot);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySnapshot(DashboardSnapshot snapshot)
    {
        SteamStatus = snapshot.SteamStatus;
        PricesStatus = snapshot.PricesStatus;
        SchemaStatus = snapshot.SchemaStatus;
        SchemaPath = snapshot.SchemaPath ?? "";
        SchemaCount = snapshot.SchemaCount;

        KeyRef = snapshot.KeyRef;
        TotalRef = snapshot.TotalRef;
        TotalValueKeysPart = snapshot.TotalValueKeysPart;
        TotalValueRefPart = snapshot.TotalValueRefPart;
        TotalStacks = snapshot.TotalStacks;
        TotalItems = snapshot.TotalItems;
        PricedStacks = snapshot.PricedStacks;
        DealsCount = snapshot.DealsCount;
        KeysCount = snapshot.KeysCount;
        RefinedCount = snapshot.RefinedCount;

        Items.Clear();
        foreach (var it in snapshot.Items) Items.Add(it);

        Deals.Clear();
        foreach (var d in snapshot.Deals) Deals.Add(d);
    }
}

