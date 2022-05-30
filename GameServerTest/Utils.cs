using GameServer.Hubs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace GameServerTest;

public class Utils
{
    public class TestSiloConfigurations : ISiloBuilderConfigurator {
        public void Configure(ISiloHostBuilder hostBuilder) {
            hostBuilder.ConfigureServices(services =>
            {
                var gameHubMock = new Mock<IGameHub>();
                services.AddSingleton<IGameHub>(gameHubMock.Object);
            });
        }
    }
}