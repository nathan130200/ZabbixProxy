# Zabbix Proxy
Local proxy between zabbix passive agent and zabbix server to communicate custom commands.<br/>
You can intercept and modify or implement new queries to use in zabbix triggers.

### How it works.
- In zabbix server with passive agent, server attempt to connect to proxy
- Proxy will check if command can be handled by proxy and respond to server if was necessary
- Route command to local zabbix passive agent if proxy cannot handle command.

<br>
NOTE: If local passive agent fails to reply then zabbix proxy sends an `ZBX_ERROR` instead of destroying connection without any reasons.

### Custom Commands
You can manually implement custom commands to handle your request in zabbix server.

```cs
[Key("my.command")]  // attribute for register this handler
public class MyCustomCommand : BaseHandler // base classe for handlers
{
  public override async Task InvokeAsync(HandlerContext ctx)
  {
    // send response to server, this function accept object, that will be serialized as string
    // and converted by zabbix server by some jsfunction on server-side.
    await ctx.RespondAsync("some response to server"); 
  }
}
```
An single class can support multiple key handlers:

```cs
using System;
using System.Threading.Tasks;
using Zabbix;
using Zabbix.Api;

namespace MyZbxProxy.Handlers
{
	[Key("agent.type")]
	[Key("agent.ping")]
	public class AgentHandler : BaseHandler
	{
		public override async Task InvokeAsync(HandlerContext ctx)
		{
			if (ctx.Command.Name.Equals("agent.type"))
				await ctx.RespondAsync(ctx.Agent.Config.Platform); // who platform is? windows or linux
			else if (ctx.Command.Name.Equals("agent.ping")) // time taken to parse packet.
			{
				var response = await ctx.Agent.RouteAsync(ctx.Packet);

				if (response != null)
				{
					var difference = (long)Math.Abs((response.Timestamp - ctx.Packet.Timestamp).TotalMilliseconds);
					await ctx.RespondAsync(difference);
				}
			}
		}
	}
}
```
You may require sometimes, passing arguments to handler and catch them. You can do just like an example:

```cs
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Zabbix.Api;

namespace MyZbxProxy.Handlers
{
	[Key("proxy.ping")]
	public class PingHandler : BaseHandler
	{
		public override async Task InvokeAsync(HandlerContext ctx)
		{
			// ctx.Command.Arguments receives all arguments of type CommandArgument,
			// also you have direct access to parsing as int, bool, float, etc.
			// just grab argument position.
			
			// in this example we will peform ping test for one or multiple hostnames in sequence then sum all values and get avg ping for all hosts.
			var numHosts = ctx.Command.Arguments.Count(); 

			// in this example, we respond error if server didn't sent any valid hosts for ping test.
			if (numHosts == 0)
				await ctx.RespondErrorAsync("Invalid address or hostname");
			else
			{
				using (var ping = new Ping())
				{
					var latency = 0L;

					foreach (var addr in ctx.Command.Arguments)
					{
						if (!string.IsNullOrEmpty(addr.Value))
						{
							var result = await ping.SendPingAsync(addr.Value);

							if (result.Status == IPStatus.Success)
								latency += result.RoundtripTime;
						}
					}

					// we sent to zabbix server reply as float.
					await ctx.RespondAsync(((float)latency / (float)numHosts));
				}
			}
		}
	}
}```
