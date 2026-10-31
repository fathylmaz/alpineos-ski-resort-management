FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /source
COPY api/SkiResort.Api.csproj api/
RUN dotnet restore api/SkiResort.Api.csproj
COPY api/ api/
RUN dotnet publish api/SkiResort.Api.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
RUN mkdir -p /app/keys && chown -R $APP_UID:$APP_UID /app/keys
USER $APP_UID
ENTRYPOINT ["dotnet", "SkiResort.Api.dll"]
