using Newtonsoft.Json;
using Spotify_Song_Sync.Classes;
using Spotify_Song_Sync.Models;
using SpotifyAPI.Web;
using SuperSimpleTcp;
using Swan;
using System;
using System.Text;
using System.Threading;
using System.Windows;

namespace Spotify_Song_Sync.Services;

public class TcpService
{
    private Config _config;
    private MainWindow _mainWindow;
    private SimpleTcpClient _client;
    private SpotifyService _spotifyService;

    private bool _ownerLoop = false;
    private Party_Info party_info_old;

    public TcpService(MainWindow mainWindow, Config config, SpotifyService spotifyService)
    {
        _config = config;
        _mainWindow = mainWindow;
        _spotifyService = spotifyService;

        _client = new($"{config.Ip}:8337");
        _client.Events.DataReceived += DataReceived;

        try
        {
            _client.Connect();
        } catch(Exception ex)
        {
            MessageBox.Show($"Error occurred while connecting to the server.\n\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(-1);
        }
    }

    private void DataReceived(object? sender, DataReceivedEventArgs e)
    {
        BaseMessage? baseMessage = JsonConvert.DeserializeObject<BaseMessage>(Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count));
        if (baseMessage == null) return;

        switch (baseMessage.Message)
        {
            case "party_created":

                Message_Text? message = baseMessage.Message_Text;
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.pnlPartyCreate.Visibility = Visibility.Collapsed;
                    _mainWindow.pnlParty.Visibility = Visibility.Visible;
                    _mainWindow.partyInfo.Text = $"Owner\nParty since: {DateTime.Now}\nParty Code: {message?.Text}";
                    _mainWindow.btnDeleteParty.Visibility = Visibility.Visible;
                    _mainWindow.btnLeaveParty.Visibility = Visibility.Collapsed;
                });

                OwnerLoop();
                break;

            case "party_joined":

                Message_Text? joinedData = baseMessage.Message_Text;
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.pnlPartyCreate.Visibility = Visibility.Collapsed;
                    _mainWindow.pnlParty.Visibility = Visibility.Visible;
                    _mainWindow.partyInfo.Text = $"Listener\nParty in since: {DateTime.Now}\nParty Code: {joinedData?.Text}";
                    _mainWindow.btnDeleteParty.Visibility = Visibility.Collapsed;
                    _mainWindow.btnLeaveParty.Visibility = Visibility.Visible;
                });
                break;

            case "party_info":

                Party_Info? partyInfo = baseMessage.Party_Info;
                if (party_info_old == null) party_info_old = partyInfo;

                if(partyInfo.SpotifyIsPlaying != party_info_old.SpotifyIsPlaying)
                {
                    if (partyInfo.SpotifyIsPlaying)
                        _spotifyService.StartPlayback(partyInfo);
                    else _spotifyService.PausePlayback();

                    party_info_old = partyInfo;
                }

                if(partyInfo.SpotifySong != party_info_old.SpotifySong)
                {
                    _spotifyService.StartPlayback(partyInfo);
                    party_info_old = partyInfo;
                }

                if (partyInfo.SpotifySongTimepointMs - party_info_old.SpotifySongTimepointMs > 500 || partyInfo.SpotifySongTimepointMs - party_info_old.SpotifySongTimepointMs < -500)
                {
                    _spotifyService.StartPlayback(partyInfo);
                    party_info_old = partyInfo;
                } else party_info_old = partyInfo;

                break;

            case "party_notfound":

                MessageBox.Show("The Party Code you've used is not existing!", "Party not found", MessageBoxButton.OK, MessageBoxImage.Error);
                break;

            case "party_left":

                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.pnlPartyCreate.Visibility = Visibility.Visible;
                    _mainWindow.pnlParty.Visibility = Visibility.Collapsed;
                });
                break;

            case "party_deleted":

                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.pnlPartyCreate.Visibility = Visibility.Visible;
                    _mainWindow.pnlParty.Visibility = Visibility.Collapsed;
                });

                if (_ownerLoop) _ownerLoop = false;
                MessageBox.Show("The Party you were in got deleted!", "Party deleted", MessageBoxButton.OK, MessageBoxImage.Warning);
                break;
        }
    }

    public void CreateParty()
    {
        BaseMessage baseMessage = new()
        {
            Message = "party_create"
        };
        _client.SendAsync(JsonConvert.SerializeObject(baseMessage));
    }

    public void JoinParty(string code)
    {
        BaseMessage baseMessage = new()
        {
            Message = "party_join",
            Message_Text = new Message_Text() { Text = code }
        };
        _client.SendAsync(JsonConvert.SerializeObject(baseMessage));
    }

    public void DeleteParty()
    {
        BaseMessage baseMessage = new()
        {
            Message = "party_delete"
        };
        _client.SendAsync(JsonConvert.SerializeObject(baseMessage));
    }

    public void LeaveParty()
    {
        BaseMessage baseMessage = new()
        {
            Message = "party_leave"
        };
        _client.SendAsync(JsonConvert.SerializeObject(baseMessage));
    }

    public async void OwnerLoop()
    {
        if (_ownerLoop) return;

        _ownerLoop = true;
        while (_ownerLoop)
        {
            Thread.Sleep(1000);

            CurrentlyPlaying? currentlyPlaying = await _spotifyService.GetCurrentlyPlaying();
            if (currentlyPlaying == null) continue;

            CurrentPlayingInfo? advInfo = JsonConvert.DeserializeObject<CurrentPlayingInfo>(currentlyPlaying.Item.ToJson());
            Party_Info partyInfo = new()
            {
                SpotifySong = advInfo.uri,
                SpotifySongTimepointMs = currentlyPlaying.ProgressMs,
                SpotifyIsPlaying = currentlyPlaying.IsPlaying
            };

            BaseMessage baseMessage = new()
            {
                Message = "party_info",
                Party_Info = partyInfo
            };

            await _client.SendAsync(JsonConvert.SerializeObject(baseMessage));
        }
    }
}