using GameServer.Exceptions;
using GameServer.Hubs;
using Orleans;

namespace GameServer.Grains;

public interface IPlayer : IGrainWithStringKey
{
    Task JoinMatchMakerQueue();

    Task StartMatchFromGameServer(IGame game);

    Task SendResultFromGameServer(MatchResponse matchResponse);

    Task<PlayerState> GetState();

    Task<PlayerGameState> GetGameState();

    Task SendMoveToGameServer(RockPaperScissorsMove move);

    Task<MatchResponse> GetLastMatchResponse();

    Task<List<AvailableMethods>> GetAvailableMethods();

    Task Subscribe(string connectionId);
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

public enum AvailableMethods
{
    SendMove,
    GetState,
    GetLastMatchResponse,
    JoinQueue,
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

    private string? _connectionId;

    private IGameHub _hub;

    public Player(ILogger<Player> logger, IGameHub hub)
    {
        number = 0;
        _logger = logger;
        _playerState = PlayerState.InMenu;
        _wins = 0;
        _losses = 0;
        _hub = hub;
    }

    public MatchResponse? LastMatchResponse { get; set; }

    public Task<List<AvailableMethods>> GetAvailableMethods()
    {
        var availableMethods = (_playerState, _playerGameState) switch
        {
            (PlayerState.InGame, PlayerGameState.Ready) => new List<AvailableMethods>()
            {
                AvailableMethods.SendMove,
                AvailableMethods.GetState,
                AvailableMethods.GetLastMatchResponse
            },

            (PlayerState.InGame, PlayerGameState.Waiting) => new List<AvailableMethods>()
            {
                AvailableMethods.GetState,
                AvailableMethods.GetLastMatchResponse
            },

            (PlayerState.InMenu, _) => new List<AvailableMethods>()
            {
                AvailableMethods.JoinQueue,
                AvailableMethods.GetState,
                AvailableMethods.GetLastMatchResponse
            },

            (PlayerState.InQueue, _) => new List<AvailableMethods>()
            {
                AvailableMethods.GetState,
                AvailableMethods.GetLastMatchResponse
            },

            (_, _) => new List<AvailableMethods>() { AvailableMethods.GetState },
        };

        return Task.FromResult(availableMethods);
    }

    public Task Subscribe(string connectionId)
    {
        _connectionId = connectionId;
        return Task.CompletedTask;
    }

    private void NotifySubscribers()
    {
        if (_connectionId != null)
        {
            _hub.ReceivePlayerStatus(_playerState, _playerGameState, _connectionId);
        }
        else
        {
            throw new Exception("You have no connection id... how?");
        }
    }

    private void ChangePlayerState(PlayerState playerState)
    {
        _playerState = playerState;
        //TODO notify observer
        NotifySubscribers();
    }

    private void ChangePlayerGameState(PlayerGameState playerGameState)
    {
        _playerGameState = playerGameState;
        //TODO notify observer
        NotifySubscribers();
    }

    // Player adds self to a queue in the match maker grain.
    public Task JoinMatchMakerQueue()
    {

        if (_connectionId == null)
        {
            throw new Exception("no context");
        }

        if (this._playerState == PlayerState.InGame)
        {
            throw new AlreadyInGameException();
        }

        if (this._playerState == PlayerState.InQueue)
        {
            throw new AlreadyInQueueException();
        }

        var matchMaker = GrainFactory.GetGrain<IMatchMaker>(Guid.Empty);

        ChangePlayerState(PlayerState.InQueue);

        return matchMaker.AddToQueue(this);
    }

    // The Game grain calls this method to put the player into a game state.
    public Task StartMatchFromGameServer(IGame game)
    {
        _game = game;

        _logger.LogInformation("Joined Game");

        ChangePlayerState(PlayerState.InGame);

        ChangePlayerGameState(PlayerGameState.Ready);

        return Task.CompletedTask;
    }

    // This player sends a move to the Game grain and enter a waiting state.
    public Task SendMoveToGameServer(RockPaperScissorsMove move)
    {
        if (_game != null)
        {
            _game.SubmitMove(this.GetPrimaryKeyString(), move);
            ChangePlayerGameState(PlayerGameState.Waiting);
        }
        else
        {
            throw new AlreadyInGameException();
        }

        return Task.CompletedTask;
    }

    // The Game grain calls this method to send the player the match results
    public Task SendResultFromGameServer(MatchResponse matchResponse)
    {
        LastMatchResponse = matchResponse;

        if (matchResponse.GameState == GameState.Ended)
        {
            ChangePlayerState(PlayerState.InMenu);
            _game = null;
        }
        else
        {
            ChangePlayerGameState(PlayerGameState.Ready);
        }

        _logger.LogInformation(matchResponse.PlayerResult == MatchResult.Win ? "You win" : "You didn't win");
        return Task.CompletedTask;
    }

    public Task<PlayerState> GetState()
    {
        return Task.FromResult<PlayerState>(_playerState);
    }

    public Task<MatchResponse> GetLastMatchResponse()
    {
        if (LastMatchResponse != null)
        {
            return Task.FromResult(LastMatchResponse);
        }

        throw new NeverPlayedAGameException();
    }

    public Task<PlayerGameState> GetGameState()
    {
        return Task.FromResult(_playerGameState);
    }
}