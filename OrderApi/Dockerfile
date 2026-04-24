FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["OrderApi/OrderApi.csproj", "OrderApi/"]
COPY ["OrderModels/OrderModels.csproj", "OrderModels/"]
RUN dotnet restore "OrderApi/OrderApi.csproj"

COPY . .
WORKDIR "/src/OrderApi"
RUN dotnet publish "OrderApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderApi.dll"]
