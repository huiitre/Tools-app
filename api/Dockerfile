# --- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Tools.Api.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish --configuration Release --output /app/publish --no-restore

# --- Runtime stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Lecture des APK (Core/AndroidApps, Core/Huihuistore) : aapt2 et apksigner, les outils officiels du
# SDK Android. apksigner est un script Java, d'où le JRE headless. Seuls ces deux outils et leurs
# bibliothèques sont gardés, pas tout le dossier build-tools. Chemin lu par AndroidApps:BuildToolsPath.
ARG ANDROID_BUILD_TOOLS_ZIP=build-tools_r34-linux.zip
RUN apt-get update \
    && apt-get install --yes --no-install-recommends openjdk-17-jre-headless unzip \
    && curl --fail --silent --show-error --location \
        "https://dl.google.com/android/repository/${ANDROID_BUILD_TOOLS_ZIP}" --output /tmp/build-tools.zip \
    && unzip -q /tmp/build-tools.zip -d /tmp/build-tools \
    && mkdir -p /opt/android-build-tools \
    && cd /tmp/build-tools/* \
    && cp -r aapt2 apksigner lib lib64 /opt/android-build-tools/ \
    && chmod +x /opt/android-build-tools/aapt2 /opt/android-build-tools/apksigner \
    && apt-get purge --yes unzip \
    && rm -rf /tmp/build-tools /tmp/build-tools.zip /var/lib/apt/lists/*

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

ENTRYPOINT ["dotnet", "Tools.Api.dll"]
