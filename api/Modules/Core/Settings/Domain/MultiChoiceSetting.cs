using System.Text.Json.Nodes;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Sélection multiple parmi une liste fermée — un groupe de cases à cocher côté frontend.
//
// C'est ce type qui a imposé `JSONB` plutôt que `TEXT` pour la colonne `value` : sa valeur est
// un tableau. En texte, il aurait fallu un séparateur, donc un encodage maison, qui casse le
// jour où une option le contient.
public sealed record MultiChoiceSetting : SettingDefinition<IReadOnlyList<string>>
{
    public required IReadOnlyList<string> Options { get; init; }

    public int MinSelected { get; init; }
    public int? MaxSelected { get; init; }

    public override string Kind => "MULTI_CHOICE";

    public override JsonNode DefaultValue => new JsonArray([.. Default.Select(o => (JsonNode)JsonValue.Create(o))]);

    public override IReadOnlyDictionary<string, object?> Constraints =>
        new Dictionary<string, object?>
        {
            ["options"] = Options,
            ["minSelected"] = MinSelected,
            ["maxSelected"] = MaxSelected
        };

    public override bool Accepts(JsonNode? value)
    {
        if (value is not JsonArray array)
        {
            return false;
        }

        var selected = new List<string>(array.Count);
        foreach (var item in array)
        {
            if (item is not JsonValue json || !json.TryGetValue<string>(out var choice))
            {
                return false;
            }

            selected.Add(choice);
        }

        // Un doublon n'est pas une sélection valide : il fausserait le compte face aux bornes,
        // et le frontend n'a aucun moyen de le produire.
        return selected.Count == selected.Distinct(StringComparer.Ordinal).Count()
            && selected.All(choice => Options.Contains(choice, StringComparer.Ordinal))
            && selected.Count >= MinSelected
            && (MaxSelected is null || selected.Count <= MaxSelected);
    }

    public override IReadOnlyList<string> Read(JsonNode value) =>
        [.. ((JsonArray)value).Select(item => item!.GetValue<string>())];
}
