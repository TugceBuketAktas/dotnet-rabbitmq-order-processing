FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["OrderWorker/OrderWorker.csproj", "OrderWorker/"]
COPY ["OrderModels/OrderModels.csproj", "OrderModels/"]
RUN dotnet restore "OrderWorker/OrderWorker.csproj"

COPY . .
WORKDIR "/src/OrderWorker"
RUN dotnet publish "OrderWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OrderWorker.dll"]
