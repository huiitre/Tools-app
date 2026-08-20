using System.Text.Json.Nodes;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Paramètre on / off.
public sealed record BooleanSetting : SettingDefinition<bool>
{
    public override string Kind => "BOOLEAN";

    public override JsonNode DefaultValue => JsonValue.Create(Default);

    public override IReadOnlyDictionary<string, object?> Constraints =>
        new Dictionary<string, object?>();

    public override bool Accepts(JsonNode? value) =>
        value is JsonValue json && json.TryGetValue<bool>(out _);

    public override bool Read(JsonNode value) => value.GetValue<bool>();
}
