FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TaskManagement.sln .
COPY src/TaskManagement.Domain/TaskManagement.Domain.csproj src/TaskManagement.Domain/
COPY src/TaskManagement.Application/TaskManagement.Application.csproj src/TaskManagement.Application/
COPY src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj src/TaskManagement.Infrastructure/
COPY src/TaskManagement.Infrastructure.Postgres/TaskManagement.Infrastructure.Postgres.csproj src/TaskManagement.Infrastructure.Postgres/
COPY src/TaskManagement.Api/TaskManagement.Api.csproj src/TaskManagement.Api/
COPY tests/TaskManagement.UnitTests/TaskManagement.UnitTests.csproj tests/TaskManagement.UnitTests/
COPY tests/TaskManagement.IntegrationTests/TaskManagement.IntegrationTests.csproj tests/TaskManagement.IntegrationTests/

RUN dotnet restore src/TaskManagement.Api/TaskManagement.Api.csproj

COPY . .
RUN dotnet publish src/TaskManagement.Api/TaskManagement.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TaskManagement.Api.dll"]
