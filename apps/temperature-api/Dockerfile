FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY temperature-api.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "temperature-api.dll"]