FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Todo.Migrations/Todo.Migrations.csproj", "src/Todo.Migrations/"]
COPY ["src/Todo.ServiceDefaults/Todo.ServiceDefaults.csproj", "src/Todo.ServiceDefaults/"]
RUN dotnet restore "src/Todo.Migrations/Todo.Migrations.csproj"
COPY . .
WORKDIR "/src/src/Todo.Migrations"
RUN dotnet build "./Todo.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Todo.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Todo.Migrations.dll"]
