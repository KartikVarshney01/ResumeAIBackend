# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["Auth_Api/ResumeAI.Auth.API/ResumeAI.Auth.API.csproj", "Auth_Api/ResumeAI.Auth.API/"]
RUN dotnet restore "Auth_Api/ResumeAI.Auth.API/ResumeAI.Auth.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Auth_Api/ResumeAI.Auth.API"
RUN dotnet build "ResumeAI.Auth.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "ResumeAI.Auth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Auth.API.dll"]
