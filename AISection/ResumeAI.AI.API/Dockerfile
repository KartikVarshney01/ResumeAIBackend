# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AISection/ResumeAI.AI.API/ResumeAI.AI.API.csproj", "AISection/ResumeAI.AI.API/"]
RUN dotnet restore "AISection/ResumeAI.AI.API/ResumeAI.AI.API.csproj"

COPY . .
WORKDIR "/src/AISection/ResumeAI.AI.API"
RUN dotnet build "ResumeAI.AI.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.AI.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.AI.API.dll"]
