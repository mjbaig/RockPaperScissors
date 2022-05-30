using GameServer.Grains;
using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;

namespace GameServer.Hubs;

public interface IGameHub
{
    Task SendMove(string playerName, RockPaperScissorsMove rockPaperScissorsMove);
    Task GetState(string playerId);
    Task<MatchResponse> GetLastMatchResponse(string playerName);
    Task JoinQueue(string playerName);
    Task<List<AvailableMethods>> GetAvailableMethods(string playerName);

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


    public async Task SendMove(string playerName, RockPaperScissorsMove rockPaperScissorsMove)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        await player.SendMoveToGameServer(rockPaperScissorsMove);
    }

    public async Task GetState(string playerId)
    {
        var connectionId = Context.ConnectionId;

        var player = _client.GetGrain<IPlayer>(playerId);

        var state = await player.GetState();

        var gameState = await player.GetGameState();

        await Clients.Clients(new List<string>() { connectionId }).SendAsync("GetState", state, gameState);
    }

    public async Task<MatchResponse> GetLastMatchResponse(string playerName)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        return await player.GetLastMatchResponse();

    }

    public async Task JoinQueue(string playerName)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        await player.JoinMatchMakerQueue();
    }

    public async Task<List<AvailableMethods>> GetAvailableMethods(string playerName)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        return await player.GetAvailableMethods();
    }

    public async Task SignIn(string playerName)
    {
        var player = _client.GetGrain<IPlayer>(playerName);

        await player.Subscribe(Context.ConnectionId);
    }


    public async Task ReceivePlayerStatus(PlayerState playerState, PlayerGameState playerGameState, string connectionId)
    {
        await Clients.Clients(new List<string> { connectionId }).SendAsync("GetState", playerState, playerGameState);
    }
}