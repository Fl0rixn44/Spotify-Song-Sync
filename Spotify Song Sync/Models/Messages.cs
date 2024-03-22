namespace Spotify_Song_Sync.Models;

public class BaseMessage
{
    public string Message { get; set; }
    public Message_Text Message_Text { get; set; }
    public Party_Info Party_Info { get; set; }
}

public class Message_Text
{
    public string Text { get; set; }
}

public class Party_Info
{
    public string SpotifySong { get; set; }
    public int? SpotifySongTimepointMs { get; set; }
    public bool SpotifyIsPlaying { get; set; }
}