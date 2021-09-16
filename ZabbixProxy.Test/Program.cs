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
    connectUri: new Uri("tcp://0.0.0.0:10050")
);

await agent.StartAsync();

while (!cts.IsCancellationRequested)
    await Task.Delay(1);

await agent.StopAsync();