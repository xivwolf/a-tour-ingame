using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TourAlert.Models;

namespace TourAlert.Services;

public class NotificationService : IDisposable
{
    private ClientWebSocket _webSocket = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Uri? _serverUri;
    private bool _disposed;

    public event Action<DiscordMessage>? MessageReceived;
    public WebSocketState ConnectionState => _webSocket.State;

    public async Task ConnectAsync(Uri uri)
    {
        _serverUri = uri;
        _cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(() => ConnectionLoop(_cancellationTokenSource.Token));
    }

    private async Task ConnectionLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && !_disposed)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                try
                {
                    // ClientWebSocket objects cannot be reused once they are aborted or closed.
                    if (_webSocket.State != WebSocketState.None)
                    {
                        _webSocket.Dispose();
                        _webSocket = new ClientWebSocket();
                    }

                    await _webSocket.ConnectAsync(_serverUri!, token);
                    _ = ReceiveLoop(token);
                }
                catch (Exception)
                {
                    // Wait 5 seconds before retrying
                    await Task.Delay(5000, token);
                    continue;
                }
            }

            // Check connection health every 5 seconds
            await Task.Delay(5000, token);
        }
    }

    private async Task ReceiveLoop(CancellationToken token)
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);
        try
        {
            while (_webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(buffer, token);
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
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                }
            }
        }
        catch (Exception)
        {
            // Error occurred, loop will exit and ConnectionLoop will handle reconnection
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _cancellationTokenSource?.Cancel();
        _webSocket.Dispose();
    }
}
