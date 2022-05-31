using System;
using System.Threading.Tasks;
using GameServer.Grains;
using NUnit.Framework;
using Orleans.TestingHost;

namespace GameServerTest;

public class GameTest
{
    private TestCluster _testCluster;

    [SetUp]
    public void Setup()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<Utils.TestSiloConfigurations>();
        var cluster = builder.Build();
        cluster.Deploy();
        _testCluster = cluster;
    }

    [Test]
    public async Task GameStartsInWaitingState()
    {
        var game = _testCluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var currentGameState = await game.GetGameState();

        Assert.AreEqual(GameState.WaitingForPlayers, currentGameState);
    }

    [Test]
    public async Task GameIsWaitAfterOnlyOnePlayerJoins()
    {
        var game = _testCluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = _testCluster.GrainFactory.GetGrain<IPlayer>("player1");

        await game.RegisterPlayer(player1);

        var currentGameState = await game.GetGameState();

        Assert.AreEqual(GameState.WaitingForPlayers, currentGameState);
    }

    [Test]
    public async Task GameIsOngoingAfterPlayersJoinState()
    {
        var game = _testCluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = _testCluster.GrainFactory.GetGrain<IPlayer>("player1");
        await player1.Subscribe("1");
        var player2 = _testCluster.GrainFactory.GetGrain<IPlayer>("player2");
        await player2.Subscribe("2");

        await game.RegisterPlayer(player1);
        await game.RegisterPlayer(player2);

        var currentGameState = await game.GetGameState();

        Assert.AreEqual(GameState.Ongoing, currentGameState);
    }

    [Test]
    public async Task TestPlayersGetResultsAfterSubmittingMoves()
    {
        var game = _testCluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = _testCluster.GrainFactory.GetGrain<IPlayer>("player1");
        await player1.Subscribe("1");
        var player2 = _testCluster.GrainFactory.GetGrain<IPlayer>("player2");
        await player2.Subscribe("2");

        await player1.Subscribe("1");
        await player2.Subscribe("2");

        await game.RegisterPlayer(player1);
        await game.RegisterPlayer(player2);

        var player1State = await player1.GetState();

        var player2State = await player2.GetState();

        while (player1State != PlayerState.InGame) player1State = await player1.GetState();

        while (player2State != PlayerState.InGame) player2State = await player2.GetState();

        await player1.SendMoveToGameServer(RockPaperScissorsMove.Paper);

        await player2.SendMoveToGameServer(RockPaperScissorsMove.Scissors);

        var player1GameState = await player1.GetGameState();

        var player2GameState = await player2.GetGameState();

        while (player1GameState == PlayerGameState.Waiting) player1GameState = await player1.GetGameState();

        while (player2GameState == PlayerGameState.Waiting) player2GameState = await player2.GetGameState();

        var playerOneMatchResults = await player1.GetLastMatchResponse();

        var playerTwoMatchResults = await player2.GetLastMatchResponse();

        Assert.AreEqual(1, playerTwoMatchResults.PlayerWins);

        Assert.AreEqual(0, playerOneMatchResults.PlayerWins);
    }

    [Test]
    public async Task TestPlayersGetNoPointsForTie()
    {
        var game = _testCluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = _testCluster.GrainFactory.GetGrain<IPlayer>("player1");
        await player1.Subscribe("1");
        var player2 = _testCluster.GrainFactory.GetGrain<IPlayer>("player2");
        await player2.Subscribe("2");

        await player1.Subscribe("1");
        await player2.Subscribe("2");

        await game.RegisterPlayer(player1);
        await game.RegisterPlayer(player2);

        var player1State = await player1.GetState();

        var player2State = await player2.GetState();

        while (player1State != PlayerState.InGame) player1State = await player1.GetState();

        while (player2State != PlayerState.InGame) player2State = await player2.GetState();

        await player1.SendMoveToGameServer(RockPaperScissorsMove.Scissors);

        await player2.SendMoveToGameServer(RockPaperScissorsMove.Scissors);

        var player1GameState = await player1.GetGameState();

        var player2GameState = await player2.GetGameState();

        while (player1GameState == PlayerGameState.Waiting) player1GameState = await player1.GetGameState();

        while (player2GameState == PlayerGameState.Waiting) player2GameState = await player2.GetGameState();

        var playerOneMatchResults = await player1.GetLastMatchResponse();

        var playerTwoMatchResults = await player2.GetLastMatchResponse();

        Assert.AreEqual(0, playerTwoMatchResults.PlayerWins);

        Assert.AreEqual(0, playerOneMatchResults.PlayerWins);
    }

    [TearDown]
    public void TearDown()
    {
        _testCluster.StopAllSilos();
    }
}