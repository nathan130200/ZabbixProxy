﻿using System.Diagnostics;

namespace Zabbix.Entities;

[DebuggerDisplay("Command={Name}")]
public sealed class Command
{
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

[DebuggerDisplay("#{Index,nq}: {Value,nq}")]
public class Argument
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public int Index { get; internal init; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string Value { get; internal init; }

    public bool AsBoolean()
        => Value?.Equals("true") == true
            || Value?.Equals("1") == true
            || (bool.TryParse(Value, out bool v) && v);

    public ushort AsUInt16()
        => ushort.TryParse(Value, out var v) ? v : (ushort)0;

    public uint AsUInt32()
        => uint.TryParse(Value, out var v) ? v : 0U;

    public ulong AsUInt64()
        => ulong.TryParse(Value, out var v) ? v : 0UL;

    public short AsIn16()
        => short.TryParse(Value, out var v) ? v : (short)0;

    public int AsInt32()
        => int.TryParse(Value, out var v) ? v : 0;

    public long AsInt64()
        => long.TryParse(Value, out var v) ? v : 0L;

    public float AsFloat()
        => float.TryParse(Value, out var v) ? v : 0f;

    public double AsDouble()
        => double.TryParse(Value, out var v) ? v : 0d;
}