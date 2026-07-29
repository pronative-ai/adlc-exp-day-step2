FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OuterloopLabApi/OuterloopLabApi/OuterloopLabApi.csproj OuterloopLabApi/OuterloopLabApi/
RUN dotnet restore OuterloopLabApi/OuterloopLabApi/OuterloopLabApi.csproj

COPY OuterloopLabApi/ OuterloopLabApi/
RUN dotnet publish OuterloopLabApi/OuterloopLabApi/OuterloopLabApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 3000
ENTRYPOINT ["dotnet", "OuterloopLabApi.dll"]
