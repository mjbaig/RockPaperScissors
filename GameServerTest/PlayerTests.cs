using System;
using System.Threading.Tasks;
using GameServer.Grains;
using NUnit.Framework;
using Orleans;
using Orleans.TestingHost;

namespace GameServerTest;

public class PlayerTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task PlayerEntersQueueCorrectly()
    {
        var builder = new TestClusterBuilder();
        var cluster = builder.Build();
        cluster.Deploy();

        var player = cluster.GrainFactory.GetGrain<IPlayer>("player");
        await player.JoinMatchMakerQueue();

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
        await playerOne.JoinMatchMakerQueue();
        
        var playerTwo = cluster.GrainFactory.GetGrain<IPlayer>("player2");
        await playerTwo.JoinMatchMakerQueue();

        var game = cluster.GrainFactory.GetGrain<IGame>(Guid.NewGuid());

        await game.RegisterPlayer(playerOne);
        
        await game.RegisterPlayer(playerTwo);

        var playerOneState = await playerOne.GetState();

        cluster.StopAllSilos();

        Assert.AreEqual(PlayerState.InGame, playerOneState);
    }
}