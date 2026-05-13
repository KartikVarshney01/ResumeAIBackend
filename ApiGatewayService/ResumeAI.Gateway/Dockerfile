# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ApiGatewayService/ResumeAI.Gateway/ResumeAI.Gateway.csproj", "ApiGatewayService/ResumeAI.Gateway/"]
RUN dotnet restore "ApiGatewayService/ResumeAI.Gateway/ResumeAI.Gateway.csproj"

COPY . .
WORKDIR "/src/ApiGatewayService/ResumeAI.Gateway"
RUN dotnet build "ResumeAI.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Gateway.dll"]
