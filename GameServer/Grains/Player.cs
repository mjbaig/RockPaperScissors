using Orleans;

namespace GameServer.Grains;

public interface IPlayer : IGrainWithStringKey
{
    Task JoinQueue();

    Task StartMatch(IGame game);

    Task EndMatch();

    Task RoundResult(MatchResponse matchResponse);

    Task SendMove();
}

public class Player : Grain, IPlayer
{
    private enum State
    {
        IN_GAME,
        IN_QUEUE,
        IN_MENU,
    }

    private enum GameState
    {
        READY,
        WAITING,
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

    public Task SendMove()
    {
        
        return Task.CompletedTask;
    }

    public Task RoundResult(MatchResponse matchResponse)
    {
        
        return Task.CompletedTask;
    }

    public Task EndMatch()
    {
        _state = State.IN_MENU;

        _game = null;
        
        return Task.CompletedTask;
    }
}