# Paramètres — Mapping Sidebar (version validée)

## 1. Compte
- Profil  
- Sécurité  
- Comptes liés  

> Identité utilisateur, sécurité, providers

---

## 2. Préférences
- Général  
- Apparence  
- Notifications  
- Paramètres (Core)

> Réglages transverses + Tools Core  
> Aucun réglage métier spécifique ici

---

## 3. Modules
> Uniquement les modules **ayant de vrais paramètres utilisateur**

- Dofus  
- Todolist  
- [Autres modules…]

> Chaque entrée contient **uniquement** les paramètres propres au module  
> Pas d’identité, pas de préférences globales

---

## 4. Actions (à part)
- Déconnexion  
- Suppression du compte  

> Actions destructives / sensibles  
> Jamais mélangées avec les réglages

---

## Règles structurelles
- Mobile :
  - pas de sidebar visible
  - liste des sections → écran dédié
- Desktop :
  - sidebar fixe
  - contenu central dynamique
- Un module sans paramètres utilisateur :
  - n’apparaît pas
- Le Core :
  - toujours dans Préférences
  - jamais dans Modules

settings/
  SettingsView.vue
  SettingsNav.vue

  sections/
    AccountProfileSection.vue
    AccountSecuritySection.vue
    AccountProvidersSection.vue

    PrefsGeneralSection.vue
    PrefsAppearanceSection.vue
    PrefsNotificationsSection.vue
    PrefsCoreSection.vue

    ModuleDofusSection.vue
    ModuleTodolistSection.vue

  components/
    SettingsConfigList.vue
    SettingsConfigItem.vue

    fields/
      ConfigFieldText.vue
      ConfigFieldNumber.vue
      ConfigFieldFloat.vue
      ConfigFieldBoolean.vue
      ConfigFieldSelect.vue


Section
  → SettingsConfigList
      → SettingsConfigItem
          → ConfigFieldX