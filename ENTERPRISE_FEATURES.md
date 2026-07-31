# Kurumsal Mimari Ek Özellikler & Yapılandırma Kılavuzu

Bu kılavuz, template projesine son eklenen kurumsal (enterprise-grade) özelliklerin nasıl çalıştırılacağını, yapılandırılacağını ve yeni projelere nasıl uyarlanacağını açıklamaktadır.

---

## 1. Dağıtık Önbellekleme (Redis Integration)

Mevcut yapıda AOP tabanlı caching (`[CacheAspect]`) varsayılan olarak **bellek içi (In-Memory)** çalışmaktadır. Çoklu sunucu ve yük dengeleyici (Load Balancer) mimarilerinde verilerin senkronize kalması için **Redis** entegrasyonu hazır durumdadır.

### Aktif Etme Adımları:
1. **appsettings.json**: Bağlantı adresini kontrol edin. Varsayılan olarak lokal Redis için tanımlıdır:
   ```json
   "ConnectionStrings": {
     "Redis": "localhost:6379"
   }
   ```
2. **WebApi/Program.cs**: Redis servisini kaydeden aşağıdaki satırı yorum satırından çıkarın:
   ```csharp
   services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetConnectionString("Redis"));
   ```
3. **Core/DependencyResolvers/CoreModule.cs**: `MemoryCacheManager` kaydını yorum satırı yapıp `RedisCacheManager` kaydını aktifleştirin:
   ```csharp
   // services.AddSingleton<ICacheManager, MemoryCacheManager>();
   services.AddSingleton<ICacheManager, RedisCacheManager>();
   ```
Artık projedeki tüm önbellekleme istekleri Redis üzerinden yönetilecektir.

---

## 2. API Versiyonlama (API Versioning)

Mobil ve web istemcilerinin eski/yeni sürümlerinin hatasız çalışabilmesi için URL tabanlı API Versiyonlama (`api/v1/controller`) aktif edilmiştir.

### Yapılandırma & Kullanım:
* **Yeni Sürüm Tanımlama**: Bir Controller sınıfı oluşturduğunuzda `[ApiVersion]` ve `[Route]` parametrelerini aşağıdaki gibi tanımlamalısınız:
  ```csharp
  [ApiVersion("1.0")] // Veya yeni sürüm için "2.0"
  [Route("api/v{version:apiVersion}/[controller]")]
  [ApiController]
  public class ProductsController : ControllerBase
  ```
* **Swagger Ayarı**: `Program.cs` içerisindeki `AddApiExplorer` ayarı Swagger dökümanının `/v1/` ve `/v2/` gruplarını otomatik ayırmasını sağlar.

---

## 3. Çoklu Dil Desteği (Localization / I18N)

Sistemdeki hata, validasyon veya iş kuralları mesajlarının dinamik olarak kullanıcının diline göre (Header'daki `Accept-Language` parametresine bağlı olarak) dönmesi sağlanmıştır.

### Yapılandırma & Kullanım:
1. **Dil Dosyaları**: `WebApi/Resources/Core.Resources.SharedResource.{culture}.resx` dosyalarında dil anahtarları tutulur:
   - `SharedResource.tr-TR.resx` (Türkçe karşılıklar)
   - `SharedResource.en-US.resx` (İngilizce karşılıklar)
2. **Middleware**: `Program.cs` içerisindeki `app.UseRequestLocalization()` gelen HTTP isteklerindeki dili otomatik yakalar.
3. **Kullanım Yöntemi (Exception Middleware)**: 
   Uygulama içerisinde bir hata fırlatırken dil anahtarını kullanırsınız. Örnek:
   ```csharp
   throw new UserFriendlyException("Kullanıcı bulunamadı.");
   ```
   [ExceptionMiddleware](file:///c:/src/backend_template/Core/Utilities/Exceptions/ExceptionMiddleware.cs) bu hatayı yakalar ve dil dosyalarından dil karşılığını otomatik bularak yanıt döner. Eğer `Accept-Language: en-US` gönderildiyse, çıktı `"User not found."` olur.

---

## 4. Docker ve Konteynerizasyon (Dockerization)

Uygulamanın çalışması için gereken PostgreSQL ve Redis gibi dış bağımlılıkları tek bir komutla ayağa kaldırmak ve uygulamayı yayınlamak için Docker dosyaları kök dizine eklenmiştir.

### Kullanım Kılavuzu:
* **docker-compose.yml**: Proje dizininde aşağıdaki komutu çalıştırarak PostgreSQL, Redis ve API projesini konteyner olarak anında ayağa kaldırabilirsiniz:
  ```bash
  docker-compose up -d
  ```
* **Yeni Projeye Uyarlama**:
  Eğer bu template'i kopyalayıp yeni bir proje oluşturursanız ve projelerin klasör/dosya isimlerini değiştirirseniz (örn: `WebApi` -> `Campus.WebApi`), [Dockerfile](file:///c:/src/backend_template/Dockerfile) içindeki `COPY` ve `WORKDIR` satırlarındaki klasör isimlerini yeni projenize göre güncellemeniz yeterlidir.
