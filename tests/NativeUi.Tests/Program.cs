using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using var stream = File.OpenRead(Path.Combine(Environment.GetEnvironmentVariable("VHINSTALL")!, "Valheim_Data/Managed/assembly_valheim.dll"));
using var pe = new PEReader(stream);
var reader = pe.GetMetadataReader();
var provider = new Types();
foreach (var handle in reader.TypeDefinitions)
{
    var type = reader.GetTypeDefinition(handle);
    if (reader.GetString(type.Name) != "InventoryGui") continue;
    foreach (var fh in type.GetFields())
    {
        var field = reader.GetFieldDefinition(fh);
        var name = reader.GetString(field.Name);
        if (name.Contains("Recipe", StringComparison.OrdinalIgnoreCase) || name.Contains("Station"))
            Console.WriteLine(name + ": " + field.DecodeSignature(provider, (object?)null));
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
