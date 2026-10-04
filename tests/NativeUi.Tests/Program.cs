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
foreach (var handle in reader.TypeDefinitions)
{
    var type = reader.GetTypeDefinition(handle);
    if (reader.GetString(type.Name) != "Smelter") continue;
    foreach (var mh in type.GetMethods())
    {
        var method = reader.GetMethodDefinition(mh);
        var name = reader.GetString(method.Name);
        var signature = method.DecodeSignature(provider,(object?)null);
        Console.WriteLine("TOKEN " + System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(mh).ToString("X8") + " " + name);
        Console.WriteLine("SMELTER " + name + "(" + string.Join(",",signature.ParameterTypes) + ") " + signature.ReturnType);
        if (method.RelativeVirtualAddress == 0 || (!name.Contains("Spawn") && !name.Contains("Processed"))) continue;
        var il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
        Console.WriteLine("IL " + name + " " + Convert.ToHexString(il));
    }
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

