namespace Centesimo.Application;

public sealed class ItalianSpokenNumberParser
{
    private static readonly Dictionary<string, int> Numbers = new()
    {
        ["zero"]=0,["uno"]=1,["due"]=2,["tre"]=3,["quattro"]=4,["cinque"]=5,["sei"]=6,["sette"]=7,["otto"]=8,["nove"]=9,
        ["dieci"]=10,["undici"]=11,["dodici"]=12,["tredici"]=13,["quattordici"]=14,["quindici"]=15,["sedici"]=16,["diciassette"]=17,["diciotto"]=18,["diciannove"]=19,
        ["venti"]=20,["trenta"]=30,["quaranta"]=40,["cinquanta"]=50,["sessanta"]=60,["settanta"]=70,["ottanta"]=80,["novanta"]=90
    };
    public bool TryParse(string value, out decimal amount)
    {
        amount = 0;
        var currency = System.Text.RegularExpressions.Regex.Match(value, @"(?<whole>.+?)\s*(?:euro|eur|€)(?:\s+(?:e|virgola)\s+(?<cents>.+))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!currency.Success || !TryParsePart(currency.Groups["whole"].Value, 999999, out var whole)) return false;
        var cents = 0;
        if (currency.Groups["cents"].Success && !TryParsePart(currency.Groups["cents"].Value, 99, out cents)) return false;
        amount = whole + cents / 100m; return true;
    }
    private static bool TryParsePart(string text, int max, out int value)
    {
        if (int.TryParse(text.Trim(), out value)) return value <= max;
        var s=text.ToLowerInvariant().Replace(" ","").Replace("é","e"); value=0;
        if (s.StartsWith("mille")){value=1000;s=s[5..];} else if(s.StartsWith("mila")){value=1000;s=s[4..];}
        if(s.Length==0)return value<=max;
        for(var i=999;i>=1;i--) if(ToItalian(i)==s){value+=i;return value<=max;}
        return false;
    }
    private static string ToItalian(int n)
    {
        if(n<10)return Numbers.First(x=>x.Value==n).Key;
        if(n<20)return Numbers.First(x=>x.Value==n).Key;
        if(n<100){var t=n/10*10;var u=n%10;var p=Numbers.First(x=>x.Value==t).Key;return u==0?p:p.TrimEnd('a','e','i','o','u')+ToItalian(u);}
        var h=n/100;var r=n%100;return (h==1?"cento":ToItalian(h)+"cento")+(r==0?"":ToItalian(r));
    }
}
