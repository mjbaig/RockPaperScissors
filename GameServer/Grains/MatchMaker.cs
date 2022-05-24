using Orleans;
using Orleans.Concurrency;

namespace GameServer.Grains;

public interface IMatchMaker: IGrainWithGuidKey
{
    Task AddToQueue(IPlayer player);
}

public class MatchMaker : Grain, IMatchMaker
{

    private Queue<IPlayer> _players;

    private readonly ILogger<MatchMaker> _logger;

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
            Guid newMatchKey = Guid.NewGuid();

            IGame game = GrainFactory.GetGrain<IGame>(newMatchKey);

            game.RegisterPlayer(_players.Dequeue());
            
            game.RegisterPlayer(_players.Dequeue());
        }

        return Task.CompletedTask;
    }

}