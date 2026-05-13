# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TemplateService/ResumeAI.Template.API/ResumeAI.Template.API.csproj", "TemplateService/ResumeAI.Template.API/"]
RUN dotnet restore "TemplateService/ResumeAI.Template.API/ResumeAI.Template.API.csproj"

COPY . .
WORKDIR "/src/TemplateService/ResumeAI.Template.API"
RUN dotnet build "ResumeAI.Template.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Template.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Template.API.dll"]
