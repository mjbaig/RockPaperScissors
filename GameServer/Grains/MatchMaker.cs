using Orleans;

namespace GameServer.Grains;

public interface IMatchMaker : IGrainWithGuidKey
{
    Task AddToQueue(IPlayer player);
}

public class MatchMaker : Grain, IMatchMaker
{
    private readonly ILogger<MatchMaker> _logger;

    private readonly Queue<IPlayer> _players;

    public MatchMaker(ILogger<MatchMaker> logger)
    {
        _logger = logger;

        _players = new Queue<IPlayer>();
    }


    public Task AddToQueue(IPlayer player)
    {
        _players.Enqueue(player);

        if (_players.Count >= 2)
        {
            var newMatchKey = Guid.NewGuid();

            var game = GrainFactory.GetGrain<IGame>(newMatchKey);

            game.RegisterPlayer(_players.Dequeue());

            game.RegisterPlayer(_players.Dequeue());
        }

        _logger.LogInformation("Number of players in queue: {}", _players.Count);

        return Task.CompletedTask;
    }
}