// Miroirs des vues de l'API Core (Core/AndroidApps). Empreintes en hexadécimal minuscule.

export type AndroidAppRelease = {
  id: number
  versionCode: number
  versionName: string | null
  minSdk: number
  url: string
  fileSize: number
  fileSha256: string
  certSha256: string
  releaseNotes: string | null
  uploadedAt: string
  uploadedBy: number
  uploadedByName: string | null
  // Celle proposée aux TV.
  isRecommended: boolean
}

export type AndroidApp = {
  id: number
  packageName: string
  label: string
  displayOrder: number
  isActive: boolean
  recommendedReleaseId: number | null
  createdAt: string
  // versionCode décroissant.
  releases: AndroidAppRelease[]
}

export type ApkInfo = {
  packageName: string
  versionCode: number
  versionName: string | null
  minSdk: number
  label: string | null
  fileSize: number
  fileSha256: string
  certSha256: string
  certSubject: string
  signatureSchemes: string[]
}

export type AndroidAppImportWarningCode =
  | 'NEW_APP'
  | 'CERTIFICATE_CHANGED'
  | 'OLDER_THAN_RECOMMENDED'
  | 'MIN_SDK_RAISED'

export type AndroidAppImportWarning = {
  code: AndroidAppImportWarningCode
  message: string
}

export type AndroidAppImportExistingApp = {
  id: number
  label: string
  recommendedVersionCode: number | null
  recommendedVersionName: string | null
  recommendedMinSdk: number | null
  recommendedFileSize: number | null
  recommendedCertSha256: string | null
}

// Réponse du premier temps de l'import : rien n'est encore créé.
export type AndroidAppImportAnalysis = {
  importId: string
  expiresAt: string
  apk: ApkInfo
  existingApp: AndroidAppImportExistingApp | null
  suggestedLabel: string
  warnings: AndroidAppImportWarning[]
}

export type ConfirmAndroidAppImportPayload = {
  // Ne sert qu'à une nouvelle app.
  label: string | null
  recommend: boolean
  releaseNotes: string | null
}

// Champ absent = inchangé.
export type UpdateAndroidAppPayload = {
  label?: string
  displayOrder?: number
  isActive?: boolean
  recommendedReleaseId?: number
}

// Mêmes bornes que l'API : les refuser ici évite d'envoyer 100 Mo pour rien.
export const ANDROID_APP_LABEL_MAX_LENGTH = 100
export const ANDROID_APP_RELEASE_NOTES_MAX_LENGTH = 2000
export const ANDROID_APK_MAX_BYTES = 200 * 1024 * 1024

// Niveaux d'API Android courants, pour lire un minSdk.
export const ANDROID_VERSION_BY_SDK: Record<number, string> = {
  21: '5.0', 22: '5.1', 23: '6', 24: '7.0', 25: '7.1', 26: '8.0', 27: '8.1', 28: '9',
  29: '10', 30: '11', 31: '12', 32: '12L', 33: '13', 34: '14', 35: '15', 36: '16',
}

// Grilles des tableaux : l'en-tête et les lignes partagent la même définition pour rester alignés.
// Colonnes des apps : chevron, nom, paquet, version sur le store, Android minimum, taille, nombre de
// versions, dernier import, actif, actions.
export const ANDROID_APPS_GRID =
  '24px minmax(140px, 1.2fr) minmax(170px, 1.5fr) minmax(120px, 1fr) 120px 80px 70px 150px 56px 64px'

// Colonnes des versions : version, Android minimum, taille, certificat, import, notes, statut,
// actions.
export const ANDROID_APP_RELEASES_GRID =
  'minmax(150px, 1.2fr) 120px 80px minmax(130px, 1fr) minmax(170px, 1.2fr) minmax(0, 1.4fr) 130px 64px'
