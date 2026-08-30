namespace Tools.Api.Modules.Temtem.Teams.Application.Commands;

// Le propriétaire n'est jamais une commande : il vient de l'appelant validé, hors de portée du
// client.

// `TemtemId` sert la popup du catalogue : « créer une équipe » y crée l'équipe **et** y place le
// Temtem dans la foulée. Deux appels laisseraient une équipe vide derrière eux si le second
// échouait.
public sealed record CreateTemtemTeamCommand(string Name, int? TemtemId);

public sealed record RenameTemtemTeamCommand(long TeamId, string Name);

public sealed record AddTemtemTeamMemberCommand(long TeamId, int TemtemId, int? Slot);

// Le drag-and-drop rend l'ordre entier : le remplacer évite les cas ambigus de déplacements
// successifs et permet de vérifier que le client ne retire ni n'ajoute aucun membre.
public sealed record ReorderTemtemTeamMembersCommand(long TeamId, IReadOnlyList<long> MemberIds);

// Le remplacement est total : la liste envoyée devient la liste retenue. Une liste vide efface
// les techniques du membre, ce qui est un choix valable en cours de composition.
public sealed record SetTemtemTeamMemberTechniquesCommand(
    long TeamId,
    long MemberId,
    IReadOnlyList<int> TechniqueIds);
