using RestlessCook;
KitchenRun.TestRouting();

// Only game data boundaries are substituted. Catalogue and graph construction
// are the production implementations linked by the test project.
namespace UnityEngine { }
namespace RestlessCook
{
    internal class ZNetScene { internal static ZNetScene? instance = new(); }
    internal class ObjectDB { internal static ObjectDB? instance; internal List<Recipe> m_recipes = new(); }
    internal class Localization { internal static Localization? instance; internal string Localize(string s) => s; }
    internal class GameObject { internal string name; internal GameObject(string n) { name = n; } }
    internal class CraftingStation { internal GameObject gameObject; internal CraftingStation(string n) { gameObject = new(n); } }
    internal class Shared { internal string m_name = ""; }
    internal class ItemData { internal Shared m_shared = new(); }
    internal class ItemDrop
    {
        internal string name; internal GameObject gameObject; internal ItemData m_itemData = new();
        internal ItemDrop(string n) { name=n; gameObject=new(n); m_itemData.m_shared.m_name=n; }
    }
    internal class Requirement { internal ItemDrop? m_resItem; internal int m_amount; }
    internal class Recipe
    {
        internal string name = ""; internal bool m_enabled = true; internal ItemDrop? m_item;
        internal CraftingStation? m_craftingStation; internal int m_minStationLevel=1, m_amount=1;
        internal Requirement[] m_resources = Array.Empty<Requirement>();
    }
    internal enum KitchenStationKind { None,Cauldron,PrepTable,Rack,Oven,MeadKettle,Fermenter }
    internal class KitchenUse { internal string Item="",Name=""; internal int Amount; }
    internal class KitchenStep
    {
        internal string Id="",Name="",Output="",Parent="",StationPrefab="";
        internal int Depth,Need,StationLevel,ParentIndex; internal KitchenStationKind Station;
        internal List<KitchenUse> Uses=new();
    }
    internal static partial class KitchenRun
    {
        private class Conversion { internal string From=""; internal KitchenStationKind Kind; internal int Amount=1; }
        private static readonly Dictionary<string,Conversion> Products=new(StringComparer.Ordinal);
        private static readonly Dictionary<string,CookRow> ById=new(StringComparer.Ordinal),ByOutput=new(StringComparer.Ordinal);
        private static readonly Dictionary<string,List<CookRow>> UsedBy=new(StringComparer.Ordinal);
        private static List<CookRow> _configuredRows=new(),_rows=new();
        private static void EnsureConversions() { }
        private static string OutputOf(CookRow row)=>row.Prefab;
        private static string Clean(string s)=>s.Replace("(Clone)","");
        private static string Label(string s)=>s;
        private static ItemDrop Item(string s)=>new(s);
        internal static void TestRouting()
        {
            var checks=0;
            void Check(bool ok,string message) { checks++;if(!ok)throw new Exception(message); }
            var bread=new CookRow{Id="bread",Prefab="Bread",Name="Bread",Station="piece_preptable",OutputAmount=2};
            bread.Uses.Add(new CookUse{Item="BarleyFlour",Amount=10});
            var pie=new CookRow{Id="pie",Prefab="LoxPie",Name="Pie",Station="piece_preptable",StationLevel=3};
            pie.Uses.Add(new CookUse{Item="CookedLoxMeat",Amount=2});
            _configuredRows.AddRange(new[]{bread,pie});
            Products["Bread"]=new Conversion{From="BreadDough",Kind=KitchenStationKind.Oven};
            Products["LoxPie"]=new Conversion{From="LoxPieUncooked",Kind=KitchenStationKind.Oven};
            Products["CookedLoxMeat"]=new Conversion{From="LoxMeat",Kind=KitchenStationKind.Rack};
            Products["MeadHealth"]=new Conversion{From="MeadBaseHealth",Kind=KitchenStationKind.Fermenter,Amount=6};
            var raw=new Recipe{name="Recipe_Bread",m_item=new("BreadDough"),m_amount=2,m_minStationLevel=2,m_craftingStation=new("piece_preptable")};
            var baseRecipe=new Recipe{name="Mead base",m_item=new("MeadBaseHealth"),m_craftingStation=new("piece_meadcauldron"),
                m_resources=new[]{new Requirement{m_resItem=new("Honey"),m_amount=10}}};
            ObjectDB.instance=new ObjectDB{m_recipes=new(){raw,baseRecipe}};
            EnsureMeads();
            var steps=Expand("Bread",3,0,"");
            Check(steps[0].Station==KitchenStationKind.Oven && steps[0].StationLevel==1 && steps[0].StationPrefab=="","A finished food row must never bypass its native oven conversion");
            Check(steps[0].Uses.Single().Item=="BreadDough" && steps[0].Uses.Single().Amount==3,"Oven must require one raw item per finished item");
            Check(steps[1].Station==KitchenStationKind.PrepTable && steps[1].StationLevel==2,"Dough must use the native preparation station and its upgrade requirement");
            Check(steps[1].Uses.Single().Amount==20,"Three bread must prepare two batches of dough at the configured cost");
            Check(OutputAmount(steps[1])==2,"Preparation must retain its configured batch yield");
            steps=Expand("LoxPie",1,0,"");
            Check(steps.Select(s=>s.Output).SequenceEqual(new[]{"LoxPie","LoxPieUncooked","CookedLoxMeat","LoxMeat"}),"Pie must prepare cooked meat on the rack, assemble a raw pie, then bake it");
            Check(steps[2].Station==KitchenStationKind.Rack,"Cooked meat must use a real rack even within a baked-food recipe");
            Check(steps[1].StationLevel==3,"Configured station progression must not be lost when a native raw recipe is absent");
            steps=Expand("MeadHealth",7,0,"");
            Check(steps[0].Station==KitchenStationKind.Fermenter && steps[0].Uses.Single().Amount==2,"Seven meads must require two native six-item fermenter batches");
            Check(steps[1].Station==KitchenStationKind.MeadKettle && steps[1].Uses.Single().Amount==20,"Mead must first prepare its bases at the native kettle using native costs");
            Check(_rows.Any(r=>r.Prefab=="MeadHealth" && r.Kind=="mead"),"Finished meads must be browsable and filterable");
            var count=_rows.Count; ObjectDB.instance=new ObjectDB{m_recipes=new(){raw,baseRecipe}}; EnsureMeads();
            Check(_rows.Count==count,"Changing world database must rebuild without duplicate native rows");
            var plain=new CookRow{Prefab="Soup",Station="piece_cauldron",OutputAmount=1}; plain.Uses.Add(new CookUse{Item="Vegetable",Amount=2});ByOutput["Soup"]=plain;
            steps=Expand("Soup",1,0,"");
            Check(steps[0].Station==KitchenStationKind.Cauldron && steps[1].Output=="Vegetable","Recipes without native machine conversions must preserve preparation behavior");
            Console.WriteLine($"{checks} kitchen routing regressions passed.");
        }
    }
}
