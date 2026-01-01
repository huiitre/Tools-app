#!/bin/sh
set -e

LOG_FILE=/tmp/flyway.log

flyway migrate 2>&1 | tee "$LOG_FILE"
STATUS=${PIPESTATUS:-${?}}

if [ "$STATUS" -ne 0 ]; then
  BODY=$(sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g; s/$/<br\/>/' "$LOG_FILE")

  {
    echo "From: ${MAIL_FROM}"
    echo "To: ${MAIL_TO}"
    echo "Subject: [FLYWAY][QA] Migration FAILED"
    echo "MIME-Version: 1.0"
    echo "Content-Type: text/html; charset=UTF-8"
    echo
    echo "<h2>Migration Flyway échouée</h2><pre>$BODY</pre>"
  } | sendmail -t

  exit 1
fi
