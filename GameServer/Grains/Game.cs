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

    public Game(ILogger<Game> logger)
    {
        _logger = logger;

        _playerCount = 0;

        _playerKeyMap = new Dictionary<string, IPlayer>();

        _playerMoveMap = new Dictionary<string, RockPaperScissorsMove>();

    }

    public Task SubmitMove(string playerKey, RockPaperScissorsMove move)
    {
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
        
        throw new NotImplementedException();
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

public enum MatchResult
{
    Win,
    Lose,
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
        RockPaperScissorsMove playerTwoMove, MatchResult playerTwoResult)
    {
        this.PlayerOneMove = playerOneMove;
        this.PlayerOneResult = playerOneResult;
        this.PlayerTwoMove = playerTwoMove;
        this.PlayerTwoResult = playerTwoResult;
    }

    private RockPaperScissorsMove PlayerOneMove { get; }

    private MatchResult PlayerOneResult { get; }

    private RockPaperScissorsMove PlayerTwoMove { get; }

    private MatchResult PlayerTwoResult { get; }
}