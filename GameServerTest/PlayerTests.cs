using System;
using System.Threading.Tasks;
using GameServer.Grains;
using NUnit.Framework;
using Orleans;
using Orleans.TestingHost;

namespace GameServerTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
    
    [Test]
    public async Task PlayerEntersQueueCorrectly()
    {
        var builder = new TestClusterBuilder();
        var cluster = builder.Build();
        cluster.Deploy();

        var player = cluster.GrainFactory.GetGrain<IPlayer>("player");
        await player.JoinQueue();

        var playerState = await player.GetState();
        
        cluster.StopAllSilos();

        Assert.AreEqual(PlayerState.InQueue, playerState);
    }
    
    [Test]
    public async Task PlayerEntersGameIfTwoAreInQueue()
    {
        var builder = new TestClusterBuilder();
        var cluster = builder.Build();
        cluster.Deploy();

        var playerOne = cluster.GrainFactory.GetGrain<IPlayer>("player1");
        await playerOne.JoinQueue();
        
        var playerTwo = cluster.GrainFactory.GetGrain<IPlayer>("player2");
        await playerTwo.JoinQueue();

        var game = cluster.GrainFactory.GetGrain<IGame>(Guid.NewGuid());

        await game.RegisterPlayer(playerOne);
        
        await game.RegisterPlayer(playerTwo);

        var playerOneState = await playerOne.GetState();

        cluster.StopAllSilos();

        Assert.AreEqual(PlayerState.InGame, playerOneState);
    }
}