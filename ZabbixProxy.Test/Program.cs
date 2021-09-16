using Zabbix;

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    if (!cts.IsCancellationRequested)
        cts.Cancel();

    e.Cancel = true;
};

var agent = new PassiveAgent(
    listenUri: new Uri("tcp://0.0.0.0:10052"),
    connectUri: new Uri("tcp://127.0.0.1:10050")
);

Util.OnPacketReceived += buff =>
{
    Console.WriteLine("recv <<\n{0}\n", buff.GetString());
};

Util.OnPacketSent += buff =>
{
    Console.WriteLine("send >>\n{0}\n", buff.GetString());
};

await agent.StartAsync();

while (!cts.IsCancellationRequested)
    await Task.Delay(1);

await agent.StopAsync();