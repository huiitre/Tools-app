# Spécification Technique du Proxy MitM (Linux)

## 1. Architecture d'Interception

Le proxy utilise une interception transparente basée sur `iptables` (NAT) pour rediriger le trafic du client Dofus Retro vers le service local.

### Stratégie de Redirection
- **Cible Automatique** : Redirection des plages IP AWS eu-west-1 utilisées par Ankama (`18.200.0.0/15`, `34.240.0.0/12`).
- **Mode Manuel** : Possibilité de forcer une IP spécifique via l'interface.
- **Port de Login** : `443` (TCP).
- **Port Local Proxy** : `5555`.

### Anti-Boucle (Loop Prevention)
Pour éviter que le proxy n'intercepte sa propre connexion sortante vers Ankama :
1. **Marquage par UID** : Seul le trafic appartenant à l'UID de l'utilisateur actuel est redirigé.
2. **Exclusion par Port Source** : Le proxy utilise explicitement la plage de ports sources `60000-60100`. Une règle `iptables` avec la cible `RETURN` est placée avant la règle de redirection pour laisser passer ce trafic.

## 2. Flux de Connexion

1. **Phase de Login** :
   - Le client tente une connexion sur le port 443 d'une IP Ankama.
   - `iptables` redirige vers `127.0.0.1:5555`.
   - `ProxyService` crée une `ProxySession` vers l'IP d'origine.
2. **Transition Jeu (Paquet AYK)** :
   - Le proxy intercepte le paquet `AYK[IP];[PORT];[TICKET]`.
   - Il démarre un nouveau serveur TCP local sur un port dynamique (5556+).
   - Il injecte un paquet `AYK` modifié vers le client : `AYK127.0.0.1;[NOUVEAU_PORT];[TICKET]`.
3. **Phase de Jeu** :
   - Le client se connecte à `127.0.0.1:[NOUVEAU_PORT]`.
   - Le proxy fait le pont avec le serveur de jeu réel.

## 3. Sécurité et Cycle de Vie

- **Privilèges** : L'utilisation de `pkexec` est requise pour manipuler `iptables`. Les commandes sont regroupées pour minimiser les demandes de mot de passe.
- **Nettoyage (Garbage Collection)** : 
  - Au démarrage et à l'arrêt, le `TrafficRedirector` purge toutes les règles `nat OUTPUT` associées à l'UID de l'utilisateur pour garantir l'intégrité du système réseau.
- **Isolation** : Le filtrage par UID garantit que les autres utilisateurs ou services système ne sont pas impactés par la redirection.

## 4. Limitations actuelles

- **IPv6** : L'interception est limitée à l'IPv4. Si le client privilégie l'IPv6, les règles `iptables` sont ignorées.
- **Plateforme** : Support complet uniquement sur Linux (Fedora testé). Windows utilise `netsh` avec des capacités limitées (pas d'interception transparente).
