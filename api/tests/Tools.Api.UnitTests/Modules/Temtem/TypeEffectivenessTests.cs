using Tools.Api.Modules.Temtem.Types.Domain;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Temtem;

// La règle du double type est une multiplication, pas une table à part : ces tests la fixent.
public sealed class TypeEffectivenessTests
{
    private const int Feu = 1;
    private const int Eau = 2;
    private const int Nature = 3;

    // Matrice réduite mais complète sur ses trois types : un couple manquant doit rester une
    // erreur, pas un neutre implicite.
    private static readonly TypeEffectiveness Effectiveness = new(new Dictionary<(int, int), decimal>
    {
        [(Feu, Feu)] = 0.5m,   [(Feu, Eau)] = 0.5m,   [(Feu, Nature)] = 2m,
        [(Eau, Feu)] = 2m,     [(Eau, Eau)] = 0.5m,   [(Eau, Nature)] = 0.5m,
        [(Nature, Feu)] = 0.5m,[(Nature, Eau)] = 2m,  [(Nature, Nature)] = 0.5m
    });

    [Theory]
    [InlineData(Eau, Feu, 2)]
    [InlineData(Feu, Nature, 2)]
    [InlineData(Feu, Eau, 0.5)]
    public void Un_type_simple_lit_la_matrice(int techniqueType, int defenderType, decimal attendu)
    {
        Assert.Equal(attendu, Effectiveness.Against(techniqueType, defenderType, null));
    }

    [Fact]
    public void Un_double_type_multiplie_les_deux_multiplicateurs()
    {
        // Eau contre Feu×Nature : 2 × 0.5 = 1, un doublement annulé par une résistance.
        Assert.Equal(1m, Effectiveness.Against(Eau, Feu, Nature));
    }

    [Fact]
    public void Deux_faiblesses_se_cumulent_en_quadruple()
    {
        Assert.Equal(4m, Effectiveness.Against(Feu, Nature, Nature));
    }

    [Fact]
    public void Deux_resistances_se_cumulent_en_quart()
    {
        Assert.Equal(0.25m, Effectiveness.Against(Feu, Feu, Eau));
    }

    // L'ordre des deux types du défenseur ne doit rien changer : la multiplication est commutative.
    [Fact]
    public void L_ordre_des_types_du_defenseur_est_sans_effet()
    {
        Assert.Equal(
            Effectiveness.Against(Eau, Feu, Nature),
            Effectiveness.Against(Eau, Nature, Feu));
    }

    [Fact]
    public void Un_couple_absent_de_la_matrice_est_une_erreur()
    {
        var vide = new TypeEffectiveness(new Dictionary<(int, int), decimal>());

        Assert.Throws<InvalidOperationException>(() => vide.Against(Feu, Eau, null));
    }
}
