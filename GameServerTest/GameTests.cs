using System;
using System.Threading.Tasks;
using GameServer.Grains;
using NUnit.Framework;
using Orleans.TestingHost;

namespace GameServerTest;

public class GameTest
{
    private TestCluster? _cluster;
    
    [SetUp]
    public void Setup()
    {
        var builder = new TestClusterBuilder();
        _cluster = builder.Build();
        _cluster.Deploy();
    }
    
    [Test]
    public async Task GameStartsInWaitingState()
    {
        
        var game = _cluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var currentGameState = await game.GetGameState();

        Assert.AreEqual(GameState.WaitingForPlayers, currentGameState);
    }

    [Test]
    public async Task GameIsWaitAfterOnlyOnePlayerJoins()
    {

        var game = _cluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = _cluster.GrainFactory.GetGrain<IPlayer>("player1");

        await game.RegisterPlayer(player1);
        
        var currentGameState = await game.GetGameState();

        Assert.AreEqual(GameState.WaitingForPlayers, currentGameState);
    }
    
    [Test]
    public async Task GameIsOngoingAfterPlayersJoinState()
    {

        var game = _cluster.GrainFactory.GetGrain<IGame>(Guid.Empty);

        var player1 = _cluster.GrainFactory.GetGrain<IPlayer>("player1");
        var player2 = _cluster.GrainFactory.GetGrain<IPlayer>("player2");

        await game.RegisterPlayer(player1);
        await game.RegisterPlayer(player2);
        
        var currentGameState = await game.GetGameState();

        Assert.AreEqual(GameState.Ongoing, currentGameState);
    }

    [TearDown]
    public void CleanUp()
    {
        _cluster.StopAllSilos();
    }
}