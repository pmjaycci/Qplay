FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY bin/Release/net6.0/publish .
ENTRYPOINT ["dotnet", "GameServer.dll"]
#dotnet publish -c Release