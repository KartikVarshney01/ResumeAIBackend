# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NotificationService/ResumeAI.Notification.API/ResumeAI.Notification.API.csproj", "NotificationService/ResumeAI.Notification.API/"]
RUN dotnet restore "NotificationService/ResumeAI.Notification.API/ResumeAI.Notification.API.csproj"

COPY . .
WORKDIR "/src/NotificationService/ResumeAI.Notification.API"
RUN dotnet build "ResumeAI.Notification.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Notification.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Notification.API.dll"]
