FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore src/PRI.Api/PRI.Api.csproj
RUN dotnet publish src/PRI.Api/PRI.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
RUN mkdir -p /var/lib/recoup \
    && chown -R "$APP_UID:0" /var/lib/recoup

ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
USER $APP_UID

CMD ["sh", "-c", "exec dotnet PRI.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]
