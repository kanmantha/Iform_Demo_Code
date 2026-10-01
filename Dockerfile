# Render has no native .NET runtime, so this app must ship as a Docker image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so layer caching survives source-only changes.
COPY IForm.Web/IForm.Web.csproj IForm.Web/
RUN dotnet restore IForm.Web/IForm.Web.csproj

COPY IForm.Web/ IForm.Web/
RUN dotnet publish IForm.Web -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render injects PORT and expects the app on 0.0.0.0. Program.cs reads this.
# The connection string is supplied as a Render secret; this SQLite value is only
# a fallback so the image still boots if the secret is missing.
ENV PORT=10000 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/iform.db"

EXPOSE 10000
# SQLite creates the file but not the parent directory, so make it up front.
RUN mkdir -p /app/data
ENTRYPOINT ["dotnet", "IForm.Web.dll"]
