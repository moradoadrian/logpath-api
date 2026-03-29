# Usa el entorno de trabajo oficial de .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copia los archivos y restaura dependencias
COPY *.csproj ./
RUN dotnet restore

# Compila el proyecto
COPY . ./
RUN dotnet publish -c Release -o out

# Crea la imagen final, super ligera
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

# Configura la API para que escuche en el puerto 80 dentro del contenedor
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Comando para encender la API
ENTRYPOINT ["dotnet", "LogPath.Api.dll"]