using System;
using System.Threading.Tasks;
using GameServer.Grains;
using NUnit.Framework;
using Orleans.TestingHost;

namespace GameServerTest;

public class PlayerTests
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
    public async Task PlayerEntersQueueCorrectly()
    {
        var player = _testCluster.GrainFactory.GetGrain<IPlayer>("player");
        await player.Subscribe("1");
        await player.JoinMatchMakerQueue();

        var playerState = await player.GetState();

        Assert.AreEqual(PlayerState.InQueue, playerState);
    }

    [Test]
    public async Task PlayerEntersGameIfTwoAreInQueue()
    {
        var playerOne = _testCluster.GrainFactory.GetGrain<IPlayer>("player1");
        await playerOne.Subscribe("1");
        await playerOne.JoinMatchMakerQueue();

        var playerTwo = _testCluster.GrainFactory.GetGrain<IPlayer>("player2");
        await playerTwo.Subscribe("2");
        await playerTwo.JoinMatchMakerQueue();

        var game = _testCluster.GrainFactory.GetGrain<IGame>(Guid.NewGuid());

        await game.RegisterPlayer(playerOne);

        await game.RegisterPlayer(playerTwo);

        var playerOneState = await playerOne.GetState();

        Assert.AreEqual(PlayerState.InGame, playerOneState);
    }

    [TearDown]
    public void TearDown()
    {
        _testCluster.StopAllSilos();
    }
}