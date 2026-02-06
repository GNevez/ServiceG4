# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar csproj e restaurar dependências
COPY *.csproj ./
RUN dotnet restore

# Copiar código e compilar
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Criar pasta para imagens
RUN mkdir -p /app/wwwroot/img

# Copiar aplicação compilada
COPY --from=build /app/publish .

# Expor porta
EXPOSE 5006

# Variáveis de ambiente
ENV ASPNETCORE_URLS=http://+:5006
ENV ASPNETCORE_ENVIRONMENT=Production

# Comando de inicialização
ENTRYPOINT ["dotnet", "cafApi.dll"]
