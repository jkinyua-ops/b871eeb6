FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Nostra.DataLoad.UI/Nostra.DataLoad.UI.csproj", "Nostra.DataLoad.UI/"]
COPY ["Nostra.DataLoad.Core/Nostra.DataLoad.Core.csproj", "Nostra.DataLoad.Core/"]
COPY ["Nostra.DataLoad.Domain/Nostra.DataLoad.Domain.csproj", "Nostra.DataLoad.Domain/"]
COPY ["Nostra.DataLoad.APIClient/Nostra.DataLoad.APIClient.csproj", "Nostra.DataLoad.APIClient/"]
RUN dotnet restore "Nostra.DataLoad.UI/Nostra.DataLoad.UI.csproj"
COPY . .
WORKDIR "/src/Nostra.DataLoad.UI"
RUN dotnet build "Nostra.DataLoad.UI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nostra.DataLoad.UI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nostra.DataLoad.UI.dll"]