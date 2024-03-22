using Newtonsoft.Json;
using SpotifySongSync_Server.Models;
using SuperSimpleTcp;
using System.Text;

namespace SpotifySongSync_Server.Services;

public class TcpService
{
    private SimpleTcpServer _server;
    public List<Party> partys = new();

    public TcpService(string ipPort)
    {
        _server = new SimpleTcpServer(ipPort);
        _server.Events.DataReceived += DataReceived;
        _server.Events.ClientDisconnected += ClientDisconnected;
        _server.Start();
    }

    private void ClientDisconnected(object? sender, ConnectionEventArgs e)
    {
        if (!IsInParty(e.IpPort)) return;

        Party? currentParty = GetParty(e.IpPort);
        if(currentParty == null) return;

        if(IsPartyOwner(e.IpPort))
        {
            BaseMessage party_deleted = new()
            {
                Message = "party_deleted"
            };
            currentParty.Member.ForEach(member => _server.SendAsync(member, JsonConvert.SerializeObject(party_deleted)));

            partys.Remove(currentParty);
        } else currentParty.Member.Remove(e.IpPort);
    }

    private void DataReceived(object? sender, DataReceivedEventArgs e)
    {
        BaseMessage? baseMessage = JsonConvert.DeserializeObject<BaseMessage>(Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count));
        if (baseMessage == null) return;

        //TODO: Check if connection stay alive, if not use ping pong

        switch (baseMessage.Message)
        {
            case "party_info":

                if (!IsInParty(e.IpPort)) return;
                if (!IsPartyOwner(e.IpPort)) return;

                Party_Info party_info = baseMessage.Party_Info;
                BaseMessage baseMessage_info = new()
                {
                    Message = "party_info",
                    Party_Info = party_info
                };

                Party? currentParty_info = GetParty(e.IpPort);
                if (currentParty_info != null)
                    currentParty_info.Member.ForEach(member => _server.SendAsync(member, JsonConvert.SerializeObject(baseMessage_info)));

                break;
            case "party_create":

                if (IsInParty(e.IpPort)) return;

                string partyCode = CreatePartyCode();
                partys.Add(new Party
                {
                    Code = partyCode,
                    Owner = e.IpPort,
                    Member = new()
                });

                BaseMessage message = new()
                {
                    Message = "party_created",
                    Message_Text = new Message_Text { Text = partyCode }
                };
                _server.SendAsync(e.IpPort, JsonConvert.SerializeObject(message));

                break;
            case "party_join":

                if (IsInParty(e.IpPort)) return;

                Message_Text codeData = baseMessage.Message_Text;
                string code = codeData.Text;

                Party? party = GetPartyByCode(code);
                if(party != null)
                {
                    party.Member.Add(e.IpPort);
                    _server.SendAsync(e.IpPort, JsonConvert.SerializeObject(new BaseMessage
                    {
                        Message = "party_joined",
                        Message_Text = new Message_Text { Text = code }
                    }));
                } else _server.SendAsync(e.IpPort, JsonConvert.SerializeObject(new BaseMessage
                {
                    Message = "party_notfound"
                }));

                break;
            case "party_delete":

                if (!IsInParty(e.IpPort)) return;
                if (!IsPartyOwner(e.IpPort)) return;

                Party? currentParty = GetParty(e.IpPort);
                if(currentParty != null)
                {
                    BaseMessage party_deleted = new()
                    {
                        Message = "party_deleted"
                    };

                    currentParty.Member.ForEach(member => _server.SendAsync(member, JsonConvert.SerializeObject(party_deleted)));

                    partys.Remove(currentParty);
                    _server.SendAsync(e.IpPort, JsonConvert.SerializeObject(party_deleted));
                }

                break;
            case "party_leave":

                if (!IsInParty(e.IpPort)) return;

                Party? currentPartyLeave = GetParty(e.IpPort);
                if (currentPartyLeave != null)
                {
                    currentPartyLeave.Member.Remove(e.IpPort);
                    _server.SendAsync(e.IpPort, JsonConvert.SerializeObject(new BaseMessage
                    {
                        Message = "party_left"
                    }));
                }

                break;
        }
    }

    //TODO: Optimize and check if it works with member check
    private Party? GetParty(string ipPort)
    {
        if (partys.FirstOrDefault(x => x.Owner == ipPort) != null) return partys.FirstOrDefault(x => x.Owner == ipPort);
        if (partys.FirstOrDefault(x => x.Member.FirstOrDefault(m => m == ipPort) != null) != null) return partys.FirstOrDefault(x => x.Member.FirstOrDefault(m => m == ipPort) != null);

        return null;
    }

    private Party? GetPartyByCode(string code) => partys.FirstOrDefault(x => x.Code == code);
    private bool IsPartyOwner(string ipPort) => partys.FirstOrDefault(x => x.Owner == ipPort) != null ? true : false;

    private bool IsInParty(string ipPort)
    {
        if (partys.FirstOrDefault(x => x.Owner == ipPort) != null) return true;
        if (partys.FirstOrDefault(x => x.Member.FirstOrDefault(m => m == ipPort) != null) != null) return true;

        return false;
    }

    private readonly Random _random = new Random();
    private string CreatePartyCode()
    {
        string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string _result = new string(Enumerable.Repeat(_chars, 7).Select(s => s[_random.Next(s.Length)]).ToArray());

        return _result;
    }
}