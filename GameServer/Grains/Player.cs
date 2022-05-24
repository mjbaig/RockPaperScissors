using Orleans;

namespace GameServer.Grains;

public interface IPlayer : IGrainWithStringKey
{
    Task JoinQueue();

    Task StartMatch(IGame game);

    Task RoundResult(MatchResponse matchResponse);

    Task<PlayerState> GetState();

    Task<PlayerGameState> GetGameState();

    Task SendMove(RockPaperScissorsMove move);
}

public enum PlayerState
{
    InGame,
    InQueue,
    InMenu,
}

public enum PlayerGameState
{
    Ready,
    Waiting,
}

public class Player : Grain, IPlayer
{
    private int number { get; set; }

    private readonly ILogger<Player> _logger;

    private IGame? _game;

    private PlayerState _playerState;

    private PlayerGameState _playerGameState;

    private int _wins;

    private int _losses;

    public Player(ILogger<Player> logger)
    {
        number = 0;
        _logger = logger;
        _playerState = PlayerState.InMenu;
        _wins = 0;
        _losses = 0;
    }

    // Player adds self to a queue in the match maker grain.
    public Task JoinQueue()
    {
        if (this._playerState == PlayerState.InGame)
        {
            throw new Exception("You're already in a game");
        }

        if (this._playerState == PlayerState.InQueue)
        {
            throw new Exception("You're already in the queue");
        }

        var matchMaker = GrainFactory.GetGrain<IMatchMaker>(Guid.Empty);

        _playerState = PlayerState.InQueue;

        return matchMaker.AddToQueue(this);
    }

    // The Game grain calls this method to put the player into a game state.
    public Task StartMatch(IGame game)
    {
        _playerState = PlayerState.InGame;

        _game = game;

        _playerGameState = PlayerGameState.Ready;

        _logger.LogInformation("Joined Game");

        return Task.CompletedTask;
    }

    // This player sends a move to the Game grain and enter a waiting state.
    public Task SendMove(RockPaperScissorsMove move)
    {
        if (_game != null)
        {
            _game.SubmitMove(this.GetPrimaryKeyString(), move);

            _playerGameState = PlayerGameState.Waiting;
        }
        else
        {
            throw new Exception("You weren't in a game dumbutt");
        }

        return Task.CompletedTask;
    }

    // The Game grain calls this method to send the player the match results
    public Task RoundResult(MatchResponse matchResponse)
    {
        if (matchResponse.GameState == Grains.GameState.Ended)
        {
            _playerState = PlayerState.InMenu;
            _game = null;
        }
        else
        {
            _playerGameState = PlayerGameState.Ready;
        }

        _logger.LogInformation(matchResponse.PlayerResult == MatchResult.Win ? "You win" : "You didn't win");
        return Task.CompletedTask;
    }

    public Task<PlayerState> GetState()
    {
        return Task.FromResult<PlayerState>(_playerState);
    }
    
    public Task<PlayerGameState> GetGameState()
    {
        return Task.FromResult(_playerGameState);
    }
}