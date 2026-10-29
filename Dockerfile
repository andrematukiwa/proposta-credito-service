# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura só com os arquivos de projeto: esta camada fica em cache enquanto as dependências não mudarem
COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/PropostaCredito.Api/PropostaCredito.Api.csproj src/PropostaCredito.Api/
COPY src/PropostaCredito.Application/PropostaCredito.Application.csproj src/PropostaCredito.Application/
COPY src/PropostaCredito.Domain/PropostaCredito.Domain.csproj src/PropostaCredito.Domain/
COPY src/PropostaCredito.Infrastructure/PropostaCredito.Infrastructure.csproj src/PropostaCredito.Infrastructure/
RUN dotnet restore src/PropostaCredito.Api/PropostaCredito.Api.csproj

COPY src/ src/
RUN dotnet publish src/PropostaCredito.Api/PropostaCredito.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Usuário sem privilégios já existente na imagem oficial
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "PropostaCredito.Api.dll"]
