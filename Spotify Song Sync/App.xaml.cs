using SpotifyAPI.Web.Auth;
using System;
using System.Net;
using System.Windows;

namespace Spotify_Song_Sync;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application 
{
    public App()
    {
        string version = new WebClient().DownloadString("https://raw.githubusercontent.com/Fl0rixn44/Spotify-Song-Sync/master/Spotify%20Song%20Sync/version.txt");
        if(version != "1.0.1")
        {
            MessageBox.Show("A newer version of Spotify Song Sync is available.", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
            BrowserUtil.Open(new Uri("https://github.com/Fl0rixn44/Spotify-Song-Sync/releases"));
            Environment.Exit(-1);
        }
    }
}