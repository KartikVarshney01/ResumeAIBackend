# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ResumeAI/ResumeAI.Resume.API/ResumeAI.Resume.API.csproj", "ResumeAI/ResumeAI.Resume.API/"]
RUN dotnet restore "ResumeAI/ResumeAI.Resume.API/ResumeAI.Resume.API.csproj"

COPY . .
WORKDIR "/src/ResumeAI/ResumeAI.Resume.API"
RUN dotnet build "ResumeAI.Resume.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Resume.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Resume.API.dll"]
