# 1. Base İmajı (Uygulamanın çalışacağı ortam)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 2. Build İmajı (Kodların derlendiği ortam)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Proje dosyalarını kopyala ve bağımlılıkları yükle (Layer Caching için önce csproj kopyalanır)
COPY ["WebApi/WebApi.csproj", "WebApi/"]
COPY ["Business/Business.csproj", "Business/"]
COPY ["DataAccess/DataAccess.csproj", "DataAccess/"]
COPY ["Core/Core.csproj", "Core/"]
COPY ["Entities/Entities.csproj", "Entities/"]
RUN dotnet restore "WebApi/WebApi.csproj"

# Tüm kodları kopyala
COPY . .
WORKDIR "/src/WebApi"
RUN dotnet build "WebApi.csproj" -c Release -o /app/build

# 3. Publish (Dağıtım paketi oluşturma)
FROM build AS publish
RUN dotnet publish "WebApi.csproj" -c Release -o /app/publish

# 4. Final İmajı (Base üzerine publish edilmiş dosyaları atma)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]
