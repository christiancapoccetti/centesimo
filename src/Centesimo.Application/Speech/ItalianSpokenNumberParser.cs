namespace Centesimo.Application;

public sealed class ItalianSpokenNumberParser
{
    private static readonly Dictionary<string, int> Lexicon = new()
    {
        ["zero"]=0,["uno"]=1,["due"]=2,["tre"]=3,["quattro"]=4,["cinque"]=5,["sei"]=6,["sette"]=7,["otto"]=8,["nove"]=9,["dieci"]=10,["undici"]=11,["dodici"]=12,["tredici"]=13,["quattordici"]=14,["quindici"]=15,["sedici"]=16,["diciassette"]=17,["diciotto"]=18,["diciannove"]=19,["venti"]=20,["trenta"]=30,["quaranta"]=40,["cinquanta"]=50,["sessanta"]=60,["settanta"]=70,["ottanta"]=80,["novanta"]=90
    };
    public bool TryParse(string phrase, out decimal amount)
    {
        amount=0; var parts=phrase.ToLowerInvariant().Split(new[]{"euro","eur","€"},StringSplitOptions.None); if(parts.Length<2)return false;
        if(!TryWhole(parts[0],out var whole)||whole>999999)return false; var cents=0;
        var tail=parts[1].Trim().TrimStart('e',' ').Replace("virgola","").Trim();
        if(tail.Length>0&&!TryWhole(tail,out cents)||cents>99)return false; amount=whole+cents/100m;return true;
    }
    private static bool TryWhole(string text,out int number)
    {
        if(int.TryParse(text.Trim(),out number))return true; var s=text.Replace(" ","").Trim(); number=0;
        for(var thousands=999;thousands>=1;thousands--) {var p=Word(thousands)+"mila"; if(s.StartsWith(p)){number=thousands*1000;s=s[p.Length..];break;}}
        if(s.StartsWith("mille")){number=1000;s=s[5..];}
        if(s.Length==0)return true; for(var i=999;i>=0;i--)if(Word(i)==s){number+=i;return true;} return false;
    }
    private static string Word(int n)
    {
        if(n<20)return Lexicon.First(x=>x.Value==n).Key; if(n<100){var t=n/10*10;var u=n%10;var w=Lexicon.First(x=>x.Value==t).Key;return u==0?w:(u is 1 or 8?w[..^1]:w)+Word(u);} var h=n/100;return(h==1?"cento":Word(h)+"cento")+(n%100==0?"":Word(n%100));
    }
}
