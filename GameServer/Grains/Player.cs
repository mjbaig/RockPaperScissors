using Orleans;

namespace GameServer.Grains;

public interface IPlayer : IGrainWithStringKey
{
    public Task<int> GetNumber();
}

public class Player : Grain, IPlayer
{
    private int number { get; set; }

    private readonly ILogger<Player> _logger;

    public Player(ILogger<Player> logger)
    {
        number = 0;
        _logger = logger;
        _logger.LogCritical("adsf");
    }

    public Task<int> GetNumber()
    {

        var currentNumber = number;

        number++;

        return Task.FromResult(currentNumber);

    }

}