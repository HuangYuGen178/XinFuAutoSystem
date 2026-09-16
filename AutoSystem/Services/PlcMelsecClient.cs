using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication.Profinet.Melsec;
namespace AutoSystem.Services
{
    // 基于 HslCommunication 的 Mitsubishi MC (MelsecMcNet) 实现
    public class PlcMelsecClient : IPlcClient, IDisposable
    {
        private readonly MelsecMcNet client;

        //public PlcMelsecClient(string ip, int port)
        //{
        //    client = new MelsecMcNet(ip, port);
        //    client.ConnectServer();
        //}
        public PlcMelsecClient(string ip, int port)
        {
            client = new MelsecMcNet(ip, port);
            // 不调用 client.ConnectServer() 以避免同步阻塞
        }
        // 提供异步连接方法，带超时控制
        public async Task ConnectAsync(int timeoutMs = 5000)
        {
            if (client == null) throw new InvalidOperationException("PLC 客户端未初始化。");

            var connectTask = Task.Run(() =>
            {
                try
                {
                    client.ConnectServer();
                }
                catch (Exception ex)
                {
                    // 将异常抛出以便上层处理/记录
                    throw new Exception("连接 PLC 失败: " + ex.Message, ex);
                }
            });

            var delay = Task.Delay(timeoutMs);
            var finished = await Task.WhenAny(connectTask, delay).ConfigureAwait(false);
            if (finished == delay)
            {
                throw new TimeoutException("连接 PLC 超时 (" + timeoutMs + " ms)。");
            }
            // await connectTask to observe potential exceptions
            await connectTask.ConfigureAwait(false);
        }
        public async Task<int> ReadintAsync(string address)
        {
            return await Task.Run(() =>
            {
                var addr = address;
                var r = client.ReadInt16(addr);
                if (!r.IsSuccess) throw new Exception("读取 PLC D 寄存器失败: " + r.Message);
                return (int)r.Content;
            });
        }

        public async Task WriteintAsync(string address, int value)
        {
            await Task.Run(() =>
            {
                var addr =  address;
                short v = (short)value;
                var r = client.Write(addr, v);
                if (!r.IsSuccess) throw new Exception("写入 PLC D 寄存器失败: " + r.Message);
            });
        }

        public async Task<string> ReadStringAsync(string address, ushort length)
        {
            return await Task.Run(() =>
            {
                var r = client.ReadString(address, length);
                if (!r.IsSuccess) throw new Exception("读取 PLC 字符串失败: " + r.Message);
                var content = r.Content ?? string.Empty;
                content = content.TrimEnd('\0').Trim();
                return content;
            });
        }

        public async Task WriteStringAsync(string address, string value)
        {
            await Task.Run(() =>
            {
                var r = client.Write(address, value);
                if (!r.IsSuccess) throw new Exception("写入 PLC 字符串失败: " + r.Message);
            });
        }

        public async Task<bool> IsConnectedAsync()
        {
            return await Task.Run(() =>
            {
                if (client == null) return false;

                try
                {
                    var t = client.GetType();
                    var candidates = new[] { "Connected", "IsConnected", "IsSocketConnected", "IsOpen" };
                    foreach (var name in candidates)
                    {
                        var prop = t.GetProperty(name);
                        if (prop != null && prop.PropertyType == typeof(bool))
                        {
                            try
                            {
                                var val = prop.GetValue(client, null);
                                if (val is bool) return (bool)val;
                            }
                            catch { /* 忽略，尝试下一个属性 */ }
                        }
                    }
                }
                catch { /* 反射失败则继续后续策略 */ }

                // 不能确定具体属性时，若 client 对象已创建，假定连接成功（保守策略）
                return true;
            });
        }

        public void Dispose()
        {
            try { client?.ConnectClose(); } catch { }
        }
    }
}



//namespace AutoSystem.Services
//{
//    // 基于 HslCommunication 的 Mitsubishi MC (MelsecMcNet) 实现
//    public class PlcMelsecClient : IPlcClient, IDisposable
//    {
//        private MelsecMcNet client;
//        private readonly string ip;
//        private readonly int port;
//        private readonly object syncLock = new object();
//        private bool disposed = false;

//        public PlcMelsecClient(string ip, int port)
//        {
//            this.ip = ip;
//            this.port = port;
//            client = new MelsecMcNet(ip, port);
//            // 不在构造中同步连接，使用 ConnectAsync 或自动重连策略
//        }

//        // 提供异步连接方法，带超时控制
//        public async Task ConnectAsync(int timeoutMs = 5000)
//        {
//            ThrowIfDisposed();

//            var connectTask = Task.Run(() =>
//            {
//                lock (syncLock)
//                {
//                    try
//                    {
//                        if (IsClientConnectedInternal()) return;
//                        client?.ConnectClose();
//                        client = new MelsecMcNet(ip, port);
//                        client.ConnectServer();
//                    }
//                    catch (Exception ex)
//                    {
//                        throw new Exception("连接 PLC 失败: " + ex.Message, ex);
//                    }
//                }
//            });

//            var delay = Task.Delay(timeoutMs);
//            var finished = await Task.WhenAny(connectTask, delay).ConfigureAwait(false);
//            if (finished == delay)
//            {
//                throw new TimeoutException("连接 PLC 超时 (" + timeoutMs + " ms)。");
//            }
//            // propagate exceptions if any
//            await connectTask.ConfigureAwait(false);
//        }

//        public async Task<int> ReadintAsync(string address)
//        {
//            ThrowIfDisposed();

//            return await Task.Run(async () =>
//            {
//                const int maxRetries = 2;
//                var addr = address;
//                Exception lastEx = null;
//                for (int attempt = 0; attempt <= maxRetries; attempt++)
//                {
//                    try
//                    {
//                        var r = client.ReadInt16(addr);
//                        if (r.IsSuccess) return (int)r.Content;

//                        lastEx = new Exception("读取 PLC D 寄存器失败: " + r.Message);
//                    }
//                    catch (Exception ex)
//                    {
//                        lastEx = ex;
//                    }

//                    // 如果不是最后一次，尝试重连再重试
//                    if (attempt < maxRetries)
//                    {
//                        try
//                        {
//                            await ReconnectAsync().ConfigureAwait(false);
//                        }
//                        catch
//                        {
//                            // 忽略，下次循环会抛出最终异常
//                        }
//                        await Task.Delay(200).ConfigureAwait(false);
//                    }
//                }

//                throw lastEx ?? new Exception("读取 PLC D 寄存器失败（未知错误）。");
//            }).ConfigureAwait(false);
//        }

//        public async Task WriteintAsync(string address, int value)
//        {
//            ThrowIfDisposed();

//            await Task.Run(async () =>
//            {
//                const int maxRetries = 2;
//                var addr = address;
//                Exception lastEx = null;
//                for (int attempt = 0; attempt <= maxRetries; attempt++)
//                {
//                    try
//                    {
//                        short v = (short)value;
//                        var r = client.Write(addr, v);
//                        if (r.IsSuccess) return;
//                        lastEx = new Exception("写入 PLC D 寄存器失败: " + r.Message);
//                    }
//                    catch (Exception ex)
//                    {
//                        lastEx = ex;
//                    }

//                    if (attempt < maxRetries)
//                    {
//                        try
//                        {
//                            await ReconnectAsync().ConfigureAwait(false);
//                        }
//                        catch { }
//                        await Task.Delay(200).ConfigureAwait(false);
//                    }
//                }

//                throw lastEx ?? new Exception("写入 PLC D 寄存器失败（未知错误）。");
//            }).ConfigureAwait(false);
//        }

//        public async Task<string> ReadStringAsync(string address, ushort length)
//        {
//            ThrowIfDisposed();

//            return await Task.Run(async () =>
//            {
//                const int maxRetries = 2;
//                Exception lastEx = null;
//                for (int attempt = 0; attempt <= maxRetries; attempt++)
//                {
//                    try
//                    {
//                        var r = client.ReadString(address, length);
//                        if (r.IsSuccess)
//                        {
//                            var content = r.Content ?? string.Empty;
//                            content = content.TrimEnd('\0').Trim();
//                            return content;
//                        }

//                        lastEx = new Exception("读取 PLC 字符串失败: " + r.Message);
//                    }
//                    catch (Exception ex)
//                    {
//                        lastEx = ex;
//                    }

//                    if (attempt < maxRetries)
//                    {
//                        try
//                        {
//                            await ReconnectAsync().ConfigureAwait(false);
//                        }
//                        catch { }
//                        await Task.Delay(200).ConfigureAwait(false);
//                    }
//                }

//                throw lastEx ?? new Exception("读取 PLC 字符串失败（未知错误）。");
//            }).ConfigureAwait(false);
//        }

//        public async Task WriteStringAsync(string address, string value)
//        {
//            ThrowIfDisposed();

//            await Task.Run(async () =>
//            {
//                const int maxRetries = 2;
//                Exception lastEx = null;
//                for (int attempt = 0; attempt <= maxRetries; attempt++)
//                {
//                    try
//                    {
//                        var r = client.Write(address, value);
//                        if (r.IsSuccess) return;
//                        lastEx = new Exception("写入 PLC 字符串失败: " + r.Message);
//                    }
//                    catch (Exception ex)
//                    {
//                        lastEx = ex;
//                    }

//                    if (attempt < maxRetries)
//                    {
//                        try
//                        {
//                            await ReconnectAsync().ConfigureAwait(false);
//                        }
//                        catch { }
//                        await Task.Delay(200).ConfigureAwait(false);
//                    }
//                }

//                throw lastEx ?? new Exception("写入 PLC 字符串失败（未知错误）。");
//            }).ConfigureAwait(false);
//        }

//        public async Task<bool> IsConnectedAsync()
//        {
//            ThrowIfDisposed();

//            return await Task.Run(() =>
//            {
//                try
//                {
//                    return IsClientConnectedInternal();
//                }
//                catch
//                {
//                    return false;
//                }
//            }).ConfigureAwait(false);
//        }

//        private bool IsClientConnectedInternal()
//        {
//            if (client == null) return false;
//            try
//            {
//                // HslCommunication 通常有 Connected 属性
//                var prop = client.GetType().GetProperty("Connected");
//                if (prop != null && prop.PropertyType == typeof(bool))
//                {
//                    var val = prop.GetValue(client, null);
//                    if (val is bool b) return b;
//                }
//            }
//            catch
//            {
//                // 反射失败，fallback 到假定 false
//            }
//            return false;
//        }

//        private async Task ReconnectAsync()
//        {
//            ThrowIfDisposed();

//            await Task.Run(() =>
//            {
//                lock (syncLock)
//                {
//                    try
//                    {
//                        // 如果当前已连上，不再重连
//                        if (IsClientConnectedInternal()) return;

//                        try { client?.ConnectClose(); } catch { }
//                        client = new MelsecMcNet(ip, port);
//                        client.ConnectServer();
//                    }
//                    catch (Exception ex)
//                    {
//                        throw new Exception("PLC 重连失败: " + ex.Message, ex);
//                    }
//                }
//            }).ConfigureAwait(false);
//        }

//        private void ThrowIfDisposed()
//        {
//            if (disposed) throw new ObjectDisposedException(nameof(PlcMelsecClient));
//        }

//        public void Dispose()
//        {
//            if (disposed) return;
//            disposed = true;
//            try { client?.ConnectClose(); } catch { }
//            client = null;
//        }
//    }
//}