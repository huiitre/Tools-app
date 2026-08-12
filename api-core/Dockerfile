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

ENTRYPOINT ["dotnet", "Tools.ApiCore.dll"]
