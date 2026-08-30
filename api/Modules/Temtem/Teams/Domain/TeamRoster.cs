namespace Tools.Api.Modules.Temtem.Teams.Domain;

// Les règles de composition d'une équipe, celles qu'aucune contrainte SQL ne sait exprimer.
//
// La place borne déjà l'équipe à six en base (CHECK 1..6 + unicité), mais elle ne désigne pas
// celle à attribuer au prochain membre : c'est cette décision-là qui vit ici. Le nombre de
// techniques, lui, ne se contraint pas du tout en SQL — compter des lignes n'est pas à la
// portée d'un CHECK.
//
// Zéro dépendance : ces règles ne connaissent ni base ni HTTP.
public static class TeamRoster
{
    public const int MaxMembers = 6;
    public const int MaxTechniquesPerMember = 4;

    // La première place libre, et non « la suivante » : un membre retiré au milieu laisse un
    // trou que le prochain ajout doit reboucher, sinon une équipe de trois pourrait se retrouver
    // pleine. Null quand les six places sont prises.
    public static int? FirstFreeSlot(IReadOnlyCollection<int> occupiedSlots)
    {
        for (var slot = 1; slot <= MaxMembers; slot++)
        {
            if (!occupiedSlots.Contains(slot))
            {
                return slot;
            }
        }

        return null;
    }
}
