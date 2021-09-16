using System.Diagnostics;

namespace Zabbix.Entities;

[DebuggerDisplay("{Name,nq}")]
public sealed class Command
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string Name { get; internal init; }

    public IEnumerable<Argument> Arguments { get; internal init; }

    public string GetString(int index)
        => GetParam(index)?.Value ?? string.Empty;

    public Argument GetParam(int index)
        => Arguments.ElementAtOrDefault(index);

    public static bool TryParse(string raw, out Command command)
    {
        command = default;

        // sample:
        //
        //      my.command.or.key[arg1,arg2,,arg4]
        // 
        // note: arg3 is empty but will be 'parsed' as empty string!
        //

        var start = raw.IndexOf('[');
        var end = raw.IndexOf(']');

        if (start != -1 && end != -1)
        {
            // we cannot parse unclosed key param pair.
            if ((start != 0 && end == -1) || (start == -1 && end != 0))
                return false;

            var name = raw.Substring(0, start);
            var args = raw.AsSpan()[(start + 1)..end].ToString()
                .Split(',', StringSplitOptions.TrimEntries);

            var data = args.Select<string, Argument>((value, pos)
                => new() { Index = pos, Value = value });

            command = new Command { Name = name, Arguments = data };
            return true;
        }
        else
        {
            command = new Command { Name = raw };
            return true;
        }
    }
}
