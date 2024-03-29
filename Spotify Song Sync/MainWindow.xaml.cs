using Spotify_Song_Sync.Classes;
using Spotify_Song_Sync.Services;
using System.Windows;

namespace Spotify_Song_Sync;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private Config _config = new();
    private SpotifyService _spotifyService;
    private TcpService _tcpService;

    public MainWindow()
    {
        InitializeComponent();

        titleBar.Title = $"Spotify Song Sync - v{App.appVersion}";

        _config.InitializeConfig();
        _spotifyService = new(this, _config);
        _tcpService = new(this, _config, _spotifyService);

        LoadWindowConfig();
    }

    private void LoadWindowConfig() => btnAuth.IsEnabled = _config.ClientId == "" ? false : true;

    private void btnSettings_Click(object sender, RoutedEventArgs e)
    {
        new ConfigWindow(_config).ShowDialog();
        LoadWindowConfig();
    }

    private void btnAuth_Click(object sender, RoutedEventArgs e) => _spotifyService.StartAuth();
    private void btnPartyCreate_Click(object sender, RoutedEventArgs e) => _tcpService.CreateParty();
    private void btnLeaveParty_Click(object sender, RoutedEventArgs e) => _tcpService.LeaveParty();
    private void btnDeleteParty_Click(object sender, RoutedEventArgs e) => _tcpService.DeleteParty();
    private void btnPartyJoin_Click(object sender, RoutedEventArgs e)
    {
        if (inpCode.Text == "") return;
        _tcpService.JoinParty(inpCode.Text);
    }
}