using System.Text;

namespace Zabbix;

public static class Util
{
    public static byte[] GetBytes(this string s)
        => Encoding.UTF8.GetBytes(s);

    public static string GetString(this byte[] buf)
        => Encoding.UTF8.GetString(buf);

    public static event Action<byte[]> OnPacketSent;
    public static event Action<byte[]> OnPacketReceived;

    internal static void FireOnPacketSent(byte[] buf)
        => OnPacketSent?.Invoke(buf);

    internal static void FireOnPacketRecv(byte[] buf)
        => OnPacketReceived?.Invoke(buf);
}