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

    // Player adds self to a queue in the match maker grain.
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

    // The Game grain calls this method to put the player into a game state.
    public Task StartMatch(IGame game)
    {
        _state = State.IN_GAME;

        _game = game;

        return Task.CompletedTask;
    }

    // This player sends a move to the Game grain and enter a waiting state.
    public Task SendMove()
    {
        return Task.CompletedTask;
    }

    // The Game grain calls this method to send the player the match results
    public Task RoundResult(MatchResponse matchResponse)
    {
        _logger.LogInformation(matchResponse.PlayerResult == MatchResult.WIN ? "You win" : "You didn't win");
        return Task.CompletedTask;
    }

    // The Game grain calls this method to end the game that they're in.
    public Task EndMatch()
    {
        _state = State.IN_MENU;

        _game = null;

        return Task.CompletedTask;
    }
}