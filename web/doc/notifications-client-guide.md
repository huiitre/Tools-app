# Documentation Client : Module Notifications (Frontend)

Guide d'utilisation et d'intégration du système de notifications dans l'application Vue.js.

## 1. Architecture du Store

Le store Pinia `useNotificationStore` est le point d'entrée unique. Il gère :
- Le chargement de l'historique au démarrage.
- La connexion automatique au flux SSE.
- La mise à jour réactive du badge et de la liste.

## 2. Éviter les conflits de nommage

**Attention** : L'API standard du navigateur s'appelle déjà `Notification`. 
Pour le métier, utilisez exclusivement l'interface **`AppNotification`**.

```typescript
import { AppNotification } from '@/modules/Core/Notification/domain/notification.types';
```

## 3. Transport (SSE)

La connexion est abstraite via `SseNotificationTransport`.
- **Auto-reconnexion** : Gérée nativement par le navigateur.
- **Point de montage** : Le store écoute les changements d'état d'authentification pour ouvrir/fermer le flux.

## 4. Intégration UI

### Composant NotificationButton
Placé dans le header, il affiche :
- Un badge avec le nombre de messages non lus.
- Un point rouge en cas de déconnexion du flux SSE.
- Une liste scrollable avec actions batch (Tout lire / Tout supprimer).

### Notifications Système (OS)
Si l'application est en arrière-plan (onglet caché), le store tente de déclencher une notification système via `window.Notification`. 
*Note : Nécessite que l'utilisateur ait autorisé les notifications sur le domaine.*

## 5. Actions Batch

Les méthodes `markAsRead([ids])` et `remove([ids])` sont optimisées :
- Appelez-les avec un tableau d'IDs pour des actions ciblées.
- Appelez-les **sans paramètres** pour des actions globales (Tout lire / Tout supprimer).
