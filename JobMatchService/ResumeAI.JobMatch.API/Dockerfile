# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["JobMatchService/ResumeAI.JobMatch.API/ResumeAI.JobMatch.API.csproj", "JobMatchService/ResumeAI.JobMatch.API/"]
RUN dotnet restore "JobMatchService/ResumeAI.JobMatch.API/ResumeAI.JobMatch.API.csproj"

COPY . .
WORKDIR "/src/JobMatchService/ResumeAI.JobMatch.API"
RUN dotnet build "ResumeAI.JobMatch.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.JobMatch.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.JobMatch.API.dll"]
