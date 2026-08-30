namespace Tools.Api.Modules.Temtem.Types.Domain;

// Efficacité d'une technique contre un Temtem, calculée depuis la matrice simple-type.
//
// **Un double type n'a pas de table à lui.** Le multiplicateur est le produit des deux
// multiplicateurs simple-type :
//
//     mult = mult(type technique → type1) × mult(type technique → type2)
//
// Vérifié le 29/08/2026 contre l'endpoint du site source (mode=double sur Feu×Eau) : le résultat
// correspond au produit des deux résultats mode=simple, valeur par valeur sur les 12 types. C'est
// pourquoi seule la matrice 12×12 est stockée.
//
// Zéro dépendance : ce calcul est la règle du jeu, il ne connaît ni base ni HTTP.
public sealed class TypeEffectiveness
{
    // La matrice est pleine — 144 lignes, multiplicateurs neutres compris — donc un couple absent
    // est une donnée manquante, pas un « neutre » implicite.
    private readonly IReadOnlyDictionary<(int Attacker, int Defender), decimal> matrix;

    public TypeEffectiveness(IReadOnlyDictionary<(int Attacker, int Defender), decimal> matrix)
    {
        this.matrix = matrix;
    }

    public decimal Against(int techniqueTypeId, int defenderType1Id, int? defenderType2Id)
    {
        var multiplier = Single(techniqueTypeId, defenderType1Id);

        return defenderType2Id is { } secondType
            ? multiplier * Single(techniqueTypeId, secondType)
            : multiplier;
    }

    // Un type qui se défend contre lui-même ne change rien au produit : le second type absent est
    // traité comme neutre par l'appelant, pas par un 1 codé ici.
    private decimal Single(int attackerTypeId, int defenderTypeId) =>
        matrix.TryGetValue((attackerTypeId, defenderTypeId), out var multiplier)
            ? multiplier
            : throw new InvalidOperationException(
                $"La matrice d'efficacité ne couvre pas {attackerTypeId} contre {defenderTypeId}.");
}
