using GameServer.Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace GameServer.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<TestController> _logger;

    private readonly IClusterClient _client;

    public TestController(ILogger<TestController> logger, IClusterClient client)
    {
        _logger = logger;
        _client = client;
    }

    [HttpGet(Name = "GetNumber")]
    public async Task<int> Get()
    {
        var player = _client.GetGrain<IPlayer>("_key");

        var number = await player.GetNumber();

        return number;
    }
}