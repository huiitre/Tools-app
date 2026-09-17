# Journal applicatif — état au 17 septembre 2026

Ce document décrit uniquement l'état réellement livré et les demandes connues. L'intégration du
journal dans les parcours Auth a été annulée intégralement et reste à concevoir avant tout nouveau
code.

## Objectif

Le journal applicatif doit conserver les actions fonctionnelles et les interactions utilisateur :
connexion, déconnexion, utilisation d'un module, changement métier, etc. Il est distinct des logs
techniques de fichier utilisés pour le diagnostic de l'application.

Les lignes sont immuables côté application : aucune route et aucun repository ne doivent permettre
de les modifier ou de les supprimer. Une suppression exceptionnelle sera faite manuellement en base
de données.

## Étape 1 — modèle et base de données : terminée

Les migrations suivantes existent et ont été exécutées manuellement sur la base de développement :

- `database/sql/V2.76.0__application_logs.sql` crée `tools_core.application_logs` ;
- `database/sql/V2.76.1__application_logs_historical_references.sql` retire les clés étrangères
  vers les utilisateurs et les modules.

La table contient :

| colonne | rôle |
|---|---|
| `id` | clé primaire technique `BIGSERIAL` |
| `created_at` | date de création fixée par PostgreSQL |
| `module_id` | identifiant historique du module ; `NULL` désigne le Core |
| `area_code` | zone fonctionnelle, par exemple `AUTH` |
| `action_code` | action, par exemple `LOGIN` |
| `user_id` | identifiant historique de l'utilisateur, nullable |
| `ip_address` | adresse IP, nullable, au format PostgreSQL `inet` |
| `user_agent` | user-agent HTTP, nullable |
| `metadata` | contexte libre JSONB, sans secret |

`module_id` et `user_id` ne sont volontairement plus des clés étrangères. Supprimer un utilisateur
ou un module ne supprime pas ses logs, n'efface pas les identifiants historiques et n'est pas bloqué
par le journal.

## Étape 2 — infrastructure générique d'écriture : terminée

Le module `api/Modules/Core/AppLogs/` fournit actuellement :

- `AppLogCommand`, utilisé par l'appelant pour décrire l'événement ;
- `AppLogService`, point d'entrée transverse ;
- `IAppLogContextProvider` et `HttpAppLogContextProvider`, qui ajoutent automatiquement l'IP et le
  user-agent de la requête ;
- `IAppLogRepository` et `PostgresAppLogRepository`, qui insèrent une ligne avec Dapper ;
- l'enregistrement explicite du module dans la composition de l'API ;
- des tests unitaires du service, de la normalisation des codes et du contexte HTTP.

Le service normalise `area_code` et `action_code` en majuscules et sérialise les métadonnées avec les
conventions JSON Web de .NET.

### Adresse IP du client derrière Nginx

`HttpAppLogContextProvider` lit `RemoteIpAddress`. Le pipeline applique auparavant les forwarded
headers standards : une requête passée par Nginx journalise donc le premier `X-Forwarded-For`, à
condition que l'adresse TCP de Nginx soit explicitement présente dans
`ReverseProxy:TrustedProxies`. La liste est vide par défaut, ce qui empêche toute usurpation par un
client qui enverrait lui-même cet en-tête.

Avant le test QA, renseigner l'adresse IP interne du conteneur/proxy Nginx dans la configuration QA
déployée, par exemple :

```json
"ReverseProxy": {
  "TrustedProxies": ["172.18.0.2"]
}
```

La table conserve l'adresse avec le type PostgreSQL `inet`. La lecture d'administration utilise
`host(ip_address)` : l'interface reçoit donc `2001:db8::1`, jamais `2001:db8::1/128` (et
`203.0.113.42`, jamais `/32`).

### Localisation IP locale (GeoLite2)

La lecture admin cherche pays et ville dans la base locale MaxMind GeoLite2 City ; aucune adresse
IP n'est envoyée à un service externe. Le fichier `GeoLite2-City.mmdb` doit être monté dans chaque
conteneur API à `/app/geoip/GeoLite2-City.mmdb` (chemin configurable avec `GeoIp:DatabasePath`).
Les IP locales, privées et absentes de la base ne produisent pas de localisation.

### Choix d'écriture

Les logs applicatifs ne sont pas critiques : les appelants font donc directement
`await AppLogService.Log(...)`. Une insertion PostgreSQL d'une ligne est suffisamment légère pour
ce besoin et conserve les erreurs observables. Une tâche détachée (`Task.Run`, tâche non attendue)
reste interdite : elle perdrait les erreurs et peut survivre au scope HTTP dont elle dépend.

Une file en mémoire avec worker ou une outbox ne sera envisagée que pour un flux beaucoup plus
volumineux ou nécessitant une diffusion fiable à d'autres services.

## Étape 3 — événements Auth : commencée

Le logout volontaire et les connexions réussies sont instrumentés :

- `AUTH / LOGOUT` porte l'utilisateur de l'access token lorsqu'il est encore valide. La route reste
  anonyme pour toujours supprimer le cookie même si le token est absent ou expiré ; dans ce cas,
  aucun utilisateur n'est journalisable. Le refresh token ne participe jamais à ce log ;
- `AUTH / LOGIN` porte l'utilisateur et `metadata.authenticationMethod` (`PASSWORD` ou `GOOGLE`).

Événements demandés pour la reprise :

- connexion réussie par mot de passe ;
- connexion réussie par Google ;
- tentative de connexion refusée, avec son mode de connexion ;
- déconnexion ;
- inscription réussie ou refusée par mot de passe ;
- inscription réussie ou refusée par Google ;
- confirmation d'adresse email réussie ou refusée.

Règles de données demandées :

- utiliser la colonne `user_id` lorsque l'utilisateur est connu ;
- ne pas recopier inutilement l'identité dans le JSONB lorsque `user_id` suffit ;
- placer dans `metadata` les informations sans colonne dédiée, notamment le mode d'authentification
  (`PASSWORD` ou `GOOGLE`) et, pour un refus anonyme, l'email ou le nom lorsqu'ils sont utiles ;
- ne jamais enregistrer de mot de passe, access token, refresh token, code OAuth ou jeton de
  confirmation ;
- pour le logout, utiliser directement l'utilisateur porté par l'access token disponible sur la
  requête ; il n'y a pas à relire ou analyser le refresh token uniquement pour produire le log ;
- ne pas modifier une implémentation de repository métier pour faire fonctionner le journal ;
- garder l'instrumentation courte et indépendante du chemin métier, sans ajouter des branches métier
  uniquement pour les logs.

Les refus et les autres parcours Auth restent à instrumenter ; les codes déjà livrés sont
`LOGIN` et `LOGOUT` dans la zone `AUTH`.

## Étapes suivantes

La lecture d'administration est livrée : `GET /admin/app-logs` exige ADMIN et propose filtres,
pagination et tri. La réponse inclut le JSON `metadata`, destiné à l'infobulle d'administration,
ainsi que la localisation GeoLite2 lorsque l'IP publique est connue. La route est présente dans
Bruno.

1. afficher le journal et ses filtres dans l'administration Web ;
2. étendre progressivement le journal aux autres modules.
