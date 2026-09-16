using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSystem.Services
{
    // 通用 TCP 客户端，适用于读码器与视觉相机（设备为 TCP 服务器）
    public class TcpDeviceClient : IDisposable
    {
        readonly string host;
        readonly int port;
        TcpClient client;
        NetworkStream stream;
        CancellationTokenSource cts;

        public event Action<string> OnTextReceived;
        public event Action<Exception> OnError;
        public bool IsConnected => client?.Connected ?? false;

        public TcpDeviceClient(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        public async Task ConnectAsync(int timeoutMs = 5000)
        {
            try
            {
                cts = new CancellationTokenSource();
                client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs)) != connectTask)
                    throw new TimeoutException("连接超时: " + host + ":" + port);

                stream = client.GetStream();
                StartReceiveLoop(cts.Token);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                throw;
            }
        }

        async void StartReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested && client?.Connected == true)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (read == 0) break;
                    var text = Encoding.UTF8.GetString(buffer, 0, read);
                    OnTextReceived?.Invoke(text);
                }
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested) OnError?.Invoke(ex);
            }
            finally
            {
                Dispose();
            }
        }

        public async Task SendAsync(string text)
        {
            if (stream == null) throw new InvalidOperationException("未连接设备");
            var data = Encoding.UTF8.GetBytes(text);
            await stream.WriteAsync(data, 0, data.Length);
        }

        public void Dispose()
        {
            try { cts?.Cancel(); } catch { }
            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }
            stream = null;
            client = null;
        }
    }
}
