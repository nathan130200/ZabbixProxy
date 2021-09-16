using System.Text.RegularExpressions;

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