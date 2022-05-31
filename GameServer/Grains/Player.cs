using GameServer.Exceptions;
using GameServer.Hubs;
using Microsoft.AspNetCore.SignalR;
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

    private readonly IRockPaperScissorsClientContext _context;

    public Player(ILogger<Player> logger, IRockPaperScissorsClientContext context)
    {
        number = 0;
        _logger = logger;
        _playerState = PlayerState.InMenu;
        _wins = 0;
        _losses = 0;
        _context = context;
    }

    private MatchResponse? _lastMatchResponse;

    private List<AvailableMethods> GetAvailableMethods()
    {
        var availableMethods = (_playerState, _playerGameState) switch
        {
            (PlayerState.InGame, PlayerGameState.Ready) => new List<AvailableMethods>()
            {
                AvailableMethods.SendMove,
            },

            (PlayerState.InGame, PlayerGameState.Waiting) => new List<AvailableMethods>()
            {
            },

            (PlayerState.InMenu, _) => new List<AvailableMethods>()
            {
                AvailableMethods.JoinQueue,
            },

            (PlayerState.InQueue, _) => new List<AvailableMethods>()
            {
            },

            (_, _) => new List<AvailableMethods>() { },
        };

        return availableMethods;
    }

    public Task Subscribe(string connectionId)
    {
        _connectionId = connectionId;
        NotifyClientsOfStateChange();
        return Task.CompletedTask;
    }

    private async void NotifyClientsOfStateChange()
    {
        if (_connectionId != null)
        {
            await _context.SendStateToClient(_playerState, _playerGameState, _connectionId);
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
            if (_lastMatchResponse != null)
            {
                await _context.SendMatchResponseToClient(_lastMatchResponse, _connectionId);
            }
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
        _lastMatchResponse = matchResponse;

        if (matchResponse.GameState == GameState.Ended)
        {
            ChangePlayerState(PlayerState.InMenu);
            _game = null;
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
        return Task.FromResult<PlayerState>(_playerState);
    }

    public Task<PlayerGameState> GetGameState()
    {
        return Task.FromResult(_playerGameState);
    }
}