using System.Net.WebSockets;
using System.Text;
using GameServer.Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace GameServer.Controllers;

[ApiController]
[Route("game")]
public class RockPaperScissorsController : ControllerBase
{
    private readonly ILogger<RockPaperScissorsController> _logger;

    private readonly IClusterClient _client;

    public RockPaperScissorsController(ILogger<RockPaperScissorsController> logger, IClusterClient client)
    {
        _logger = logger;
        _client = client;
    }

    [HttpGet("player/{id}")]
    public async Task<string> Get(string id)
    {
        var player = _client.GetGrain<IPlayer>(id);

        await player.JoinQueue();

        return "joined Queue";
    }

    [HttpGet("/ws/player/{id}")]
    public async Task Socket(string id)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _logger.Log(LogLevel.Information, "WebSocket connection established");
            await Echo(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }

    private async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        _logger.Log(LogLevel.Information, "Message received from Client");

        while (!result.CloseStatus.HasValue)
        {
            var serverMsg = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(buffer));
            await webSocket.SendAsync(new ArraySegment<byte>(serverMsg, 0, serverMsg.Length), result.MessageType,
                result.EndOfMessage, CancellationToken.None);
            _logger.Log(LogLevel.Information, "Message sent to Client {}", Encoding.UTF8.GetString(buffer));

            buffer = new byte[1024 * 4];
            
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            _logger.Log(LogLevel.Information, "Message received from Client");

        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        _logger.Log(LogLevel.Information, "WebSocket connection closed");
    }
}