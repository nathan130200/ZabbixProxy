using System.Diagnostics;

namespace Zabbix.Entities;

[DebuggerDisplay("Type={Type,nq}; Size={Length,nq}")]
public class ZabbixPacket
{
    public ZabbixPacketType Type { get; set; }
    public byte[] Data { get; set; }
    public int Length => Data?.Length ?? 0;

    public static readonly byte[] Magic = "ZBXD".GetBytes();

    public async Task WriteAsync(Stream s)
    {
        var buf = new byte[1 + (sizeof(int) * 3) + Length];
        var pos = 0;

        Array.Copy(Magic, 0, buf, pos, 4);
        pos += 4;

        buf[pos] = (byte)Type;
        pos++;

        var len = BitConverter.GetBytes(Length);
        Array.Copy(len, 0, buf, pos, 4);
        pos += 4;

        var nullptr = new byte[4];
        Array.Copy(nullptr, 0, buf, pos, 4);
        pos += 4;

        Array.Copy(Data, 0, buf, pos, Length);

        await s.WriteAsync(buf);
        Util.FireOnPacketSent(Data);
    }

    public async Task<bool> ReadAsync(Stream s)
    {
        var buf = new byte[1 + sizeof(int) * 2];
        var cur = 0;

        if (await s.ReadAsync(buf) != buf.Length)
            return false;

        var hdr = buf.AsMemory(0, 4);

        if (!hdr.Span.SequenceEqual(Magic))
            return false;

        cur += 4;
        Type = (ZabbixPacketType)buf[cur++];

        if (Type != ZabbixPacketType.Communication)
            return false;

        var length = BitConverter.ToInt32(buf.AsSpan(cur, 4));

        if (length >= ushort.MaxValue)
            return false;

        Data = new byte[length];

        var reserved = new byte[4];

        if (await s.ReadAsync(reserved) != 4)
            return false;

        if (await s.ReadAsync(Data) != length)
            return false;

        Util.FireOnPacketRecv(Data);
        return true;
    }

    internal static async Task<ZabbixPacket> ParseAsync(Stream stream)
    {
        try
        {
            var pkt = new ZabbixPacket();

            if (!await pkt.ReadAsync(stream))
                return null;

            return pkt;
        }
        catch
        {
            return null;
        }
    }
}
