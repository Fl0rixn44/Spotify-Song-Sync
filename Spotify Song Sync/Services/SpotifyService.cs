using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Windows;
using Spotify_Song_Sync.Classes;
using System.Windows.Media.Imaging;
using RestSharp;
using System.Linq;
using System.IO;
using Spotify_Song_Sync.Models;
using Newtonsoft.Json;
using Swan;

namespace Spotify_Song_Sync.Services;

public class SpotifyService
{
    private MainWindow _mainWindow;

    private readonly Config _config;
    private RestClient _restClient = new();
    private SpotifyClient _spotifyClient;
    private EmbedIOAuthServer _server;

    public SpotifyService(MainWindow mainWindow, Config config)
    {
        _mainWindow = mainWindow;
        _config = config;
    }

    public async void StartAuth()
    {
        _mainWindow.btnAuth.IsEnabled = false;

        _server = new EmbedIOAuthServer(new Uri("http://localhost:5543/callback"), 5543);
        await _server.Start();

        _server.ImplictGrantReceived += OnImplicitGrantReceived;
        _server.ErrorReceived += OnErrorReceived;

        LoginRequest request = new LoginRequest(_server.BaseUri, _config.ClientId, LoginRequest.ResponseType.Token)
        {
            Scope = new List<string>
            {
                Scopes.UserReadPlaybackState,
                Scopes.UserModifyPlaybackState,
                Scopes.UserReadCurrentlyPlaying,
                Scopes.Streaming,
                Scopes.AppRemoteControl,
                Scopes.UserReadEmail,
                Scopes.UserReadPrivate,
                Scopes.UserLibraryModify,
                Scopes.UserLibraryRead,
                Scopes.UserTopRead,
                Scopes.UserReadPlaybackPosition,
                Scopes.UserReadRecentlyPlayed
            }
        };

        BrowserUtil.Open(request.ToUri());
    }

    private async Task OnImplicitGrantReceived(object sender, ImplictGrantResponse response)
    {
        await _server.Stop();
        _spotifyClient = new SpotifyClient(response.AccessToken);

        Device? currentDevice = await GetActiveDevice();
        if (currentDevice == null)
        {
            MessageBox.Show($"No Device detected, please start Spotify on any device with your connected account!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _mainWindow.Dispatcher.Invoke(() => _mainWindow.btnAuth.IsEnabled = true);

            return;
        }

        PrivateUser user = await _spotifyClient.UserProfile.Current();
        _mainWindow.Dispatcher.Invoke(() => LoadSpotifyInformations(user, currentDevice));
    }

    private async Task OnErrorReceived(object sender, string error, string state)
    {
        MessageBox.Show($"Error occurred during authentication.\n\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        _mainWindow.Dispatcher.Invoke(() => _mainWindow.btnAuth.IsEnabled = true);
        await _server.Stop();
    }

    private async void LoadSpotifyInformations(PrivateUser privateUser, Device device)
    {
        Image? image = privateUser.Images.FirstOrDefault();
        if (image != null) 
        {
            RestRequest request = new(image.Url);
            RestResponse response = _restClient.ExecuteGet(request);

            if(response.IsSuccessStatusCode)
            {
                using (MemoryStream stream = new(response.RawBytes))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    _mainWindow.spotifyUserPicture.Source = bitmap;
                }
            }
        }

        _mainWindow.spotifyInfo.Visibility = Visibility.Visible;
        _mainWindow.spotifyInfo.Text = $"Welcome {privateUser.DisplayName}!\nCountry: {privateUser.Country}\nDevice: {device.Name}";
        _mainWindow.btnPartyCreate.IsEnabled = true;
        _mainWindow.btnPartyJoin.IsEnabled = true;
        _mainWindow.btnSettings.IsEnabled = false;
    }

    public async Task<Device?> GetActiveDevice()
    {
        DeviceResponse deviceResponse = await _spotifyClient.Player.GetAvailableDevices();
        return deviceResponse.Devices.FirstOrDefault();
    }

    public async Task<CurrentlyPlaying?> GetCurrentlyPlaying() => await _spotifyClient.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest() {});

    public async void StartPlayback(Party_Info partyInfo)
    {
        Device? device = await GetActiveDevice();
        if (device != null)
        {
            PlayerResumePlaybackRequest playbackRequest = new()
            {
                PositionMs = partyInfo.SpotifySongTimepointMs,
                OffsetParam = new PlayerResumePlaybackRequest.Offset { Position = 0 },
                DeviceId = device.Id,
                Uris = new List<string>() { partyInfo.SpotifySong }
            };

            await _spotifyClient.Player.ResumePlayback(playbackRequest);
        }
    }

    public async Task<Party_Info?> GetPartyInfo()
    {
        CurrentlyPlaying? currentlyPlaying = await GetCurrentlyPlaying();
        if (currentlyPlaying == null) return null;

        CurrentPlayingInfo? currentPlayingInfo = JsonConvert.DeserializeObject<CurrentPlayingInfo>(currentlyPlaying.Item.ToJson());
        if (currentPlayingInfo == null) return null;

        return new Party_Info()
        {
            SpotifySong = currentPlayingInfo.uri,
            SpotifySongTimepointMs = currentlyPlaying.ProgressMs,
            SpotifyIsPlaying = currentlyPlaying.IsPlaying
        };
    }

    public void PausePlayback() => _spotifyClient.Player.PausePlayback();
}