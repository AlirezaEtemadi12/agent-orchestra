# The UI is a static export; the API serves it and reads the runs folders mounted read-only.

FROM node:22-alpine AS ui
WORKDIR /src

COPY ui/package.json ui/package-lock.json ./
RUN npm ci

COPY ui/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src

COPY api/ ./
RUN dotnet publish src/Orchestra.Api/Orchestra.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=api /app ./
COPY --from=ui /src/out ./wwwroot

ENV ASPNETCORE_URLS=http://+:3500 \
    Orchestra__DatabasePath=/data/orchestra.db

EXPOSE 3500

RUN useradd --create-home --uid 10001 orchestra \
    && mkdir -p /data \
    && chown -R orchestra:orchestra /data /app

USER orchestra

ENTRYPOINT ["dotnet", "Orchestra.Api.dll"]
