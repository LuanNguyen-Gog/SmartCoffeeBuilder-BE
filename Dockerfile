FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["SmartCoffeeBuilder.API/SmartCoffeeBuilder.API.csproj", "SmartCoffeeBuilder.API/"]
COPY ["SmartCoffeeBuilder.Service/SmartCoffeeBuilder.Service.csproj", "SmartCoffeeBuilder.Service/"]
COPY ["SmartCoffeeBuilder.Repository/SmartCoffeeBuilder.Repository.csproj", "SmartCoffeeBuilder.Repository/"]
RUN dotnet restore "SmartCoffeeBuilder.API/SmartCoffeeBuilder.API.csproj"

COPY . .
WORKDIR "/src/SmartCoffeeBuilder.API"
RUN dotnet build "SmartCoffeeBuilder.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SmartCoffeeBuilder.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartCoffeeBuilder.API.dll"]
