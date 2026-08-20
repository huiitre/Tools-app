using System.Text.Json.Nodes;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Entier borné. Les bornes sont inclusives.
public sealed record IntegerSetting : SettingDefinition<long>
{
    public required long Min { get; init; }
    public required long Max { get; init; }

    public override string Kind => "INTEGER";

    public override JsonNode DefaultValue => JsonValue.Create(Default);

    public override IReadOnlyDictionary<string, object?> Constraints =>
        new Dictionary<string, object?> { ["min"] = Min, ["max"] = Max };

    // `TryGetValue<long>` refuse un JSON décimal : 2.5 n'est pas un entier tronqué en silence,
    // c'est une valeur d'un autre type.
    public override bool Accepts(JsonNode? value) =>
        value is JsonValue json
        && json.TryGetValue<long>(out var number)
        && number >= Min
        && number <= Max;

    public override long Read(JsonNode value) => value.GetValue<long>();
}
