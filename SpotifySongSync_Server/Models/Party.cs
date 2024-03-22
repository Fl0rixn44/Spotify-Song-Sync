namespace SpotifySongSync_Server.Models;

public class Party
{
    public string Code { get; set; }
    public string Owner { get; set; }
    public List<string> Member { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
}
