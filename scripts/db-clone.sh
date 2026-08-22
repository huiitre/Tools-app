#!/usr/bin/env bash
# Rapatrie l'état d'une base du NAS dans la base locale.
#
#   npm run db:clone -- prod
#   npm run db:clone -- qa
#
# Le dump est conservé dans .local/db/ : tu peux le rejouer sans re-solliciter le NAS avec
#   bash scripts/db-restore.sh < .local/db/prod.dump

source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

SOURCE="${1:-}"
case "${SOURCE}" in
    prod|qa|dev) ;;
    *)
        echo "usage: npm run db:clone -- <prod|qa|dev>" >&2
        exit 1
        ;;
esac

check_connection

DUMP_DIR="${ROOT_DIR}/.local/db"
DUMP_FILE="${DUMP_DIR}/${SOURCE}.dump"
mkdir -p "${DUMP_DIR}"

log "dump de ${SOURCE} depuis ${SSH_HOST}"
${SSH_CMD} "${REMOTE}" "${REMOTE_SCRIPTS_PATH}/db-dump.sh ${SOURCE}" > "${DUMP_FILE}"
log "dump récupéré ($(du -h "${DUMP_FILE}" | cut -f1))"

bash "$(dirname "${BASH_SOURCE[0]}")/db-restore.sh" < "${DUMP_FILE}"
