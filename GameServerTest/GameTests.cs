using System;
using System.Threading.Tasks;
using GameServer.Grains;
using NUnit.Framework;
using Orleans.TestingHost;

namespace GameServerTest;

public class GameTest
{
    
    [Test]
    public async Task GameStartsInWaitingState()
    {
        var builder = new TestClusterBuilder();
        TestCluster cluster = builder.Build();
        cluster.Deploy();
        
        var game = cluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var currentGameState = await game.GetGameState();

        Assert.AreEqual(GameState.WaitingForPlayers, currentGameState);
    }

    [Test]
    public async Task GameIsWaitAfterOnlyOnePlayerJoins()
    {
        var builder = new TestClusterBuilder();
        TestCluster cluster = builder.Build();
        cluster.Deploy();

        var game = cluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = cluster.GrainFactory.GetGrain<IPlayer>("player1");

        await game.RegisterPlayer(player1);
        
        var currentGameState = await game.GetGameState();

        Assert.AreEqual(GameState.WaitingForPlayers, currentGameState);
    }
    
    [Test]
    public async Task GameIsOngoingAfterPlayersJoinState()
    {
        var builder = new TestClusterBuilder();
        TestCluster cluster = builder.Build();
        cluster.Deploy();

        var game = cluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = cluster.GrainFactory.GetGrain<IPlayer>("player1");
        var player2 = cluster.GrainFactory.GetGrain<IPlayer>("player2");

        await game.RegisterPlayer(player1);
        await game.RegisterPlayer(player2);
        
        var currentGameState = await game.GetGameState();

        Assert.AreEqual(GameState.Ongoing, currentGameState);
    }
    
    [Test]
    public async Task TestPlayersGetResultsAfterSubmittingMoves()
    {
        var builder = new TestClusterBuilder();
        TestCluster cluster = builder.Build();
        cluster.Deploy();

        var game = cluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = cluster.GrainFactory.GetGrain<IPlayer>("player1");
        var player2 = cluster.GrainFactory.GetGrain<IPlayer>("player2");

        await game.RegisterPlayer(player1);
        await game.RegisterPlayer(player2);

        var player1State = await player1.GetState();

        var player2State = await player2.GetState();

        while (player1State != PlayerState.InGame)
        {
            player1State = await player1.GetState();
        }
        
        while (player2State != PlayerState.InGame)
        {
            player2State = await player2.GetState();
        }

        await player1.SendMove(RockPaperScissorsMove.Paper);

        await player2.SendMove(RockPaperScissorsMove.Scissors);

        var player1GameState = await player1.GetGameState();
        
        var player2GameState = await player2.GetGameState();

        while (player1GameState == PlayerGameState.Waiting)
        {
            player1GameState = await player1.GetGameState();
        }
        
        while (player2GameState == PlayerGameState.Waiting)
        {
            player2GameState = await player2.GetGameState();
        }

        var playerOneMatchResults = await player1.GetLastMatchResponse();

        var playerTwoMatchResults = await player2.GetLastMatchResponse();

        Assert.AreEqual(1, playerTwoMatchResults.PlayerWins);
        
        Assert.AreEqual(0, playerOneMatchResults.PlayerWins);
        
        cluster.StopAllSilos();
    }
    
    [Test]
    public async Task TestPlayersGetNoPointsForTie()
    {
        var builder = new TestClusterBuilder();
        TestCluster cluster = builder.Build();
        cluster.Deploy();

        var game = cluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = cluster.GrainFactory.GetGrain<IPlayer>("player1");
        var player2 = cluster.GrainFactory.GetGrain<IPlayer>("player2");

        await game.RegisterPlayer(player1);
        await game.RegisterPlayer(player2);

        var player1State = await player1.GetState();

        var player2State = await player2.GetState();

        while (player1State != PlayerState.InGame)
        {
            player1State = await player1.GetState();
        }
        
        while (player2State != PlayerState.InGame)
        {
            player2State = await player2.GetState();
        }

        await player1.SendMove(RockPaperScissorsMove.Scissors);

        await player2.SendMove(RockPaperScissorsMove.Scissors);

        var player1GameState = await player1.GetGameState();
        
        var player2GameState = await player2.GetGameState();

        while (player1GameState == PlayerGameState.Waiting)
        {
            player1GameState = await player1.GetGameState();
        }
        
        while (player2GameState == PlayerGameState.Waiting)
        {
            player2GameState = await player2.GetGameState();
        }

        var playerOneMatchResults = await player1.GetLastMatchResponse();

        var playerTwoMatchResults = await player2.GetLastMatchResponse();

        Assert.AreEqual(0, playerTwoMatchResults.PlayerWins);
        
        Assert.AreEqual(0, playerOneMatchResults.PlayerWins);
        
        cluster.StopAllSilos();
    }
    
}