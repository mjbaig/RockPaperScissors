using GameServer.Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace GameServer.Controllers;

[ApiController]
[Route("game")]
public class RockPaperScissorsController: ControllerBase
{
    
    private readonly ILogger<RockPaperScissorsController> _logger;

    private readonly IClusterClient _client;
    
    public RockPaperScissorsController(ILogger<RockPaperScissorsController> logger, IClusterClient client)
    {
        _logger = logger;
        _client = client;
    }
    
    [HttpGet("player/{id}")]
    public async Task<string> Get(string id)
    {
        var player = _client.GetGrain<IPlayer>(id);

        await player.JoinQueue();

        return "joined Queue";
    }
    
}