using IconExtensions;

using System.Text.Json.Serialization;

namespace RunHideInTrayCommon;

[JsonSourceGenerationOptions(IncludeFields = true, Converters = [typeof(IconJsonConverter)])]
[JsonSerializable(typeof(Config))]
public partial class ConfigJsonSerializerContext : JsonSerializerContext;
