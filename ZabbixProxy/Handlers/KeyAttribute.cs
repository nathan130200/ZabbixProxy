using System.Globalization;
using System.Text.RegularExpressions;
using Zabbix.Entities;

namespace Zabbix.Handlers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class KeyAttribute : Attribute
{
    public Regex Pattern { get; }

    public KeyAttribute(string raw, bool isFromRegex)
    {
        if (!isFromRegex)
            raw = Regex.Escape(raw);

        Pattern = new Regex(raw, RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);
    }
}

public abstract class BaseHandler
{
    public abstract Task InvokeAsync(HandlerContext ctx);
}

public class HandlerContext
{
    public Command Command { get; init; }
    public Stream Stream { get; init; }

    public async Task RespondAsync(object value)
    {
        var text = string.Empty;

        if (value is bool b)
            text = b ? "true" : "false";
        else
        {
            if (value is IFormattable fmt)
            {
                if (value is float || value is double)
                    text = fmt.ToString("F6", CultureInfo.InvariantCulture);
                else
                    text = fmt.ToString(string.Empty, CultureInfo.InvariantCulture);
            }
        }

        var packet = new ZabbixPacket
        {
            Type = ZabbixPacketType.Communication,
            Data = text.GetBytes()
        };

        await packet.WriteAsync(Stream);
    }
}