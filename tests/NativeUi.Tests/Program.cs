using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using var stream = File.OpenRead(Path.Combine(Environment.GetEnvironmentVariable("VHINSTALL")!, "Valheim_Data/Managed/assembly_valheim.dll"));
using var pe = new PEReader(stream);
var reader = pe.GetMetadataReader();
var provider = new Types();
string? selectionType = null;
foreach (var handle in reader.TypeDefinitions)
{
    var type = reader.GetTypeDefinition(handle);
    if (reader.GetString(type.Name) != "InventoryGui") continue;
    foreach (var fh in type.GetFields())
    {
        var field = reader.GetFieldDefinition(fh);
        if (reader.GetString(field.Name) == "m_selectedRecipe")
            selectionType = field.DecodeSignature(provider, (object?)null);
    }
}
if (selectionType == null) throw new Exception("Native selected recipe field is missing.");
var recipes = 0; var items = 0;
foreach (var handle in reader.TypeDefinitions)
{
    var type = reader.GetTypeDefinition(handle);
    if (reader.GetString(type.Name) != selectionType) continue;
    foreach (var fh in type.GetFields())
    {
        var field = reader.GetFieldDefinition(fh);
        if ((field.Attributes & System.Reflection.FieldAttributes.Static) != 0) continue;
        var signature = field.DecodeSignature(provider, (object?)null);
        Console.WriteLine(selectionType + "." + reader.GetString(field.Name) + ": " + signature);
        if (signature == "Recipe") recipes++;
        if (signature == "ItemData") items++;
    }
}
if (recipes != 1 || items != 1)
    throw new Exception($"Selected recipe contract changed: {selectionType}, Recipe fields={recipes}, ItemData fields={items}");
Console.WriteLine("Native recipe selection contract passed.");
// Validate the native input queue separately from the processed-output buffer.
var inputPrefix = false; var firstInput = false;
foreach (var handle in reader.TypeDefinitions)
{
    var type = reader.GetTypeDefinition(handle);
    if (reader.GetString(type.Name) != "Smelter") continue;
    foreach (var mh in type.GetMethods())
    {
        var method = reader.GetMethodDefinition(mh); var name = reader.GetString(method.Name);
        if (name == "GetQueuedOre")
        {
            var signature = method.DecodeSignature(provider, (object?)null);
            firstInput = signature.ParameterTypes.Length == 0 && signature.ReturnType == "String";
        }
        if (name != "QueueOre" || method.RelativeVirtualAddress == 0) continue;
        var il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
        for (var i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] != 0x72 || il[i + 4] != 0x70) continue;
            var token = BitConverter.ToInt32(il, i + 1);
            inputPrefix |= reader.GetUserString(System.Reflection.Metadata.Ecma335.MetadataTokens.UserStringHandle(token & 0xffffff)) == "item";
        }
    }
}
if (!inputPrefix || !firstInput) throw new Exception("Native smelter input queue contract changed; inspect QueueOre before updating ProductionQueue.");
Console.WriteLine("Native smelter input queue contract passed.");
// The cooking adapters call native owner-side insertion and collection paths.
// Fail CI rather than silently compiling a changed timer or content contract.
var cookingContracts = new Dictionary<string,string[]>
{
    ["WearNTear.SetupColliders"] = new[]{"Void"},
    ["WearNTear.OnPlaced"] = new[]{"Void"},
    ["Feast.Start"] = new[]{"Void"},
    ["Feast.UpdateVisual"] = new[]{"Void"},
    ["CookingStation.RPC_AddItem"] = new[]{"Void","Int64","String","Boolean"},
    ["CookingStation.GetFreeSlot"] = new[]{"Int32"},
    ["CookingStation.GetSlot"] = new[]{"Void","Int32","String&","Single&","Status&","Boolean&"},
    ["CookingStation.SetSlot"] = new[]{"Void","Int32","String","Single","Status","Boolean","Int64"},
    ["CookingStation.SetSlotVisual"] = new[]{"Void","Int32","String","Boolean","Status"},
    ["Fermenter.RPC_AddItem"] = new[]{"Void","Int64","Int32","Boolean"},
    ["Fermenter.GetContent"] = new[]{"Int32"},
    ["Fermenter.GetFermentationTime"] = new[]{"Double"},
    ["Fermenter.RPC_Tap"] = new[]{"Void","Int64"},
    ["Fermenter.DelayedTap"] = new[]{"Void"},
    ["Smelter.GetBakeTimer"] = new[]{"Single"},
    ["Smelter.Spawn"] = new[]{"Void","String","Int32"}
};
foreach (var handle in reader.TypeDefinitions)
{
    var type=reader.GetTypeDefinition(handle);var typeName=reader.GetString(type.Name);
    foreach (var mh in type.GetMethods())
    {
        var method=reader.GetMethodDefinition(mh);var key=typeName+"."+reader.GetString(method.Name);
        if (!cookingContracts.TryGetValue(key,out var expected)) continue;
        var signature=method.DecodeSignature(provider,(object?)null);
        if (signature.ReturnType==expected[0] && signature.ParameterTypes.SequenceEqual(expected.Skip(1))) cookingContracts.Remove(key);
    }
}
if (cookingContracts.Count>0) throw new Exception("Native cooking contracts changed: "+string.Join(", ",cookingContracts.Keys));
Console.WriteLine("16 native cooking and feast lifecycle contracts passed.");
// Optional binary check catches packages compiled against the removed overload,
// even when recompiling unchanged C# would bind a new optional parameter.
var cookDll = Environment.GetEnvironmentVariable("RESTLESS_COOK_DLL");
if (!string.IsNullOrEmpty(cookDll))
{
    using var cookStream = File.OpenRead(cookDll);
    using var cookPe = new PEReader(cookStream);
    var cookReader = cookPe.GetMetadataReader();
    var nativeRackMethods = new HashSet<string>();
    foreach (var th in reader.TypeDefinitions)
    {
        var type = reader.GetTypeDefinition(th);
        if (reader.GetString(type.Name) != "CookingStation") continue;
        foreach (var mh in type.GetMethods())
        {
            var method = reader.GetMethodDefinition(mh);
            var sig = method.DecodeSignature(provider, (object?)null);
            nativeRackMethods.Add(reader.GetString(method.Name) + ":" + sig.ReturnType + "(" + string.Join(",", sig.ParameterTypes) + ")");
        }
    }
    var checkedMethods = 0;
    foreach (var mh in cookReader.MemberReferences)
    {
        var method = cookReader.GetMemberReference(mh);
        if (method.GetKind() != MemberReferenceKind.Method || method.Parent.Kind != HandleKind.TypeReference) continue;
        var type = cookReader.GetTypeReference((TypeReferenceHandle)method.Parent);
        if (cookReader.GetString(type.Name) != "CookingStation") continue;
        var sig = method.DecodeMethodSignature(provider, (object?)null);
        var key = cookReader.GetString(method.Name) + ":" + sig.ReturnType + "(" + string.Join(",", sig.ParameterTypes) + ")";
        if (!nativeRackMethods.Contains(key)) throw new Exception("Cook binary references missing native CookingStation method: " + key);
        checkedMethods++;
    }
    if (checkedMethods == 0) throw new Exception("Cook binary has no CookingStation method references to validate.");
    Console.WriteLine($"{checkedMethods} Cook binary cooking-station method references resolve.");
}
// Custom Crafting drives native selection/actions while the native UI is hidden.
var craftMethods=new Dictionary<string,string[]>
{
    ["InventoryGui.SetRecipe"]=new[]{"Void","Int32","Boolean"},
    ["InventoryGui.OnCraftPressed"]=new[]{"Void"},
    ["InventoryGui.OnCraftCancelPressed"]=new[]{"Void"},
    ["InventoryGui.DoCrafting"]=new[]{"Void","Player"},
    ["InventoryGui.OnRepairPressed"]=new[]{"Void"},
    ["InventoryGui.UpdateRepair"]=new[]{"Void"},
    ["InventoryGui.OnTabCraftPressed"]=new[]{"Void"},
    ["InventoryGui.OnTabUpgradePressed"]=new[]{"Void"},
    ["InventoryGui.UpdateGamepad"]=new[]{"Void"},
    ["InventoryGui.UpdateRecipeGamepadInput"]=new[]{"Void"},
    ["InventoryGui.UpdateRecipe"]=new[]{"Void","Player","Single"},
    ["Player.HaveRequirements"]=new[]{"Boolean","Recipe","Boolean","Int32","Int32"},
    ["Recipe.GetAmount"]=new[]{"Int32","Int32","Int32&","ItemData&","Int32"},
    ["Inventory.CanAddItem"]=new[]{"Boolean","GameObject","Int32"},
    ["Player.SetSelectedPiece"]=new[]{"Boolean","Piece"},
};
var foundCraft=new HashSet<string>();
foreach(var handle in reader.TypeDefinitions)
{
    var type=reader.GetTypeDefinition(handle);var name=reader.GetString(type.Name);
    if(name!="InventoryGui"&&name!="Player"&&name!="Recipe"&&name!="Inventory")continue;
    foreach(var mh in type.GetMethods())
    {
        var method=reader.GetMethodDefinition(mh);var key=name+"."+reader.GetString(method.Name);
        if(!craftMethods.TryGetValue(key,out var expected))continue;
        var signature=method.DecodeSignature(provider,(object?)null);
        if(signature.ReturnType==expected[0]&&signature.ParameterTypes.SequenceEqual(expected.Skip(1)))foundCraft.Add(key);
    }
}
if(foundCraft.Count!=craftMethods.Count)throw new Exception("Native crafting window contracts changed: "+string.Join(", ",craftMethods.Keys.Except(foundCraft)));
Console.WriteLine($"{craftMethods.Count} native crafting window contracts passed.");
var controllerMethods = new Dictionary<string,string[]>
{
    ["HotkeyBar.UpdateIcons"] = new[]{"Void","Player"},
    ["InventoryGrid.UpdateGamepad"] = new[]{"Void"},
    ["InventoryGrid.SetGamepadSelection"] = new[]{"Void","Vector2i"},
    ["InventoryGrid.GetGamepadSelectedItem"] = new[]{"ItemData"},
    ["InventoryGrid.GetGamepadSelectedElement"] = new[]{"RectTransform"},
    ["InventoryGrid.get_SelectionGridPosition"] = new[]{"Vector2i"},
    ["InventoryGui.IsContainerOpen"] = new[]{"Boolean"},
    ["InventoryGui.OnStackAll"] = new[]{"Void"},
    ["ZInput.IsExclusiveGamepadActive"] = new[]{"Boolean"},
    ["ZInput.GetButtonDown"] = new[]{"Boolean","String"},
    ["ZInput.GetButton"] = new[]{"Boolean","String"}
};
var controllerFields = new Dictionary<string,string>
{
    ["HotkeyBar.m_selected"] = "Int32",
    ["InventoryGrid.m_uiGroup"] = "UIGroupHandler",
    ["InventoryGrid.m_selected"] = "Vector2i",
    ["InventoryGrid.jumpToNextContainer"] = "Boolean",
    ["UIGamePad.m_zinputKey"] = "String"
};
var controllerOwners = new HashSet<string>{"HotkeyBar","InventoryGrid","InventoryGui","InventoryElement","UIGamePad","UIGroupHandler","ZInput"};
var controllerDefinitions = new HashSet<string>();
foreach(var assembly in new[]{"assembly_valheim.dll","assembly_utils.dll","assembly_guiutils.dll"})
{
    using var inputStream=File.OpenRead(Path.Combine(Environment.GetEnvironmentVariable("VHINSTALL")!,"Valheim_Data/Managed",assembly));
    using var inputPe=new PEReader(inputStream);var inputReader=inputPe.GetMetadataReader();
    foreach(var th in inputReader.TypeDefinitions)
    {
    var type=inputReader.GetTypeDefinition(th);var typeName=inputReader.GetString(type.Name);
    foreach(var mh in type.GetMethods())
    {
        var method=inputReader.GetMethodDefinition(mh);var key=typeName+"."+inputReader.GetString(method.Name);
        var sig=method.DecodeSignature(provider,(object?)null);
        if(controllerOwners.Contains(typeName))controllerDefinitions.Add(key+":"+sig.ReturnType+"("+string.Join(",",sig.ParameterTypes)+")");
        if(!controllerMethods.TryGetValue(key,out var expected))continue;
        if(sig.ReturnType==expected[0]&&sig.ParameterTypes.SequenceEqual(expected.Skip(1)))controllerMethods.Remove(key);
    }
    foreach(var fh in type.GetFields())
    {
        var field=inputReader.GetFieldDefinition(fh);var key=typeName+"."+inputReader.GetString(field.Name);
        if(controllerOwners.Contains(typeName))controllerDefinitions.Add(key+":"+field.DecodeSignature(provider,(object?)null));
        if(controllerFields.TryGetValue(key,out var expected)&&field.DecodeSignature(provider,(object?)null)==expected)controllerFields.Remove(key);
    }
    }
}
if(controllerMethods.Count+controllerFields.Count>0)throw new Exception("Controller native contracts changed: "+string.Join(", ",controllerMethods.Keys.Concat(controllerFields.Keys)));
Console.WriteLine("16 native controller API contracts passed.");
var coreDll=Environment.GetEnvironmentVariable("RESTLESS_CORE_DLL");
if(!string.IsNullOrEmpty(coreDll))
{
    using var coreStream=File.OpenRead(coreDll);using var corePe=new PEReader(coreStream);
    var coreReader=corePe.GetMetadataReader();var checkedReferences=0;
    foreach(var rh in coreReader.MemberReferences)
    {
        var member=coreReader.GetMemberReference(rh);
        if(member.Parent.Kind!=HandleKind.TypeReference)continue;
        var type=coreReader.GetTypeReference((TypeReferenceHandle)member.Parent);
        var owner=coreReader.GetString(type.Name);if(!controllerOwners.Contains(owner))continue;
        var key=owner+"."+coreReader.GetString(member.Name)+":";
        if(member.GetKind()==MemberReferenceKind.Method)
        {
            var sig=member.DecodeMethodSignature(provider,(object?)null);
            key+=sig.ReturnType+"("+string.Join(",",sig.ParameterTypes)+")";
        }
        else key+=member.DecodeFieldSignature(provider,(object?)null);
        if(!controllerDefinitions.Contains(key))throw new Exception("Core binary references missing native controller member: "+key);
        checkedReferences++;
    }
    if(checkedReferences==0)throw new Exception("No Core controller member references validated.");
    var names=coreReader.TypeDefinitions.Select(h=>coreReader.GetString(coreReader.GetTypeDefinition(h).Name)).ToHashSet();
    if(!names.Contains("ControllerNavigation")||!names.Contains("ControllerUi"))throw new Exception("Core binary lacks controller fixes.");
    Console.WriteLine($"{checkedReferences} Core binary controller member references resolve; controller implementations present.");
}

sealed class Types : ISignatureTypeProvider<string, object?>
{
    public string GetArrayType(string t, ArrayShape s) => t + "[]";
    public string GetByReferenceType(string t) => t + "&";
    public string GetFunctionPointerType(MethodSignature<string> s) => "function";
    public string GetGenericInstantiation(string t, ImmutableArray<string> a) => t + "<" + string.Join(",",a) + ">";
    public string GetGenericMethodParameter(object? c, int i) => "!!" + i;
    public string GetGenericTypeParameter(object? c, int i) => "!" + i;
    public string GetModifiedType(string m, string t, bool r) => t;
    public string GetPinnedType(string t) => t;
    public string GetPointerType(string t) => t + "*";
    public string GetPrimitiveType(PrimitiveTypeCode t) => t.ToString();
    public string GetSZArrayType(string t) => t + "[]";
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) => r.GetString(r.GetTypeDefinition(h).Name);
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) => r.GetString(r.GetTypeReference(h).Name);
    public string GetTypeFromSpecification(MetadataReader r, object? c, TypeSpecificationHandle h, byte k) => r.GetTypeSpecification(h).DecodeSignature(this,c);
}
