using Nop.Data.Mapping;
using Nop.Plugin.Misc.NopwaraQA.Domain;

namespace Nop.Plugin.Misc.NopwaraQA.Data;

/// <summary>
/// Plugin table naming compatibility
/// </summary>
public class NopwaraQANameCompatibility : INameCompatibility
{
    public Dictionary<Type, string> TableNames => new()
    {
        [typeof(ProductQuestion)] = "NopwaraProductQuestion"
    };

    public Dictionary<(Type, string), string> ColumnName => new();
}
