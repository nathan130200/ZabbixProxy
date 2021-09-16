namespace Zabbix.Handlers;

public abstract class BaseHandler
{
    public abstract Task InvokeAsync(HandlerContext ctx);
}
