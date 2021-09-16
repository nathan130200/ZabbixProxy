using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using Zabbix.Entities;
using Zabbix.Handlers;

namespace Zabbix;

public class PassiveAgent
{
    private ConcurrentDictionary<Regex, BaseHandler> handlers;
    private CancellationTokenSource cts;
    private IPEndPoint endpoint, proxyEndpoint;
    private Socket socket;

    public PassiveAgent() : this(new Uri("tcp://0.0.0.0:10052"), new Uri("tcp://127.0.0.1:10050"))
    {

    }

    public PassiveAgent(Uri listenUri, Uri connectUri)
    {
        endpoint = new IPEndPoint(IPAddress.Parse(listenUri.Host), listenUri.Port);
        proxyEndpoint = new IPEndPoint(IPAddress.Parse(connectUri.Host), connectUri.Port);
        handlers = new ConcurrentDictionary<Regex, BaseHandler>();
        RegisterHandlers(typeof(PassiveAgent).Assembly);
    }

    public void RegisterHandlers(Assembly assembly)
    {
        var types = assembly.GetTypes().Where(xt => xt.IsSubclassOf(typeof(BaseHandler)));

        foreach (var type in types)
        {
            var attrs = type.GetCustomAttributes<KeyAttribute>();

            if (attrs?.Any() == false)
                continue;

            ConstructorInfo ctor = null;
            bool is_server_ctor = false;

            foreach (var temp in type.GetTypeInfo().DeclaredConstructors)
            {
                if (temp.GetParameters().Length == 1 && temp.GetParameters()[0].Equals(GetType()))
                {
                    is_server_ctor = true;
                    ctor = temp;
                    break;
                }
                else if (temp.GetParameters().Length == 0)
                {
                    ctor = temp;
                    break;
                }
            }

            BaseHandler ptr = null;

            if (ctor == null)
                continue;
            else
            {
                var args = new object[is_server_ctor ? 1 : 0];

                if (is_server_ctor)
                    args[0] = this;

                ptr = (BaseHandler)ctor.Invoke(args);

                if (ptr != null)
                {
                    foreach (var attr in attrs.Select(x => x.Pattern))
                        handlers[attr] = ptr;
                }
            }
        }
    }

    public async Task StartAsync()
    {
        await Task.Yield();

        if (cts != null && !cts.IsCancellationRequested)
            return;

        cts = new CancellationTokenSource();
        socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(endpoint);
        socket.Listen();

        _ = Task.Run(async () => await AcceptAsync(), cts.Token);
    }

    public async Task StopAsync()
    {
        await Task.Yield();

        if (cts == null)
            return;

        if (cts.IsCancellationRequested)
            return;

        cts.Cancel();

        try
        {
            socket.Dispose();
            socket = null;
        }
        catch { }
    }

    async Task AcceptAsync()
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var sock = await socket.AcceptAsync();
                _ = Task.Run(async () => await HandleAsync(sock));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            await Task.Delay(1);
        }

        async Task HandleAsync(Socket socket)
        {
            using (socket)
            {
                using var stream = new NetworkStream(socket, true);
                var result = await ZabbixPacket.ParseAsync(stream);

                if (result != null)
                {
                    if (Command.TryParse(result.Data.GetString(), out var cmd))
                    {
                        var match = handlers.FirstOrDefault(x => x.Key.IsMatch(cmd.Name)).Value;

                        if (match != null)
                        {
                            await match.InvokeAsync(new()
                            {
                                Stream = stream,
                                Command = cmd
                            });
                        }
                        else
                        {
                            try
                            {
                                var response = await MakeProxyRequestAsync(proxyEndpoint, result);

                                if (response != null)
                                    await response.WriteAsync(stream);
                                else
                                    await DispatchErrorAsync(stream, "Feature not supported");
                            }
                            catch (Exception ex)
                            {
                                await DispatchErrorAsync(stream, $"Cannot retrieve packet from proxy!\n{ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        await DispatchErrorAsync(stream, "Bad request");
                    }
                }
                else
                {
                    await DispatchErrorAsync(stream, "Invalid packet");
                }
            }
        }
    }

    static async Task DispatchErrorAsync(Stream stream, string msg)
    {
        await new ZabbixPacket
        {
            Type = ZabbixPacketType.Communication,
            Data = $"ZBX_NOTSUPPORTED\0{msg}".GetBytes()
        }.WriteAsync(stream);
    }

    static Task<ZabbixPacket> MakeProxyRequestAsync(IPEndPoint endpoint, ZabbixPacket packet, TimeSpan? timeout = default)
    {
        var tsc = new TaskCompletionSource<ZabbixPacket>();
        var cts = new CancellationTokenSource(timeout.GetValueOrDefault(TimeSpan.FromSeconds(10)));
        cts.Token.Register(() => tsc.TrySetResult(null));

        _ = Task.Run(async () =>
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(endpoint, cts.Token);

                using var stream = client.GetStream();
                await packet.WriteAsync(stream);
                await stream.FlushAsync();

                packet = new ZabbixPacket();

                if (await packet.ReadAsync(stream))
                    tsc.TrySetResult(packet);
            }
            catch (Exception ex)
            {
                tsc.TrySetException(ex);
            }
        });

        return tsc.Task;
    }
}