FROM flyway/flyway:10

USER root

RUN apt-get update \
 && apt-get install -y sendmail \
 && rm -rf /var/lib/apt/lists/*

COPY sql /flyway/sql
COPY entrypoint.sh /entrypoint.sh

RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]