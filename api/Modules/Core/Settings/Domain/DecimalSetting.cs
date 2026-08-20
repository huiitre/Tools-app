using System.Text.Json.Nodes;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Nombre décimal borné. Les bornes sont inclusives.
//
// `decimal` et non `double` : les valeurs manipulées ici sont saisies par un humain et
// comparées à des bornes rondes. Un `double` ferait échouer une borne à 0.1 pour une raison
// que personne ne pourrait expliquer depuis l'interface.
public sealed record DecimalSetting : SettingDefinition<decimal>
{
    public required decimal Min { get; init; }
    public required decimal Max { get; init; }

    public override string Kind => "DECIMAL";

    public override JsonNode DefaultValue => JsonValue.Create(Default);

    public override IReadOnlyDictionary<string, object?> Constraints =>
        new Dictionary<string, object?> { ["min"] = Min, ["max"] = Max };

    // Un entier JSON est une valeur décimale acceptable : `3` vaut `3.0`.
    public override bool Accepts(JsonNode? value) =>
        value is JsonValue json
        && json.TryGetValue<decimal>(out var number)
        && number >= Min
        && number <= Max;

    public override decimal Read(JsonNode value) => value.GetValue<decimal>();
}
