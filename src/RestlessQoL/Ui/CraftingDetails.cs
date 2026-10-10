using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace RestlessQoL.Ui;

// Split native tooltip text for presentation; keep values and unrecognised lines intact.
internal static class CraftingDetails
{
    internal sealed class Attribute
    {
        internal string Name="",Current="";
        internal string? Next;
        internal bool Changed=>Next!=null&&Plain(Current)!=Plain(Next);
    }
    internal sealed class Details
    {
        internal string Description="";
        internal readonly List<Attribute> Attributes=new();
    }
    internal static string Plain(string text)=>Regex.Replace(text??"","<[^>]*>","").Trim();
    internal static Details Read(string current,string? next=null)
    {
        var result=Parse(current);
        if(next==null)return result;
        var after=Parse(next);var used=new HashSet<int>();
        foreach(var attribute in result.Attributes)
        {
            var index=after.Attributes.FindIndex(a=>a.Name==attribute.Name&&!used.Contains(after.Attributes.IndexOf(a)));
            if(index<0){attribute.Next="—";continue;}
            used.Add(index);attribute.Next=after.Attributes[index].Current;
        }
        for(var i=0;i<after.Attributes.Count;i++)if(!used.Contains(i))result.Attributes.Add(new Attribute{Name=after.Attributes[i].Name,Current="—",Next=after.Attributes[i].Current});
        return result;
    }
    private static Details Parse(string tooltip)
    {
        var result=new Details();var prose=new List<string>();
        foreach(var line in tooltip.Replace("\r","").Split('\n'))
        {
            var colon=line.IndexOf(':');
            if(colon>0&&Plain(line.Substring(0,colon)).Length>0&&Plain(line.Substring(colon+1)).Length>0)
                result.Attributes.Add(new Attribute{Name=Plain(line.Substring(0,colon)),Current=line.Substring(colon+1).Trim()});
            else if(!string.IsNullOrWhiteSpace(line))prose.Add(line.Trim());
        }
        result.Description=string.Join("\n",prose);return result;
    }
}
