using GameServer.Grains;
using Orleans;

namespace GameServer.Services;

public class RockPaperScissorsService
{
    private readonly ILogger<RockPaperScissorsService> _logger;

    private readonly IClusterClient _client;
    
    public RockPaperScissorsService(ILogger<RockPaperScissorsService> logger, IClusterClient client)
    {
        _logger = logger;
        _client = client;
    }

    public Task GetAvailableMethods(string playerId)
    {
        
    }
}