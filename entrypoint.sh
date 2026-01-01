#!/bin/sh

LOG_FILE=/tmp/flyway.log

if ! flyway migrate 2>&1 | tee "$LOG_FILE"; then
  BODY=$(sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g; s/$/<br\/>/' "$LOG_FILE")

  {
    echo "From: ${MAIL_FROM}"
    echo "To: ${MAIL_TO}"
    echo "Subject: [FLYWAY][QA] Migration FAILED"
    echo "MIME-Version: 1.0"
    echo "Content-Type: text/html; charset=UTF-8"
    echo
    echo "<h2>Migration Flyway échouée</h2><pre>${BODY}</pre>"
  } | msmtp \
        --host="${SMTP_HOST}" \
        --port="${SMTP_PORT}" \
        --auth=on \
        --user="${SMTP_USER}" \
        --passwordeval="echo ${SMTP_PASSWORD}" \
        --tls=on \
        --tls-trust-file=/etc/ssl/certs/ca-certificates.crt \
        "${MAIL_TO}"

  exit 1
fi
