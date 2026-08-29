using System.Data;
using Dapper;

namespace Tools.Api.Modules.Riot.Common.Infrastructure;

// **Dapper ne connaît pas DateOnly en écriture.** Il refuse le type avant même d'atteindre le
// pilote : « The member SeenAt of type System.DateOnly cannot be used as a parameter value ».
// La lecture, elle, fonctionne sans rien : Npgsql rend déjà un DateOnly pour une colonne `date`.
//
// Ce gestionnaire ne fait donc que débloquer le sens écriture, en passant la valeur telle quelle
// à Npgsql, qui sait quoi en faire.
//
// L'enregistrement est **global à Dapper**, pas au module. Il est posé ici parce que Riot est le
// premier code de l'API à manipuler une date sans heure (`valorant_store_history.seen_at`) ; il
// remontera dans le Core le jour où un deuxième module en aura besoin.
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.Value = value;
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly dateOnly => dateOnly,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        string text => DateOnly.Parse(text),
        _ => throw new InvalidCastException($"Impossible de lire un DateOnly depuis {value.GetType()}.")
    };
}
