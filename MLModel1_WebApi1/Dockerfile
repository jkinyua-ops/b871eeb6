FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MLModel1_WebApi1/MLModel1_WebApi1.csproj", "MLModel1_WebApi1/"]
RUN dotnet restore "MLModel1_WebApi1/MLModel1_WebApi1.csproj"
COPY . .
WORKDIR "/src/MLModel1_WebApi1"
RUN dotnet build "MLModel1_WebApi1.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MLModel1_WebApi1.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MLModel1_WebApi1.dll"]