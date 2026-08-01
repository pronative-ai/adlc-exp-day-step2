FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OuterloopLabApi.sln ./
COPY OuterloopLabApi/ ./OuterloopLabApi/
COPY Tests/ ./Tests/
RUN dotnet restore OuterloopLabApi.sln
RUN dotnet publish OuterloopLabApi.sln -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 3000
ENTRYPOINT ["dotnet", "OuterloopLabApi.dll"]
