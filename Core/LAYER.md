# Core Katmanı

## Genel Bakış

**Core**, tüm katmanlar tarafından kullanılan temel altyapı kütüphanesidir. Framework-agnostic (ASP.NET Core'a bağımlı olmayan) tasarımı sayesinde herhangi bir .NET projesine taşınabilir.

Diğer katmanlar (Entities, DataAccess, Business, WebApi) Core'a bağımlıdır ama **Core hiçbir katmana bağımlı değildir**.

---

## Klasör Yapısı

```
Core/
├── CrossCuttingConcerns/
│   ├── Caching/                     # Cache soyutlaması
│   │   ├── ICacheManager.cs         # Cache interface (Get/Add/Remove/Pattern)
│   │   └── Microsoft/
│   │       └── MemoryCacheManager.cs # In-memory implementasyon
│   └── Validation/
│       └── ValidationTool.cs        # FluentValidation entegrasyon aracı
│
├── DataAccess/
│   ├── IEntityRepository.cs         # Generic repository interface
│   ├── IRefreshTokenRepository.cs   # RefreshToken özel repository
│   └── EntityFramework/
│       └── EfEntityRepositoryBase.cs # EF Core generic repository base
│
├── DependencyResolvers/
│   └── CoreModule.cs                # Core servislerini yükleyen modül
│
├── Entities/
│   ├── Abstract/
│   │   ├── IEntity.cs               # Entity marker interface
│   │   └── IDto.cs                  # DTO marker interface
│   └── Concrete/Users/
│       ├── User.cs                  # Kullanıcı entity (kimlik doğrulama destekli)
│       ├── OperationClaim.cs        # Yetki/rol entity
│       ├── UserOperationClaim.cs    # Kullanıcı-Yetki ilişki tablosu
│       └── RefreshToken.cs         # Refresh token (rotation destekli)
│
├── Extensions/
│   ├── ClaimExtensions.cs           # JWT claim ekleme extension'ları
│   ├── ClaimsPrincipalExtensions.cs # HttpContext.User extension'ları
│   └── ServiceCollectionExtensions.cs # Core modül yükleme extension'ı
│
└── Utilities/
    ├── Business/
    │   └── BusinessRules.cs         # İş kuralı zincirleme yardımcısı
    ├── Email/
    │   ├── IEmailService.cs         # Email servisi interface
    │   └── SmtpEmailService.cs      # SMTP implementasyon
    ├── Exceptions/
    │   ├── ErrorCodes.cs            # Machine-readable hata kodları
    │   ├── ErrorResponse.cs         # Standart API hata yanıtı modeli
    │   ├── ExceptionMiddleware.cs   # Global hata yakalama middleware
    │   └── UserFriendlyException.cs # Kontrollü iş mantığı hataları
    ├── Interceptors/
    │   ├── AspectInterceptorSelector.cs # AOP selector
    │   ├── MethodInterception.cs    # Async-aware AOP base
    │   └── MethodInterceptionBaseAttribute.cs # Attribute base
    ├── IoC/
    │   ├── ICoreModule.cs           # Modül interface
    │   └── ServiceTool.cs           # Aspect'ler için Service Locator
    ├── Logging/
    │   ├── ILoggerService.cs        # Loglama interface
    │   └── SerilogLoggerService.cs  # Serilog implementasyon
    ├── Results/
    │   ├── IResult.cs               # Sonuç interface
    │   ├── IDataResult.cs           # Veri döndüren sonuç interface
    │   ├── Result.cs                # Base sonuç sınıfı
    │   ├── DataResult.cs            # Generic data sonuç base
    │   ├── SuccessResult.cs         # Başarılı sonuç
    │   ├── ErrorResult.cs           # Başarısız sonuç
    │   ├── SuccessDataResult.cs     # Başarılı + veri
    │   └── ErrorDataResult.cs      # Başarısız + veri
    └── Security/
        ├── Encryption/
        │   ├── SecurityKeyHelper.cs     # JWT signing key oluşturucu
        │   └── SigningCredentialsHelper.cs # HMACSHA512 signing
        ├── Hashing/
        │   └── HashingHelper.cs         # HMACSHA512 şifre hash/doğrulama
        ├── JWT/
        │   ├── AccessToken.cs           # JWT yanıt modeli
        │   ├── ITokenHelper.cs          # Token helper interface
        │   ├── JwtHelper.cs             # JWT + RefreshToken implementasyon
        │   └── TokenOptions.cs          # appsettings konfigürasyon modeli
        └── UserContext/
            ├── IUserContextService.cs   # Kullanıcı context interface
            └── UserContextService.cs    # JWT claim okuma implementasyon
```

---

## Temel Kavramlar

### IResult Pattern

Her servis metodu `IResult` veya `IDataResult<T>` döner. Bu sayede:
- Exception fırlatmadan başarı/hata bilgisi taşınır
- HTTP status kodu ve hata kodu birlikte iletilir
- Controller'lar sonucu doğrudan client'a yönlendirir

```csharp
// Başarılı
return new SuccessDataResult<User>(user, "Giriş başarılı", 200);

// Başarısız
return new ErrorDataResult<User>("Kullanıcı bulunamadı", 404, ErrorCodes.ResourceNotFound);
```

### AOP (Aspect-Oriented Programming)

Castle DynamicProxy + Autofac ile method intercept:

```csharp
// Business katmanında servis metoduna attribute ekle
[SecuredOperation("Admin")]   // Yetki kontrolü
[CacheAspect(duration: 60)]   // Cache
[ValidationAspect(typeof(ProductAddValidator))] // Validasyon
public async Task<IDataResult<Product>> Add(ProductAddDto dto) { ... }
```

### Hata Yönetimi

```
HTTP Request
    → ExceptionMiddleware (tüm hataları yakalar)
        → ValidationException (FluentValidation) → 400
        → UserFriendlyException → İlgili HTTP kodu
        → UnauthorizedAccessException → 401
        → SecurityException → 403
        → KeyNotFoundException → 404
        → Exception → 500
```

### Cache Sistemi

```csharp
// Service Locator (Aspect içinde)
var cache = ServiceTool.ServiceProvider.GetService<ICacheManager>();
cache.Add("Products_All", data, 60); // 60 dakika

// Pattern ile temizle
cache.RemoveByPattern("Products_");
```

---

## Bağımlılıklar (NuGet)

| Paket | Amaç |
|-------|------|
| Autofac | Dependency Injection container |
| Castle.Core | DynamicProxy (AOP) |
| FluentValidation | Validasyon |
| Serilog | Loglama |
| Microsoft.IdentityModel.Tokens | JWT doğrulama |
| System.IdentityModel.Tokens.Jwt | JWT üretimi |
| Microsoft.Extensions.Caching.Memory | In-memory cache |
| Microsoft.EntityFrameworkCore | Generic repository için |

---

## Yeni Katman Eklerken

1. `IEntityRepository<T>` → Tüm CRUD işlemleri hazır
2. `IResult` / `IDataResult<T>` → Servis geri dönüş tipleri
3. `MethodInterception` → Yeni aspect oluşturmak için miras al
4. `ICacheManager` → Cache işlemleri
5. `ILoggerService` → Loglama
