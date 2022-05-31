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

    Task Subscribe(string connectionId);
}

public enum PlayerState
{
    InGame,
    InQueue,
    InMenu
}

public enum PlayerGameState
{
    Ready,
    Waiting
}

public enum AvailableMethods
{
    SendMove,
    GetState,
    GetLastMatchResponse,
    JoinQueue
}

public class PlayerData {
    public PlayerData(int wins, int losses)
    {
        Wins = wins;
        Losses = losses;
    }

    public int Wins { get; set; }
    public int Losses { get; set; }
}

public class Player : Grain, IPlayer
{
    private readonly IRockPaperScissorsClientContext _context;

    private readonly ILogger<Player> _logger;

    private string? _connectionId;

    private IGame? _game;

    private MatchResponse? _lastMatchResponse;

    private PlayerGameState _playerGameState;

    private PlayerState _playerState;

    private readonly IMetrics _metrics;

    private readonly PlayerData _playerData;

    public Player(ILogger<Player> logger, IRockPaperScissorsClientContext context)
    {
        number = 0;
        _logger = logger;
        _playerState = PlayerState.InMenu;
        _context = context;
        // _metrics = GrainFactory.GetGrain<IMetrics>(Guid.Empty);
        _playerData = new PlayerData(0, 0);
    }

    private int number { get; }

    public Task Subscribe(string connectionId)
    {
        _connectionId = connectionId;
        NotifyClientsOfStateChange();
        return Task.CompletedTask;
    }

// Player adds self to a queue in the match maker grain.
    public Task JoinMatchMakerQueue()
    {
        if (_connectionId == null) throw new Exception("no context");

        if (_playerState == PlayerState.InGame) throw new AlreadyInGameException();

        if (_playerState == PlayerState.InQueue) throw new AlreadyInQueueException();

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
        _lastMatchResponse = matchResponse;

        if (matchResponse.GameState == GameState.Ended)
        {
            ChangePlayerState(PlayerState.InMenu);
            _game = null;
            if (_lastMatchResponse.isMatchWon)
            {
                _playerData.Wins += 1;
            }
            else
            {
                _playerData.Losses += 1;
            }
        }
        else
        {
            ChangePlayerGameState(PlayerGameState.Ready);
        }

        NotifyClientsOfStateChange();
        NotifyClientsOfMatchResults();

        return Task.CompletedTask;
    }

    public Task<PlayerState> GetState()
    {
        return Task.FromResult(_playerState);
    }

    public Task<MatchResponse> GetLastMatchResponse()
    {
        if (_lastMatchResponse != null) return Task.FromResult(_lastMatchResponse);

        throw new NeverPlayedAGameException();
    }

    public Task<PlayerGameState> GetGameState()
    {
        return Task.FromResult(_playerGameState);
    }

    private List<AvailableMethods> GetAvailableMethods()
    {
        var availableMethods = (_playerState, _playerGameState) switch
        {
            (PlayerState.InGame, PlayerGameState.Ready) => new List<AvailableMethods>
            {
                AvailableMethods.SendMove
            },

            (PlayerState.InGame, PlayerGameState.Waiting) => new List<AvailableMethods>(),

            (PlayerState.InMenu, _) => new List<AvailableMethods>
            {
                AvailableMethods.JoinQueue
            },

            (PlayerState.InQueue, _) => new List<AvailableMethods>(),

            (_, _) => new List<AvailableMethods>()
        };

        return availableMethods;
    }

    private async void NotifyClientsOfStateChange()
    {
        if (_connectionId != null)
        {
            await _context.SendStateToClient(_playerState, _playerGameState, _playerData, _connectionId);
            await _context.SendAvailableMethodsToClient(GetAvailableMethods(), _connectionId);
        }
        else
        {
            throw new Exception("You have no connection id... how?");
        }
    }

    private async void NotifyClientsOfMatchResults()
    {
        if (_connectionId != null)
        {
            if (_lastMatchResponse != null) await _context.SendMatchResponseToClient(_lastMatchResponse, _connectionId);
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
        NotifyClientsOfStateChange();
    }

    private void ChangePlayerGameState(PlayerGameState playerGameState)
    {
        _playerGameState = playerGameState;
        //TODO notify observer
        NotifyClientsOfStateChange();
    }
}