# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["PaymentService/ResumeAI.Payment.API/ResumeAI.Payment.API.csproj", "PaymentService/ResumeAI.Payment.API/"]
RUN dotnet restore "PaymentService/ResumeAI.Payment.API/ResumeAI.Payment.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/PaymentService/ResumeAI.Payment.API"
RUN dotnet build "ResumeAI.Payment.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "ResumeAI.Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Payment.API.dll"]
