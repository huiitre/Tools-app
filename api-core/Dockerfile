# --- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Tools.ApiCore.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish --configuration Release --output /app/publish --no-restore

# --- Runtime stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ARG APPLICATION_VERSION=0.1.0
ARG GIT_SHA=unknown

LABEL org.opencontainers.image.version=${APPLICATION_VERSION}
LABEL org.opencontainers.image.revision=${GIT_SHA}

ENV ASPNETCORE_URLS=http://+:8080
ENV Application__Version=${APPLICATION_VERSION}
ENV Application__GitSha=${GIT_SHA}

COPY --from=build /app/publish ./

USER $APP_UID

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Tools.ApiCore.dll"]
