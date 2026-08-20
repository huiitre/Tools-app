using System.Text.Json.Nodes;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Choix unique parmi une liste fermée — un groupe de boutons radio côté frontend.
public sealed record ChoiceSetting : SettingDefinition<string>
{
    public required IReadOnlyList<string> Options { get; init; }

    public override string Kind => "CHOICE";

    public override JsonNode DefaultValue => JsonValue.Create(Default);

    public override IReadOnlyDictionary<string, object?> Constraints =>
        new Dictionary<string, object?> { ["options"] = Options };

    // Comparaison ordinale et sensible à la casse : une option est un code, pas un libellé.
    public override bool Accepts(JsonNode? value) =>
        value is JsonValue json
        && json.TryGetValue<string>(out var choice)
        && Options.Contains(choice, StringComparer.Ordinal);

    public override string Read(JsonNode value) => value.GetValue<string>();
}
