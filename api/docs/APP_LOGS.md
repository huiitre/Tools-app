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
| `ip_address` | adresse IP, nullable |
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

### Limite actuelle à traiter avant l'étape 3

`AppLogService.Log` retourne actuellement la `Task` de l'insertion PostgreSQL. Un appelant qui fait
`await` attend donc l'écriture du log avant de terminer son traitement HTTP.

Le besoin exprimé est que l'écriture du journal ne ralentisse pas chaque appel. Il faut donc choisir
et valider un vrai mécanisme asynchrone avant d'instrumenter Auth. Ne pas lancer simplement la `Task`
sans l'attendre : cela rendrait les erreurs invisibles et ne garantirait pas que l'écriture termine.

Options à arbitrer lors de la reprise :

- une file en mémoire avec un worker en arrière-plan, rapide mais susceptible de perdre les éléments
  encore en mémoire si le processus s'arrête brutalement ;
- une file durable ou un mécanisme d'outbox, plus fiable mais plus complexe et impliquant toujours
  une écriture durable sur le chemin de la requête.

Aucune de ces options n'est choisie ou implémentée actuellement.

## Étape 3 — événements Auth : non commencée

Aucun contrôleur, use case ou repository du module Auth n'écrit actuellement dans le journal
applicatif.

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

Les noms définitifs des `action_code` et la manière de capter proprement les succès et les refus
doivent être validés avec l'architecture asynchrone avant de coder.

## Étapes suivantes

Après l'instrumentation Auth :

1. ajouter un use case et une route d'administration en lecture seule avec filtres et pagination ;
2. ajouter cette route à la collection Bruno ;
3. afficher le journal et ses filtres dans l'administration Web ;
4. étendre progressivement le journal aux autres modules.
