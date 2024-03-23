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

    private async void DataReceived(object? sender, DataReceivedEventArgs e)
    {
        BaseMessage? baseMessage = JsonConvert.DeserializeObject<BaseMessage>(Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count));
        if (baseMessage == null) return;

        switch (baseMessage.Message)
        {
            case "party_created":

                Message_Text? partyCreatedMessage = baseMessage.Message_Text;
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.pnlPartyCreate.Visibility = Visibility.Collapsed;
                    _mainWindow.pnlParty.Visibility = Visibility.Visible;
                    _mainWindow.partyInfo.Text = $"Owner\nParty since: {DateTime.Now}\nParty Code: {partyCreatedMessage?.Text}";
                    _mainWindow.btnDeleteParty.Visibility = Visibility.Visible;
                    _mainWindow.btnLeaveParty.Visibility = Visibility.Collapsed;
                });

                OwnerLoop();
                break;

            case "party_joined":

                Message_Text? partyJoinedMessage = baseMessage.Message_Text;
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.pnlPartyCreate.Visibility = Visibility.Collapsed;
                    _mainWindow.pnlParty.Visibility = Visibility.Visible;
                    _mainWindow.partyInfo.Text = $"Listener\nParty in since: {DateTime.Now}\nParty Code: {partyJoinedMessage?.Text}";
                    _mainWindow.btnDeleteParty.Visibility = Visibility.Collapsed;
                    _mainWindow.btnLeaveParty.Visibility = Visibility.Visible;
                });
                break;

            case "party_info":

                Party_Info? partyInfo_server = baseMessage.Party_Info;
                Party_Info? partyInfo_local = await _spotifyService.GetPartyInfo();

                if (partyInfo_local == null) partyInfo_local = new();

                //TimeSpan timeDiff = DateTime.Now - partyInfo_server.MessageSent;
                //if (timeDiff.TotalSeconds > 1.5) return;

                if (partyInfo_server.SpotifyIsPlaying != partyInfo_local.SpotifyIsPlaying)
                {
                    if (partyInfo_server.SpotifyIsPlaying)
                        _spotifyService.StartPlayback(partyInfo_server);
                    else _spotifyService.PausePlayback();
                }

                if(partyInfo_server.SpotifySong != partyInfo_local.SpotifySong)
                    _spotifyService.StartPlayback(partyInfo_server);

                if (partyInfo_server.SpotifyIsPlaying && (partyInfo_server.SpotifySongTimepointMs - partyInfo_local.SpotifySongTimepointMs > 500 || partyInfo_server.SpotifySongTimepointMs - partyInfo_local.SpotifySongTimepointMs < -500))
                    _spotifyService.StartPlayback(partyInfo_server);

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

    public void CreateParty() => _client.SendAsync(JsonConvert.SerializeObject(new BaseMessage { Message = "party_create" }));

    public void JoinParty(string code)
    {
        BaseMessage baseMessage = new()
        {
            Message = "party_join",
            Message_Text = new Message_Text() { Text = code }
        };
        _client.SendAsync(JsonConvert.SerializeObject(baseMessage));
    }

    public void DeleteParty() => _client.SendAsync(JsonConvert.SerializeObject(new BaseMessage { Message = "party_delete" }));
    public void LeaveParty() => _client.SendAsync(JsonConvert.SerializeObject(new BaseMessage { Message = "party_leave" }));

    public async void OwnerLoop()
    {
        if (_ownerLoop) return;

        _ownerLoop = true;
        while (_ownerLoop)
        {
            Thread.Sleep(500);

            CurrentlyPlaying? currentlyPlaying = await _spotifyService.GetCurrentlyPlaying();
            if (currentlyPlaying == null) continue;

            CurrentPlayingInfo? currentPlayingInfo = JsonConvert.DeserializeObject<CurrentPlayingInfo>(currentlyPlaying.Item.ToJson());
            if(currentPlayingInfo == null) continue;

            Party_Info? partyInfo = await _spotifyService.GetPartyInfo();
            if(partyInfo == null) continue;

            BaseMessage baseMessage = new()
            {
                Message = "party_info",
                Party_Info = partyInfo
            };

            await _client.SendAsync(JsonConvert.SerializeObject(baseMessage));
        }
    }
}