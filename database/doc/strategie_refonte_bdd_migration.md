# Refonte base de données – Stratégie de migration avec conservation des données

Ce document décrit une stratégie **simple, maîtrisée et sûre** pour refondre complètement le schéma de base de données en production **tout en conservant certaines données utilisateur**.

---

## Objectif

- Réinitialiser entièrement les schémas existants (refonte propre)
- Conserver certaines données critiques (ex : santé, statistiques, sets…)
- Éviter toute dépendance aux IDs techniques
- Rester compatible avec Flyway

---

## Principe clé

> **Toute migration de données se fait via une clé métier stable (email)**  
> **Jamais via un ID technique.**

---

## Étape 1 — Sauvegarde ciblée avant reset

Dans une migration Flyway dédiée (ex: `V100__backup_before_reset.sql`) :

- Créer un schéma temporaire :
```sql
CREATE SCHEMA IF NOT EXISTS schema_backup;
```

- Sauvegarder les données à conserver **en incluant l’email utilisateur** :
```sql
CREATE TABLE schema_backup.health_backup AS
SELECT
    u.email AS user_email,
    h.weight,
    h.measured_at
FROM tools_health.weight_entries h
JOIN tools_core.users u ON u.id = h.user_id;
```

(Répéter le principe pour chaque table critique)

---

## Étape 2 — Reset complet des schémas

Toujours dans la même migration :

```sql
DROP SCHEMA IF EXISTS tools_core CASCADE;
DROP SCHEMA IF EXISTS tools_health CASCADE;
DROP SCHEMA IF EXISTS tools_dofus CASCADE;
-- autres schémas si nécessaire
```

Puis recréation propre :

```sql
CREATE SCHEMA tools_core;
CREATE SCHEMA tools_health;
-- création des nouvelles tables propres
```

---

## Étape 3 — Déploiement et réinscription

- L’application redémarre sur une base propre
- Les utilisateurs se réinscrivent normalement
- Les nouveaux IDs utilisateurs sont générés

Aucune donnée sauvegardée n’est encore réinjectée à ce stade.

---

## Étape 4 — Réinjection des données sauvegardées

Dans une **migration Flyway suivante** (ex: `V101__restore_user_data.sql`) :

```sql
INSERT INTO tools_health.weight_entries (
    user_id,
    weight,
    measured_at
)
SELECT
    u.id,
    b.weight,
    b.measured_at
FROM schema_backup.health_backup b
JOIN tools_core.users u ON u.email = b.user_email;
```

Le mapping se fait automatiquement grâce à l’email.

---

## Étape 5 — Nettoyage

Une fois la migration validée :

```sql
DROP SCHEMA schema_backup CASCADE;
```

---

## Avantages de cette approche

- ✅ aucune perte de données critiques
- ✅ pas de hardcode d’ID
- ✅ migrations rejouables
- ✅ compatible Flyway
- ✅ stratégie claire et documentée

---

## Conclusion

Cette méthode permet une **refonte complète et assumée** de la base de données,  
sans compromis sur la sécurité des données importantes.

> **Brutal sur le schéma, propre sur les données.**
