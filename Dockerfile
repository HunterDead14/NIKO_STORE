# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Спершу копіюємо лише .csproj — щоб restore кешувався окремо від коду.
COPY src/Domain/Domain.csproj                 src/Domain/
COPY src/Application/Application.csproj       src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Api/Api.csproj                       src/Api/
RUN dotnet restore src/Api/Api.csproj

# Тепер увесь код і публікація.
COPY . .
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
