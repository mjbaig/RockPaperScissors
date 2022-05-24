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
        InGame,
        InQueue,
        InMenu,
    }

    private enum GameState
    {
        Ready,
        Waiting,
    }

    private int number { get; set; }

    private readonly ILogger<Player> _logger;

    private IGame? _game;

    private State _state;

    private int _wins;

    private int _losses;

    public Player(ILogger<Player> logger)
    {
        number = 0;
        _logger = logger;
        _state = State.InMenu;
        _wins = 0;
        _losses = 0;
    }

    // Player adds self to a queue in the match maker grain.
    public Task JoinQueue()
    {
        if (this._state == State.InGame)
        {
            throw new Exception("You're already in a game");
        }

        if (this._state == State.InQueue)
        {
            throw new Exception("You're already in the queue");
        }

        var matchMaker = GrainFactory.GetGrain<IMatchMaker>(Guid.Empty);

        _state = State.InQueue;

        return matchMaker.AddToQueue(this);
    }

    // The Game grain calls this method to put the player into a game state.
    public Task StartMatch(IGame game)
    {
        _state = State.InGame;

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
        _logger.LogInformation(matchResponse.PlayerResult == MatchResult.Win ? "You win" : "You didn't win");
        return Task.CompletedTask;
    }

    // The Game grain calls this method to end the game that they're in.
    public Task EndMatch()
    {
        _state = State.InMenu;

        _game = null;

        return Task.CompletedTask;
    }
}