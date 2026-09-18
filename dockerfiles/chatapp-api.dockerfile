FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
ENV ASPNETCORE_HTTP_PORTS=5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["backend/ChatApp.API/ChatApp.API.csproj", "backend/ChatApp.API/"]
COPY ["backend/ChatApp.Core/ChatApp.Core.csproj", "backend/ChatApp.Core/"]
COPY ["backend/ChatApp.Infrastructure/ChatApp.Infrastructure.csproj", "backend/ChatApp.Infrastructure/"]
RUN dotnet restore "backend/ChatApp.API/ChatApp.API.csproj"
COPY . .
WORKDIR "/src/backend/ChatApp.API"
RUN dotnet build "ChatApp.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatApp.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatApp.API.dll"]
