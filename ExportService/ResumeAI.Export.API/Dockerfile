# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ExportService/ResumeAI.Export.API/ResumeAI.Export.API.csproj", "ExportService/ResumeAI.Export.API/"]
RUN dotnet restore "ExportService/ResumeAI.Export.API/ResumeAI.Export.API.csproj"

COPY . .
WORKDIR "/src/ExportService/ResumeAI.Export.API"
RUN dotnet build "ResumeAI.Export.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Export.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Export.API.dll"]
