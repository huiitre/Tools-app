using System.Text.RegularExpressions;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Todolists.Domain;

public sealed partial class Todolist
{
    public long? Id { get; }

    public string Name
    {
        get;
        private set
        {
            if (string.IsNullOrWhiteSpace(value)) throw AppException.Validation("NAME_REQUIRED", "Le nom de la todolist est obligatoire");
            field = value;
        }
    }

    public bool Active { get; private set; }

    public bool Favorite { get; private set; }

    public string ColorHex
    {
        get;
        private set
        {
            if (string.IsNullOrWhiteSpace(value)) throw AppException.Validation("COLOR_HEX_REQUIRED", "La couleur de la todolist est obligatoire");
            if (!ColorHexPattern().IsMatch(value)) throw AppException.Validation("COLOR_HEX_INVALID", "Le format de couleur est invalide");
            field = value;
        }
    }

    public long DisplayOrder { get; private set; }

    private Todolist(
        long? id,
        string name,
        bool active,
        bool favorite,
        string colorHex,
        long displayOrder
    ) {
        Id = id;
        Name = name;
        Active = active;
        Favorite = favorite;
        ColorHex = colorHex;
        DisplayOrder = displayOrder;

        EnsureInactiveIsNotFavorite();
    }

    [GeneratedRegex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
    private static partial Regex ColorHexPattern();

    // Règle sur deux champs : vérifiée une fois toutes les affectations faites, jamais dans un setter.
    private void EnsureInactiveIsNotFavorite()
    {
        if (!Active && Favorite) throw AppException.Validation("INACTIVE_TODOLIST_CANNOT_BE_FAVORITE", "Une todolist inactive ne peut pas être favorite");
    }

    public static Todolist Rehydrate(
        long id,
        string name,
        bool active,
        bool favorite,
        string colorHex,
        long displayOrder
    ) {
        return new Todolist(
            id: id,
            name: name,
            active: active,
            favorite: favorite,
            colorHex: colorHex,
            displayOrder: displayOrder
        );
    }

    public static Todolist Create(
        string name,
        bool? active,
        bool? favorite,
        string? colorHex,
        long? displayOrder
    ) {
        return new Todolist(
            id: null,
            name: name,
            active: active ?? true,
            favorite: favorite ?? false,
            colorHex: colorHex ?? "#FFFFFF",
            displayOrder: displayOrder ?? 0
        );
    }

    public void Update(
        string? name,
        bool? active,
        bool? favorite,
        string? colorHex,
        long? displayOrder
    ) {
        if (name is not null) Name = name;
        if (active.HasValue) Active = active.Value;
        if (favorite.HasValue) Favorite = favorite.Value;
        if (colorHex is not null) ColorHex = colorHex;
        if (displayOrder.HasValue) DisplayOrder = displayOrder.Value;

        EnsureInactiveIsNotFavorite();
    }
}
