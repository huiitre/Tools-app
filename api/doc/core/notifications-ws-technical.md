# WebSocket Notifications - Spécifications Techniques

Ce document décrit l'implémentation du système de notifications temps réel via WebSockets (STOMP/SockJS).

## 1. Architecture
L'implémentation repose sur **Spring Messaging** et le protocole **STOMP**.

- **Endpoint de connexion** : `/ws`
- **Protocole de repli** : SockJS activé.
- **Broker** : Simple Broker (en mémoire).

## 2. Configuration (`WebSocketConfig.java`)
La configuration définit deux préfixes principaux :
- `/app` : Pour les messages envoyés du client vers le serveur (non utilisé actuellement pour les notifications).
- `/topic` : Pour les messages diffusés par le serveur vers les clients.

## 3. Sécurité & Authentification
Contrairement aux requêtes HTTP classiques, l'authentification ne passe pas par les headers HTTP standards (souvent bloqués ou limités par les navigateurs/proxys lors du handshake WS).

### Handshake & CONNECT
1. Le client initie la connexion sur `/ws`.
2. Une fois la socket ouverte, le client **doit** envoyer une frame STOMP `CONNECT` incluant le token JWT dans l'en-tête `Authorization`.
3. Un `ChannelInterceptor` (`WebSocketChannelInterceptor`) intercepte cette frame.

### Validation (`WebSocketChannelInterceptor`)
- **Action** : Intercepte les frames de type `CONNECT`.
- **Validation** : Extrait le header `Authorization`, valide le JWT via le `JwtProvider`.
- **Injection** : Si valide, injecte l'utilisateur dans le contexte de la session WebSocket.
- **Refus** : Si le token est absent ou expiré, la connexion est immédiatement fermée (Log : `WebSocket auth failed`).

## 4. Flux de Diffusion
Les notifications sont envoyées via `SimpMessagingTemplate`.

### Destination
Chaque utilisateur écoute sur un topic privé :
` /topic/notifications-{userId}`

### Payload
Le format envoyé est un objet JSON représentant la notification (Title, Body, Type, Metadata).

## 5. Cas spécifiques : Electron & Production
- **CORS** : Les origins sont restreintes aux domaines autorisés et au pattern `app://.` pour Electron.
- **Nginx** : En production, Nginx doit être configuré pour supporter l'upgrade de protocole :
```nginx
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection "upgrade";
```

## 6. Debugging
Les logs de connexion et de refus d'authentification sont disponibles via le logger :
`fr.huiitre.tools.modules.core.security.infrastructure.interceptors.WebSocketChannelInterceptor`
