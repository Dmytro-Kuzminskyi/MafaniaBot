FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["MafaniaBot.csproj", ""]
RUN dotnet restore "./MafaniaBot.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "MafaniaBot.csproj" -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "MafaniaBot.dll"]
