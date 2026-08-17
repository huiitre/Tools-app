# Documentation Technique : Système de Notifications (Backend)

Ce module fournit un mécanisme de notifications temps réel (SSE) et asynchrone pour l'ensemble de l'application.

## 1. Fonctionnement Global

Le système repose sur un bus d'événements interne (**Spring ApplicationEvents**).

1.  **Déclenchement** : Un module (ex: Valorant) publie un `NotificationEvent`.
2.  **Traitement (Asynchrone)** : Le `NotificationEventListener` attrape l'événement.
3.  **Persistence** : Une ligne est créée dans `notifications` (source) et une ligne par destinataire dans `user_notifications`.
4.  **Distribution** : Le `SseNotificationService` pousse le message aux utilisateurs connectés.

## 2. Usage Manuel (API)

Pour envoyer une notification via Postman ou un service externe :
**Route** : `POST /api/v3/notifications`
**Rôle requis** : `TECH` minimum.

### Payload
```json
{
  "title": "Titre du message",
  "body": "Contenu de la notification",
  "type": "INFO", // SUCCESS, WARNING, ERROR, INFO
  "targetUserId": null,   // Cible un utilisateur précis
  "targetRoleId": null,   // Cible tous les membres d'un rôle (ID)
  "targetModuleId": null, // Cible tous les membres d'un module (ID)
  "metadata": "{\"route\": \"valorant-shop\"}" // JSON optionnel pour le front
}
```
*Note : Si tous les `target` sont à null, la notification est envoyée à TOUT LE MONDE.*

## 3. Sécurité Spécifique

- **Bypass TECH** : Les utilisateurs possédant le rôle `TECH` (ID dynamique) sont **exclus** de tous les envois pour éviter de polluer leur propre interface.
- **SSE Auth** : Comme `EventSource` ne supporte pas les headers, le JWT est passé via le paramètre de requête `?token=...`.

## 4. Maintenance

- **Heartbeat** : Un signal est envoyé toutes les 20 secondes pour maintenir les connexions ouvertes à travers les proxies/firewalls.
- **Suppression** : La suppression est physique (`DELETE`) dans la table `user_notifications`.
