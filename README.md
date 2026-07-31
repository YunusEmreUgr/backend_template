# Enterprise Clean Architecture .NET Web API Template

Bu repo, büyük ölçekli ve kurumsal düzeydeki (Tier-1) yazılım projelerinde kullanılmak üzere tasarlanmış, **Clean Architecture (Temiz Mimari)** prensiplerine ve **AOP (Aspect Oriented Programming)** yaklaşımına uygun, yüksek performanslı ve güvenli bir .NET 9.0 Web API şablonudur (template).

---

## 🚀 Teknolojik Altyapı & Kütüphaneler

* **Runtime:** .NET 9.0 SDK
* **Veritabanı:** Entity Framework Core (PostgreSQL / Npgsql)
* **IoC Container & AOP:** Autofac & Autofac.Extras.DynamicProxy
* **Validasyon:** FluentValidation
* **Loglama:** Serilog (Console & File Sinks)
* **Önbellekleme:** In-Memory Cache & Distributed Cache (Redis - StackExchange.Redis)
* **Kimlik Doğrulama:** JWT (JSON Web Token), Refresh Token, Google OAuth & Apple ID Token Doğrulama
* **Versiyonlama:** Asp.Versioning (URL-based API Versioning)
* **Güvenlik:** ASP.NET Core Rate Limiting (DDoS & Brute Force Protection)
* **Dökümantasyon:** Swagger / OpenAPI
* **Test:** xUnit, Moq, FluentAssertions, WebApplicationFactory (Integration Tests)

---

## 🏛️ Mimari Katmanlar (Clean Architecture)

Proje, bağımlılıkların içe doğru akmasını ve iş mantığının (Business Logic) dış teknolojilerden (Veritabanı, UI, API paketleri vb.) izole edilmesini sağlayan 5 ana katman + 1 Test projesinden oluşmaktadır:

```
Solution/
├── Core/             # Sistem genelinde paylaşılan bağımsız altyapı sınıfları (Framework katmanı)
├── Entities/         # Veritabanı tabloları, DTO'lar ve Domain modelleri
├── DataAccess/       # EF Core DbContext, Interceptors, Repository somut sınıfları
├── Business/         # İş mantığı servisleri, doğrulama kuralları ve AOP Aspect'leri
├── WebApi/           # API Controller'lar, Middlewares ve Startup konfigürasyonları
└── Tests/            # Birim (Unit) ve Entegrasyon (Integration) testleri
```

Detaylı katman mimarisine ve sorumluluklarına ulaşmak için ilgili klasörlerdeki `LAYER.md` dosyalarını inceleyebilirsiniz:
- [Core Katmanı Açıklaması](file:///c:/src/backend_template/Core/LAYER.md)
- [Business Katmanı Açıklaması](file:///c:/src/backend_template/Business/LAYER.md)
- [Tests Katmanı Açıklaması](file:///c:/src/backend_template/Tests/LAYER.md)

---

## 🌟 Öne Çıkan Kurumsal Özellikler

### 1. Aspect Oriented Programming (AOP) Altyapısı
Metotların üzerine eklenen nitelikler (attributes) ile kod tekrarı önlenir ve çapraz kesen ilgiler (cross-cutting concerns) merkezi olarak yönetilir:
* **`[TransactionScopeAspect]`**: İşlem bütünlüğünü (Unit of Work) sağlar. Metot içinde hata oluşursa veritabanındaki işlemler otomatik geri alınır (Rollback).
* **`[PerformanceAspect]`**: Belirlenen milisaniye eşiğini aşan metotları tespit ederek Serilog üzerinden uyarı logu fırlatır.
* **`[ValidationAspect]`**: FluentValidation kurallarını iş mantığına girmeden önce otomatik çalıştırır.
* **`[CacheAspect]` & `[CacheRemoveAspect]`**: Sık sorgulanan verileri otomatik önbelleğe alır ve veri güncellendiğinde önbelleği temizler.

### 2. Google & Apple OAuth Entegrasyonu
Canlı mobil veya web uygulamaları için en üst düzey güvenlikte sosyal giriş altyapısı:
* **Google Auth**: İstemciden gelen Token'ı Google API'sine gitmeden, imzalarını ve Client ID'lerini doğrulayarak doğrular.
* **Apple Auth**: Apple Public Key setini dinamik çekip JWT imza doğrulamasını gerçekleştirir, güvenli oturum açtırır.
* Şifre hashleme ve şifre çözme işlemleri `HMACSHA512` algoritması ile tamamen güvenli şekilde tasarlanmıştır.

### 3. EF Core Audit Interceptor (Otomatik Tarih Takibi)
Veritabanı kayıt işlemlerinde, `IAuditableEntity` arayüzünü implement eden entity'lerin `CreatedAt` ve `UpdatedAt` alanları EF Core `SaveChangesInterceptor` tarafından otomatik doldurulur. Yansıma (Reflection) maliyeti olmadan çalışır.

### 4. Sayfalama Altyapısı (Pagination)
Veritabanı düzeyinde verimli ve hızlı sayfalama sağlayan `PaginatedList<T>` ve `.ToPaginatedListAsync()` uzantı metotları bulunmaktadır. Toplam sayfa, anlık sayfa ve kayıt sayılarını içeren JSON uyumlu bir meta veri döndürür.

### 5. API Güvenliği (Rate Limiting & CORS)
* **Global Rate Limiter**: IP veya kullanıcı bazlı olarak dakikada maksimum 100 istek sınırı uygular (DDoS koruması).
* **Hassas Endpoint Koruması**: Brute Force saldırılarını önlemek için `/api/v1/auth/login` (15 dakikada max 10) ve `/api/v1/auth/register` (1 saatte max 5) için özel limitler tanımlıdır.

### 6. Sistem Sağlık Kontrolleri (Health Checks)
Uygulamanın ve PostgreSQL veritabanı bağlantısının durumunu izleyen `/api/health` rotası mevcuttur. Kubernetes veya Load Balancer sağlık denetimleri için hazırdır.

---

## 💎 Kurumsal Standartlar & Tasarım Prensipleri

Bu şablon, kurumsal düzeydeki yazılım geliştirme süreçlerinde sürdürülebilirliği (maintainability), genişletilebilirliği (extensibility) ve test edilebilirliği (testability) maksimize etmek amacıyla aşağıdaki prensiplere sadık kalınarak tasarlanmıştır:

* **Clean Architecture**: İş kuralları (Core/Business), dış dünyadaki değişikliklerden (veritabanı değişimi, API kütüphanesi güncellemeleri vb.) etkilenmez.
* **SOLID Prensipleri**: Her sınıfın tek bir sorumluluğu vardır, bağımlılıklar soyutlamalar (Interfaces) üzerinden yönetilir (Dependency Inversion).
* **Aspect Oriented Programming (AOP)**: İş mantığı kodları sadece iş mantığını barındırır. Loglama, önbellekleme, doğrulama ve işlem bütünlüğü (transaction) gibi sistem genelindeki ortak kesen ilgiler (cross-cutting concerns) kod kirliliği yaratmadan araya giren (interceptor) aspect'ler ile çözülür.
* **Kapsamlı Test Altyapısı**: İş kuralları ve validasyonlar Unit Test ile, API rotaları ve sistem entegrasyonu ise in-memory çalışan Integration Test altyapısı ile güvence altına alınmıştır.

