using GameServer.Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace GameServer.Controllers;

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{
    
    private readonly ILogger<TestController> _logger;

    private readonly IClusterClient _client;

    public TestController(ILogger<TestController> logger, IClusterClient client)
    {
        _logger = logger;
        _client = client;
    }

    [HttpGet("{id}")]
    public async Task<string> Get(string id)
    {
        var player = _client.GetGrain<IPlayer>(id);

        await player.JoinMatchMakerQueue();

        return "joined Queue";
    }
}