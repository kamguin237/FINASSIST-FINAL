FROM mcr.microsoft.com/dotnet/sdk:10.0-resolute AS build
WORKDIR /src

COPY FinAssist.Core/FinAssist.Core.csproj FinAssist.Core/
COPY FinAssist.Application/FinAssist.Application.csproj FinAssist.Application/
COPY FinAssist.Infrastructure/FinAssist.Infrastructure.csproj FinAssist.Infrastructure/
COPY FinAssist.API/FinAssist.API.csproj FinAssist.API/

RUN dotnet restore FinAssist.API/FinAssist.API.csproj

COPY . .

RUN dotnet publish FinAssist.API/FinAssist.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-resolute AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FinAssist.API.dll"]
