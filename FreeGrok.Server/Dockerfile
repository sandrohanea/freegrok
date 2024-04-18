FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FreeGrok.Server/FreeGrok.Server.csproj", "FreeGrok.Server/"]
COPY ["FreeGrok.Common/FreeGrok.Common.csproj", "FreeGrok.Common/"]
RUN dotnet restore "FreeGrok.Server/FreeGrok.Server.csproj"
COPY . .
WORKDIR "/src/FreeGrok.Server"
RUN dotnet build "FreeGrok.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FreeGrok.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://*:80
ENTRYPOINT ["dotnet", "FreeGrok.Server.dll", "--urls=http://*:80"]
