using GameServer.Hubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace GameServerTest;

public class Utils
{
    public class TestSiloConfigurations : IHostConfigurator
    {
        public void Configure(IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
            {
                var clientContext = new Mock<IRockPaperScissorsClientContext>();
                services.AddSingleton(clientContext.Object);
            });
        }
    }
}