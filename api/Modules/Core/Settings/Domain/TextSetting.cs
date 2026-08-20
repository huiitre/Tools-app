using System.Text.Json.Nodes;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Chaîne libre, bornée en longueur.
public sealed record TextSetting : SettingDefinition<string>
{
    public required int MaxLength { get; init; }

    // Vide autorisé ou non. Une chaîne vide et une absence de valeur ne sont pas la même chose :
    // l'absence fait hériter, le vide est un choix.
    public bool AllowEmpty { get; init; } = true;

    public override string Kind => "TEXT";

    public override JsonNode DefaultValue => JsonValue.Create(Default);

    public override IReadOnlyDictionary<string, object?> Constraints =>
        new Dictionary<string, object?> { ["maxLength"] = MaxLength, ["allowEmpty"] = AllowEmpty };

    public override bool Accepts(JsonNode? value) =>
        value is JsonValue json
        && json.TryGetValue<string>(out var text)
        && text.Length <= MaxLength
        && (AllowEmpty || text.Length > 0);

    public override string Read(JsonNode value) => value.GetValue<string>();
}
