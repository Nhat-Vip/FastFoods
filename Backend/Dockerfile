# Dockerfile cho ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ASM.csproj", "./"]
RUN dotnet restore "ASM.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "ASM.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ASM.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ASM.dll"]