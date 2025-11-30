using System.Text.Json.Serialization;

namespace RunHideInTrayCommon.Json;

[JsonSourceGenerationOptions(IncludeFields = true, Converters = [typeof(IconJsonConverter)])]
[JsonSerializable(typeof(Config))]
public partial class ConfigJsonSerializerContext : JsonSerializerContext;
