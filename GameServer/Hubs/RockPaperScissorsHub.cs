using GameServer.Grains;
using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;

namespace GameServer.Hubs;

public interface IGameHub
{
    Task SendMove();
    Task GetState(string playerId);
    Task<MatchResponse> GetLastMatchResponse();
    Task JoinQueue(string playerName);
    Task<AvailableMethods> GetAvailableMethods();

    Task SignIn(string playerName);

    Task ReceivePlayerStatus(PlayerState playerState, PlayerGameState playerGameState, string connectionId);
}

public class RockPaperScissorsHub : Hub, IGameHub
{

    private IClusterClient _client;

    private ILogger<RockPaperScissorsHub> _logger;
    
    public RockPaperScissorsHub(ILogger<RockPaperScissorsHub> logger, IClusterClient client)
    {
        _logger = logger;
        _client = client;
    }


    public async Task SendMove()
    {
        throw new NotImplementedException();
    }

    public async Task GetState(string playerId)
    {
        var connectionId = Context.ConnectionId;

        var player = _client.GetGrain<IPlayer>(playerId);

        var state = await player.GetState();

        var gameState = await player.GetGameState();

        await Clients.Clients(new List<string>() { connectionId }).SendAsync("GetState", state, gameState);
    }

    public async Task<MatchResponse> GetLastMatchResponse()
    {
        throw new NotImplementedException();
    }

    public async Task JoinQueue(string playerName)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        await player.JoinMatchMakerQueue();

    }

    public async Task<AvailableMethods> GetAvailableMethods()
    {
        throw new NotImplementedException();
    }

    public async Task SignIn(string playerName)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        await player.Subscribe(Context.ConnectionId);
    }
    

    public async Task ReceivePlayerStatus(PlayerState playerState, PlayerGameState playerGameState, string connectionId)
    {
        _logger.LogInformation("This works {}", connectionId);
        await Clients.Clients(new List<string> { connectionId }).SendAsync("GetState", playerState, playerGameState);
    }
}


