using System.Diagnostics;

namespace Zabbix.Entities;

[DebuggerDisplay("{Value,nq}")]
public class Argument
{
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