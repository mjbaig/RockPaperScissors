using Orleans;

namespace GameServer.Grains;

public interface IGame : IGrainWithGuidKey
{
    public Task SubmitMove(string playerKey, RockPaperScissorsMove move);

    public Task RegisterPlayer(IPlayer player);
}

public class Game : Grain, IGame
{
    private readonly Dictionary<string, IPlayer> _playerKeyMap;

    private int _playerCount;

    private Dictionary<string, RockPaperScissorsMove> _playerMoveMap;

    private readonly ILogger<Game> _logger;

    private int _playerOneWins;

    private int _playerTwoWins;

    private GameState _gameState;

    public Game(ILogger<Game> logger)
    {
        _logger = logger;

        _playerCount = 0;

        _playerKeyMap = new Dictionary<string, IPlayer>();

        _playerMoveMap = new Dictionary<string, RockPaperScissorsMove>();

        _playerOneWins = 0;

        _playerTwoWins = 0;

        _gameState = GameState.ONGOING;
    }

    public Task SubmitMove(string playerKey, RockPaperScissorsMove move)
    {
        if (_gameState == GameState.ENDED)
        {
            throw new Exception("this game has ended");
        }
        
        if (!_playerKeyMap.ContainsKey(playerKey))
        {
            throw new Exception("You never registered to play moron");
        }

        if (_playerMoveMap.ContainsKey(playerKey))
        {
            throw new Exception("You already submitted a move idiot");
        }

        _playerMoveMap[playerKey] = move;

        if (_playerMoveMap.Count == 2)
        {
            //TODO call players
            _playerMoveMap = new Dictionary<string, RockPaperScissorsMove>();
        }

        return Task.CompletedTask;
    }

    private MatchResponse ExecuteTurn()
    {
        var playerKeys = _playerMoveMap.Keys.ToList();

        var playerOneId = playerKeys[0];
        var playerTwoId = playerKeys[1];

        var playerOneMove = _playerMoveMap[playerOneId];
        var playerTwoMove = _playerMoveMap[playerTwoId];

        MatchResult playerOneMatchResult;

        MatchResult playerTwoMatchResult;

        if (playerOneMove == RockPaperScissorsMove.Paper)
        {
            if (playerTwoMove == RockPaperScissorsMove.Rock)
            {
                playerOneMatchResult = MatchResult.WIN;
                playerTwoMatchResult = MatchResult.LOSE;
            }
            else if (playerTwoMove == RockPaperScissorsMove.Scissors)
            {
                playerOneMatchResult = MatchResult.LOSE;
                playerTwoMatchResult = MatchResult.WIN;
            }
            else
            {
                playerOneMatchResult = MatchResult.TIE;
                playerTwoMatchResult = MatchResult.TIE;
            }
        }
        else if (playerOneMove == RockPaperScissorsMove.Rock)
        {
            if (playerTwoMove == RockPaperScissorsMove.Paper)
            {
                playerOneMatchResult = MatchResult.LOSE;
                playerTwoMatchResult = MatchResult.WIN;
            }
            else if (playerTwoMove == RockPaperScissorsMove.Scissors)
            {
                playerOneMatchResult = MatchResult.WIN;
                playerTwoMatchResult = MatchResult.LOSE;
            }
            else
            {
                playerOneMatchResult = MatchResult.TIE;
                playerTwoMatchResult = MatchResult.TIE;
            }
        }
        else
        {
            if (playerTwoMove == RockPaperScissorsMove.Paper)
            {
                playerOneMatchResult = MatchResult.WIN;
                playerTwoMatchResult = MatchResult.LOSE;
            }
            else if (playerTwoMove == RockPaperScissorsMove.Rock)
            {
                playerOneMatchResult = MatchResult.LOSE;
                playerTwoMatchResult = MatchResult.WIN;
            }
            else
            {
                playerOneMatchResult = MatchResult.TIE;
                playerTwoMatchResult = MatchResult.TIE;
            }
        }

        if (playerOneMatchResult == MatchResult.WIN)
        {
            _playerOneWins++;
        }

        if (playerTwoMatchResult == MatchResult.WIN)
        {
            _playerTwoWins++;
        }

        if (_playerOneWins == 2 || _playerTwoWins == 2)
        {
            _gameState = GameState.ENDED;
        }

        _playerMoveMap = new Dictionary<string, RockPaperScissorsMove>();

        return new MatchResponse(playerOneMove,
            playerOneMatchResult,
            playerTwoMove,
            playerTwoMatchResult, playerOneId, playerTwoId, _gameState);
    }

    public Task RegisterPlayer(IPlayer player)
    {
        if (_playerCount >= 2)
        {
            throw new Exception("Too many dang players");
        }

        _playerCount++;
        _playerKeyMap[player.GetPrimaryKeyString()] = player;

        if (_playerCount == 2)
        {
            var playerKeys = _playerKeyMap.Keys.ToList();
            var player1 = _playerKeyMap[playerKeys[0]];
            player1.StartMatch(this);
            var player2 = _playerKeyMap[playerKeys[1]];
            player2.StartMatch(this);
        }

        return Task.CompletedTask;
    }
}

public enum GameState
{
    ONGOING,
    ENDED,
}

public enum MatchResult
{
    WIN,
    LOSE,
    TIE,
}

public enum RockPaperScissorsMove
{
    Rock,
    Paper,
    Scissors,
}

public class MatchResponse
{
    public MatchResponse(RockPaperScissorsMove playerOneMove, MatchResult playerOneResult,
        RockPaperScissorsMove playerTwoMove, MatchResult playerTwoResult, string playerOneId, string playerTwoId,
        GameState gameState)
    {
        this.PlayerOneMove = playerOneMove;
        this.PlayerOneResult = playerOneResult;
        this.PlayerTwoMove = playerTwoMove;
        this.PlayerTwoResult = playerTwoResult;
        PlayerOneId = playerOneId;
        PlayerTwoId = playerTwoId;
        GameState = gameState;
    }

    private GameState GameState { get; }

    private string PlayerOneId { get; }
    private RockPaperScissorsMove PlayerOneMove { get; }

    private MatchResult PlayerOneResult { get; }

    private string PlayerTwoId { get; }
    private RockPaperScissorsMove PlayerTwoMove { get; }

    private MatchResult PlayerTwoResult { get; }
}