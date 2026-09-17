#!/usr/bin/env bash
# Applique les migrations Flyway absentes sur le Postgres local.
#
#   npm run db:migrate             # jusqu'à la dernière migration du dépôt
#   npm run db:migrate -- 2.76.0   # s'arrête à la version demandée
#
# Le clone de prod/QA transporte flyway_schema_history : Flyway ne rejoue donc que les fichiers
# absents. Sur une base locale vide, il les applique tous dans l'ordre. Ce script ne se connecte
# jamais au NAS.

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT_DIR}/.env"
CONTAINER="${PG_CONTAINER:-tools_postgres_dev}"
TARGET="${1:-}"

usage() {
    echo "usage: npm run db:migrate [-- <version>]" >&2
    echo "example: npm run db:migrate -- 2.76.0" >&2
}

if [[ $# -gt 1 ]] || [[ -n "${TARGET}" && ! "${TARGET}" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    usage
    exit 1
fi

if [[ ! -f "${ENV_FILE}" ]]; then
    echo ".env introuvable : impossible de lire les identifiants de la base locale." >&2
    exit 1
fi

set -a
# shellcheck disable=SC1090
source "${ENV_FILE}"
set +a

: "${DB_NAME:?DB_NAME manquant dans .env}"
: "${DB_USERNAME:?DB_USERNAME manquant dans .env}"
: "${DB_PASSWORD:?DB_PASSWORD manquant dans .env}"

if ! docker inspect "${CONTAINER}" >/dev/null 2>&1; then
    echo "Conteneur ${CONTAINER} introuvable. Lance d'abord : npm run db:up" >&2
    exit 1
fi

if [[ "$(docker inspect --format '{{.State.Running}}' "${CONTAINER}")" != "true" ]]; then
    echo "Conteneur ${CONTAINER} arrêté. Lance d'abord : npm run db:up" >&2
    exit 1
fi

FLYWAY_ARGS=(
    "-url=jdbc:postgresql://127.0.0.1:5432/${DB_NAME}"
    "-user=${DB_USERNAME}"
    "-password=${DB_PASSWORD}"
    "-connectRetries=3"
)

if [[ -n "${TARGET}" ]]; then
    FLYWAY_ARGS+=("-target=${TARGET}")
fi

docker run --rm \
    --network "container:${CONTAINER}" \
    --volume "${ROOT_DIR}/database/sql:/flyway/sql:ro" \
    flyway/flyway:10 \
    "${FLYWAY_ARGS[@]}" \
    migrate
