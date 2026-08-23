#!/usr/bin/env bash
# Chargé par les scripts de synchronisation : lit .env et valide l'accès SSH.

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT_DIR}/.env"

if [[ -f "${ENV_FILE}" ]]; then
    set -a
    # shellcheck disable=SC1090
    source "${ENV_FILE}"
    set +a
fi

: "${SSH_HOST:?SSH_HOST manquant — renseigne-le dans .env (voir .env.example)}"
: "${SSH_USER:?SSH_USER manquant — renseigne-le dans .env (voir .env.example)}"
SSH_PORT="${SSH_PORT:-22}"

REMOTE="${SSH_USER}@${SSH_HOST}"
SSH_CMD="ssh -p ${SSH_PORT} -o BatchMode=yes -o ConnectTimeout=10"

# Emplacements sur le NAS. Surchargeables depuis .env si l'arborescence bouge.
REMOTE_ASSETS_PATH="${REMOTE_ASSETS_PATH:-/data/docker/tools/tools_assets}"
REMOTE_SNAPSHOTS_PATH="${REMOTE_SNAPSHOTS_PATH:-/data/docker/tools/tools_palworld_server_data_extractor/data/json}"

# Emplacements locaux. Par défaut ceux que lisent les API en dev
# (tools.assets.base-path et palworld.server-data.path).
LOCAL_ASSETS_PATH="${LOCAL_ASSETS_PATH:-/data/docker/tools/tools_assets}"
LOCAL_SNAPSHOTS_PATH="${LOCAL_SNAPSHOTS_PATH:-/data/docker/tools/tools_palworld_server_data_extractor/data/json}"

# La barre de progression n'a de sens que dans un terminal : redirigée dans un fichier ou un
# pipe, elle produit des mégaoctets de retours chariot. Hors TTY on ne garde que le résumé.
if [[ -t 1 ]]; then
    RSYNC_PROGRESS=(--info=progress2 -h)
else
    RSYNC_PROGRESS=(--stats -h)
fi

# Scripts résidents sur le NAS (db-dump.sh / db-restore.sh) et conteneur Postgres local.
REMOTE_SCRIPTS_PATH="${REMOTE_SCRIPTS_PATH:-/data/docker/tools/scripts}"
PG_CONTAINER="${PG_CONTAINER:-tools_postgres_dev}"

log() {
    printf '\033[36m[sync]\033[0m %s\n' "$*"
}

check_connection() {
    if ! ${SSH_CMD} "${REMOTE}" true 2>/dev/null; then
        echo "Connexion SSH impossible vers ${SSH_HOST}:${SSH_PORT}." >&2
        echo "Vérifie .env, et que ta clé est chargée (ssh-add -l)." >&2
        exit 1
    fi
}
