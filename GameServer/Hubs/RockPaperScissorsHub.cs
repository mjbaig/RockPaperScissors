using GameServer.Grains;
using Microsoft.AspNetCore.SignalR;
using Orleans;

namespace GameServer.Hubs;

public interface IGameHub
{
    Task SendMove(string playerName, string move);
    Task JoinQueue(string playerName);
    Task SignIn(string playerName);
}

public class RockPaperScissorsHub : Hub, IGameHub
{
    private readonly IClusterClient _client;

    private ILogger<RockPaperScissorsHub> _logger;

    public RockPaperScissorsHub(ILogger<RockPaperScissorsHub> logger, IClusterClient client)
    {
        _logger = logger;
        _client = client;
    }


    public async Task SendMove(string playerName, string move)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        var rockPaperScissorsMove = move.ToLower() switch
        {
            "rock" => RockPaperScissorsMove.Rock,
            "paper" => RockPaperScissorsMove.Paper,
            "scissors" => RockPaperScissorsMove.Scissors,
            _ => throw new Exception("not a valid move")
        };

        await player.SendMoveToGameServer(rockPaperScissorsMove);
    }


    public async Task JoinQueue(string playerName)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        await player.JoinMatchMakerQueue();
    }

    public async Task SignIn(string playerName)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        await player.Subscribe(Context.ConnectionId);
    }
}

public interface IRockPaperScissorsClientContext
{
    Task SendStateToClient(PlayerState playerState, PlayerGameState playerGameState, string connectionId);

    Task SendAvailableMethodsToClient(List<AvailableMethods> availableMethodsList, string connectionId);

    Task SendMatchResponseToClient(MatchResponse matchResponse, string connectionId);
}

public class RockPaperScissorsClientContext : IRockPaperScissorsClientContext
{
    private readonly IHubContext<RockPaperScissorsHub> _hub;

    public RockPaperScissorsClientContext(IHubContext<RockPaperScissorsHub> hub)
    {
        _hub = hub;
    }

    public async Task SendStateToClient(PlayerState playerState, PlayerGameState playerGameState, string connectionId)
    {
        await _hub.Clients.Clients(new List<string> { connectionId })
            .SendAsync("state", playerState.ToString(), playerGameState.ToString());
    }

    public async Task SendAvailableMethodsToClient(List<AvailableMethods> availableMethodsList, string connectionId)
    {
        await _hub.Clients.Clients(new List<string> { connectionId })
            .SendAsync("availableMethods", from x in availableMethodsList select x.ToString());
    }

    public async Task SendMatchResponseToClient(MatchResponse matchResponse, string connectionId)
    {
        await _hub.Clients.Clients(new List<string> { connectionId })
            .SendAsync("matchResponse", matchResponse);
    }
}