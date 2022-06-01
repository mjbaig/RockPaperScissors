using Orleans;

namespace GameServer.Grains;

public interface IGame : IGrainWithGuidKey
{
    public Task SubmitMove(string playerKey, RockPaperScissorsMove move);

    public Task RegisterPlayer(IPlayer player);

    public Task<GameState> GetGameState();
}

public class Game : Grain, IGame
{
    private readonly ILogger<Game> _logger;
    private readonly Dictionary<string, IPlayer> _playerKeyMap;

    private GameState _gameState;

    private int _playerCount;

    private Dictionary<string, RockPaperScissorsMove> _playerMoveMap;

    private string? _playerOneId;

    private int _playerOneWins;

    private string? _playerTwoId;

    private int _playerTwoWins;

    public Game(ILogger<Game> logger)
    {
        _logger = logger;

        _playerCount = 0;

        _playerKeyMap = new Dictionary<string, IPlayer>();

        _playerMoveMap = new Dictionary<string, RockPaperScissorsMove>();

        _playerOneWins = 0;

        _playerTwoWins = 0;

        _gameState = GameState.WaitingForPlayers;
    }

    public Task SubmitMove(string playerKey, RockPaperScissorsMove move)
    {
        if (_gameState == GameState.Ended) throw new Exception("this game has ended");

        if (!_playerKeyMap.ContainsKey(playerKey)) throw new Exception("You never registered to play moron");

        if (_playerMoveMap.ContainsKey(playerKey)) throw new Exception("You already submitted a move idiot");

        _playerMoveMap[playerKey] = move;

        if (_playerMoveMap.Count == 2)
        {
            //TODO call players
            var matchResponse = ExecuteTurn();

            _playerKeyMap[_playerOneId].SendResultFromGameServer(matchResponse[_playerOneId]);
            _playerKeyMap[_playerTwoId].SendResultFromGameServer(matchResponse[_playerTwoId]);
        }

        return Task.CompletedTask;
    }

    public Task RegisterPlayer(IPlayer player)
    {
        if (_playerCount >= 2) throw new Exception("Too many dang players");

        _playerCount++;
        _playerKeyMap[player.GetPrimaryKeyString()] = player;

        if (_playerCount == 2)
        {
            var playerKeys = _playerKeyMap.Keys.ToList();

            _playerOneId = playerKeys[0];
            var player1 = _playerKeyMap[_playerOneId];
            player1.StartMatchFromGameServer(this);

            _playerTwoId = playerKeys[1];
            var player2 = _playerKeyMap[_playerTwoId];
            player2.StartMatchFromGameServer(this);

            _gameState = GameState.Ongoing;
        }

        return Task.CompletedTask;
    }

    public Task<GameState> GetGameState()
    {
        return Task.FromResult(_gameState);
    }

    private Dictionary<string, MatchResponse> ExecuteTurn()
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
                playerOneMatchResult = MatchResult.Win;
                playerTwoMatchResult = MatchResult.Lose;
            }
            else if (playerTwoMove == RockPaperScissorsMove.Scissors)
            {
                playerOneMatchResult = MatchResult.Lose;
                playerTwoMatchResult = MatchResult.Win;
            }
            else
            {
                playerOneMatchResult = MatchResult.Tie;
                playerTwoMatchResult = MatchResult.Tie;
            }
        }
        else if (playerOneMove == RockPaperScissorsMove.Rock)
        {
            if (playerTwoMove == RockPaperScissorsMove.Paper)
            {
                playerOneMatchResult = MatchResult.Lose;
                playerTwoMatchResult = MatchResult.Win;
            }
            else if (playerTwoMove == RockPaperScissorsMove.Scissors)
            {
                playerOneMatchResult = MatchResult.Win;
                playerTwoMatchResult = MatchResult.Lose;
            }
            else
            {
                playerOneMatchResult = MatchResult.Tie;
                playerTwoMatchResult = MatchResult.Tie;
            }
        }
        else
        {
            if (playerTwoMove == RockPaperScissorsMove.Paper)
            {
                playerOneMatchResult = MatchResult.Win;
                playerTwoMatchResult = MatchResult.Lose;
            }
            else if (playerTwoMove == RockPaperScissorsMove.Rock)
            {
                playerOneMatchResult = MatchResult.Lose;
                playerTwoMatchResult = MatchResult.Win;
            }
            else
            {
                playerOneMatchResult = MatchResult.Tie;
                playerTwoMatchResult = MatchResult.Tie;
            }
        }

        if (playerOneMatchResult == MatchResult.Win) _playerOneWins++;

        if (playerTwoMatchResult == MatchResult.Win) _playerTwoWins++;

        bool isPlayerOneWinner = false;
        bool isPlayerTwoWinner = false;

        if (_playerOneWins == 2 || _playerTwoWins == 2)
        {
            _gameState = GameState.Ended;
            isPlayerOneWinner = _playerOneWins == 2;
            isPlayerTwoWinner = _playerTwoWins == 2;
        }

        _playerMoveMap = new Dictionary<string, RockPaperScissorsMove>();

        int round = _playerOneWins + _playerTwoWins;
        
        var matchResponseDictionary = new Dictionary<string, MatchResponse>
        {
            [playerOneId] = new(playerOneMove, playerOneMatchResult, _playerOneWins, _gameState, isPlayerOneWinner,
                playerTwoMove, round),
            [playerTwoId] = new(playerTwoMove, playerTwoMatchResult, _playerTwoWins, _gameState, isPlayerTwoWinner,
                playerOneMove, round)
        };

        return matchResponseDictionary;
    }
}

public enum GameState
{
    WaitingForPlayers,
    Ongoing,
    Ended
}

public enum MatchResult
{
    Win,
    Lose,
    Tie
}

public enum RockPaperScissorsMove
{
    Rock,
    Paper,
    Scissors
}

public class MatchResponse
{
    public MatchResponse(RockPaperScissorsMove playerMove,
        MatchResult playerResult,
        int playerWins,
        GameState gameState, bool isMatchWon, RockPaperScissorsMove opponentMove, int round)
    {
        PlayerMove = playerMove.ToString();
        PlayerResult = playerResult.ToString();
        GameState = gameState;
        IsMatchWon = isMatchWon;
        Round = round;
        OpponentMove = opponentMove.ToString();
        PlayerWins = playerWins;
    }

    public int PlayerWins { get; }
    public GameState GameState { get; }

    public string PlayerMove { get; }

    public string OpponentMove { get; }

    public string PlayerResult { get; }

    public bool IsMatchWon { get; }

    public int Round { get; }
}