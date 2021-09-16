using System.Globalization;
using Zabbix.Entities;

namespace Zabbix.Handlers;

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