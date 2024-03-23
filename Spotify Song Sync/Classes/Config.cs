using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;

namespace Spotify_Song_Sync.Classes;

public class Config
{
    private readonly string _configPath = $@"{Environment.CurrentDirectory}\config.json";

    public string ConfigVersion { get; set; } = "1.0.0";
    public string ClientId { get; set; } = "";
    public string Ip { get; set; } = "45.145.41.236";

    public Config() {}

    public void InitializeConfig()
    {
        try
        {
            if (!File.Exists(_configPath))
                File.WriteAllText(_configPath, JsonConvert.SerializeObject(this));

            LoadConfig();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error while loading config, try to delete the config.\n\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadConfig()
    {
        string configContent = File.ReadAllText(_configPath);
        Config? tempConfig = JsonConvert.DeserializeObject<Config>(configContent);
        if (tempConfig?.ConfigVersion != ConfigVersion)
        {
            ClientId = tempConfig.ClientId;
            Ip = tempConfig.Ip;
            SaveConfig();
        }
        else
        {
            ClientId = tempConfig.ClientId;
            Ip = tempConfig.Ip;
        }
    }

    public void SaveConfig() => File.WriteAllText(_configPath, JsonConvert.SerializeObject(this));
}