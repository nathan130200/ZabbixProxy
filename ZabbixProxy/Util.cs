using System.Text;

namespace Zabbix;

public static class Util
{
    public static byte[] GetBytes(this string s)
        => Encoding.UTF8.GetBytes(s);

    public static string GetString(this byte[] buf)
        => Encoding.UTF8.GetString(buf);
}