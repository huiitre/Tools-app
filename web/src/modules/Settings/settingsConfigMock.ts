// settingsConfigMock.ts

export type ConfigValueType =
  | 'TEXT'
  | 'BOOLEAN'
  | 'NUMBER'
  | 'FLOAT'
  | 'SELECT'

export interface ConfigMock {
  id: number
  code: string
  module_id: number | null
  value_type: ConfigValueType
  value: string
  required_role_id: number | null
  is_active: boolean

  // --- UI uniquement ---
  ui: {
    section:
      | 'prefs-general'
      | 'prefs-appearance'
      | 'prefs-notifications'
      | 'prefs-core'
      | 'module-dofus'
      | 'module-todolist'
    label: string
    description?: string
  }
}

export const CONFIGS_MOCK: ConfigMock[] = [

  /* =========================
   * PRÉFÉRENCES — GÉNÉRAL
   * module_id = null
   * ========================= */
  {
    id: 1,
    code: 'language',
    module_id: null,
    value_type: 'SELECT',
    value: 'fr',
    required_role_id: null,
    is_active: true,
    ui: {
      section: 'prefs-general',
      label: 'Langue',
      description: 'Langue de l’interface'
    }
  },

  {
    id: 2,
    code: 'timezone',
    module_id: null,
    value_type: 'TEXT',
    value: 'Europe/Paris',
    required_role_id: null,
    is_active: true,
    ui: {
      section: 'prefs-general',
      label: 'Fuseau horaire'
    }
  },

  /* =========================
   * PRÉFÉRENCES — APPARENCE
   * ========================= */
  {
    id: 3,
    code: 'theme',
    module_id: null,
    value_type: 'SELECT',
    value: 'dark',
    required_role_id: null,
    is_active: true,
    ui: {
      section: 'prefs-appearance',
      label: 'Thème',
      description: 'Clair ou sombre'
    }
  },

  {
    id: 4,
    code: 'compact_mode',
    module_id: null,
    value_type: 'BOOLEAN',
    value: 'false',
    required_role_id: null,
    is_active: true,
    ui: {
      section: 'prefs-appearance',
      label: 'Mode compact'
    }
  },

  /* =========================
   * PRÉFÉRENCES — NOTIFICATIONS
   * ========================= */
  {
    id: 5,
    code: 'notifications_email',
    module_id: null,
    value_type: 'BOOLEAN',
    value: 'true',
    required_role_id: null,
    is_active: true,
    ui: {
      section: 'prefs-notifications',
      label: 'Notifications email'
    }
  },

  {
    id: 6,
    code: 'notifications_push',
    module_id: null,
    value_type: 'BOOLEAN',
    value: 'false',
    required_role_id: null,
    is_active: true,
    ui: {
      section: 'prefs-notifications',
      label: 'Notifications push'
    }
  },

  /* =========================
   * CORE (TOOLS)
   * ========================= */
  {
    id: 7,
    code: 'default_page_size',
    module_id: null,
    value_type: 'NUMBER',
    value: '25',
    required_role_id: 2, // ex: ROLE_ADMIN
    is_active: true,
    ui: {
      section: 'prefs-core',
      label: 'Taille de page par défaut'
    }
  },

  /* =========================
   * MODULE — DOFUS
   * module_id = 1
   * ========================= */
  {
    id: 10,
    code: 'dofus_default_server',
    module_id: 1,
    value_type: 'TEXT',
    value: 'draconiros',
    required_role_id: null,
    is_active: true,
    ui: {
      section: 'module-dofus',
      label: 'Serveur par défaut'
    }
  },

  {
    id: 11,
    code: 'dofus_auto_sync',
    module_id: 1,
    value_type: 'BOOLEAN',
    value: 'true',
    required_role_id: null,
    is_active: true,
    ui: {
      section: 'module-dofus',
      label: 'Synchronisation automatique'
    }
  },

  /* =========================
   * MODULE — TODOLIST
   * module_id = 2
   * ========================= */
  {
    id: 20,
    code: 'todolist_default_sort',
    module_id: 2,
    value_type: 'SELECT',
    value: 'due_date',
    required_role_id: null,
    is_active: true,
    ui: {
      section: 'module-todolist',
      label: 'Tri par défaut'
    }
  },

  /* =========================
   * MODULE — VIDE (EXEMPLE)
   * module_id = 3
   * ========================= */
  // Aucun paramètre pour ce module
]