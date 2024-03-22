using Spotify_Song_Sync.Classes;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Http;
using System;
using System.Diagnostics;
using System.Windows;

namespace Spotify_Song_Sync;

/// <summary>
/// Interaction logic for ConfigWindow.xaml
/// </summary>
public partial class ConfigWindow
{
    private Config _config;

    public ConfigWindow(Config config)
    {
        InitializeComponent();

        _config = config;
        txtSecret.Text = _config.ClientId;
    }

    private void btnApp_Click(object sender, RoutedEventArgs e) => BrowserUtil.Open(new Uri("https://developer.spotify.com/dashboard"));
    private void btnCopyUri_Click(object sender, RoutedEventArgs e) => Clipboard.SetText("http://localhost:5543/callback");
    private void btnPasteSecret_Click(object sender, RoutedEventArgs e) => txtSecret.Text = Clipboard.GetText();

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        _config.ClientId = txtSecret.Text;
        _config.SaveConfig();
        this.Close();
    }

    private void btnExit_Click(object sender, RoutedEventArgs e) => this.Close();
}