using GameServer.Hubs;
using Orleans;

namespace GameServer.Grains;

public interface IMetrics : IGrainWithGuidKey
{
    Task SendMetrics(string metrics);
}

public class Metrics : Grain, IMetrics
{
    private readonly ILogger<Metrics> _logger;

    private readonly IRockPaperScissorsClientContext _context;

    public Metrics(ILogger<Metrics> logger, IRockPaperScissorsClientContext context)
    {
        _logger = logger;
        _context = context;
    }


    public Task SendMetrics(string metrics)
    {
        throw new NotImplementedException();
    }
}