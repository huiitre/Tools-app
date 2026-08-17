namespace Tools.ApiCore.Modules.Notifications.Application.Views;

// Une notification telle que le frontend l'affiche. `Read` porte l'état de lecture du
// destinataire, pas celui de la notification : la même notification est lue par l'un et pas
// par l'autre.
//
// `Type` sort en code (`INFO`, `SUCCESS`, …) et non en `NotificationType` : sérialisée telle
// quelle, l'énumération partirait en entier, là où le frontend et l'API Java attendent le code.
public sealed record NotificationView(
    long Id,
    string Title,
    string Body,
    string Type,
    string? Metadata,
    DateTime CreatedAt,
    bool Read
);
