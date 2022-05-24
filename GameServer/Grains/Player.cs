using Orleans;

namespace GameServer.Grains;

public enum Move
{
    ROCK,
    PAPER,
    Scissors,
}

public interface IPlayer : IGrainWithStringKey
{
    public Task JoinQueue();

    public Task StartMatch(IGame game);

    public Task EndMatch();
    
}

public class Player : Grain, IPlayer
{
    private enum State
    {
        IN_GAME,
        IN_QUEUE,
        IN_MENU,
    }

    private int number { get; set; }

    private readonly ILogger<Player> _logger;

    private IGame? _game;

    private State _state;

    public Player(ILogger<Player> logger)
    {
        number = 0;
        _logger = logger;
        _state = State.IN_MENU;
    }

    public Task JoinQueue()
    {
        if (this._state == State.IN_GAME)
        {
            throw new Exception("You're already in a game");
        }
        
        if (this._state == State.IN_QUEUE)
        {
            throw new Exception("You're already in the queue");
        }

        var matchMaker = GrainFactory.GetGrain<IMatchMaker>(Guid.Empty);

        _state = State.IN_QUEUE;
        
        return matchMaker.AddToQueue(this);
    }

    public Task StartMatch(IGame game)
    {
        _state = State.IN_GAME;

        _game = game;
        
        return Task.CompletedTask;
    }

    public Task EndMatch()
    {
        _state = State.IN_MENU;

        _game = null;
        
        return Task.CompletedTask;
    }
}