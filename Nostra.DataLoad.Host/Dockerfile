FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Nostra.DataLoad.Host/Nostra.DataLoad.Host.csproj", "Nostra.DataLoad.Host/"]
COPY ["Nostra.DataLoad.Core/Nostra.DataLoad.Core.csproj", "Nostra.DataLoad.Core/"]
COPY ["Nostra.DataLoad.Domain/Nostra.DataLoad.Domain.csproj", "Nostra.DataLoad.Domain/"]
COPY ["Nostra.DataLoad.APIClient/Nostra.DataLoad.APIClient.csproj", "Nostra.DataLoad.APIClient/"]
COPY ["Nostra.DataLoad.AutotaskAPIClient/Nostra.DataLoad.AutotaskAPIClient.csproj", "Nostra.DataLoad.AutotaskAPIClient/"]
COPY ["Nostra.DataLoad.Cin7APIClient/Nostra.DataLoad.Cin7APIClient.csproj", "Nostra.DataLoad.Cin7APIClient/"]
RUN dotnet restore "Nostra.DataLoad.Host/Nostra.DataLoad.Host.csproj"
COPY . .
WORKDIR "/src/Nostra.DataLoad.Host"
RUN dotnet build "Nostra.DataLoad.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nostra.DataLoad.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nostra.DataLoad.Host.dll"]