using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SamplePlugin.Models;

namespace SamplePlugin.Services;

public class NotificationService : IDisposable
{
    private readonly ClientWebSocket _webSocket = new();
    private CancellationTokenSource? _cancellationTokenSource;
    public event Action<DiscordMessage>? MessageReceived;
    public WebSocketState ConnectionState => _webSocket.State;

    public async Task ConnectAsync(Uri uri)
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
        _ = Task.Run(ReceiveLoop);
    }

    private async Task ReceiveLoop()
    {
        var buffer = new ArraySegment<byte>(new byte[2048]);
        while (_webSocket.State == WebSocketState.Open)
        {
            if (_cancellationTokenSource?.IsCancellationRequested == true)
            {
                break;
            }
            var result = await _webSocket.ReceiveAsync(buffer, _cancellationTokenSource!.Token);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                if (buffer.Array == null) continue;
                var json = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                var message = JsonSerializer.Deserialize<DiscordMessage>(json);
                if (message != null)
                {
                    MessageReceived?.Invoke(message);
                }
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _webSocket.Dispose();
    }
}
