using Microsoft.AspNetCore.SignalR;

namespace GameServer.Controllers;

public class RockPaperScissorsHub : Hub<IGameClient>
{

    public Task GetPlayerState(string playerId)
    {
        return Clients.All.GetState();
    }

}

