# Zabbix Proxy
Local proxy between zabbix passive agent and zabbix server to communicate custom commands.<br/>
You can intercept and modify or implement new queries to use in zabbix triggers.

### How it works.
- In zabbix server with passive agent, server attempt to connect to proxy
- Proxy will check if command can be handled by proxy and respond to server if was necessary
- Route command to local zabbix passive agent if proxy cannot handle command.

<br>
NOTE: If server fails to respond, zabbix proxy sends an request instead of destroying connection.

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
