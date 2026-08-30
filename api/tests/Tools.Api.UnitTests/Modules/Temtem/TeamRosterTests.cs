using Tools.Api.Modules.Temtem.Teams.Domain;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Temtem;

public sealed class TeamRosterTests
{
    [Fact]
    public void Une_equipe_vide_commence_a_la_place_1()
    {
        Assert.Equal(1, TeamRoster.FirstFreeSlot([]));
    }

    [Fact]
    public void Les_places_se_remplissent_dans_l_ordre()
    {
        Assert.Equal(4, TeamRoster.FirstFreeSlot([1, 2, 3]));
    }

    [Fact]
    public void Un_trou_laisse_par_un_retrait_est_rebouche_avant_la_suite()
    {
        // Sans cette règle, une équipe de trois pourrait se retrouver « pleine » après quelques
        // retraits : la place suivante ne serait jamais la première libre.
        Assert.Equal(2, TeamRoster.FirstFreeSlot([1, 3, 4, 5, 6]));
    }

    [Fact]
    public void Une_equipe_de_six_n_a_plus_de_place()
    {
        Assert.Null(TeamRoster.FirstFreeSlot([1, 2, 3, 4, 5, 6]));
    }

    [Fact]
    public void L_ordre_des_places_occupees_est_indifferent()
    {
        Assert.Equal(3, TeamRoster.FirstFreeSlot([6, 2, 1, 4]));
    }
}
