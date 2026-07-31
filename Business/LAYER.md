# Business Katmanı

## Genel Bakış

**Business (İş Mantığı) Katmanı**, uygulamanın iş mantığının (business rules), doğrulama kurallarının (validation rules), yetkilendirme aspect'lerinin ve veri haritalama profillerinin yönetildiği merkezi katmandır. 

Clean Architecture prensiplerine göre bu katman:
- `Core`, `Entities` ve `DataAccess` katmanlarına bağımlıdır.
- WebApi katmanından gelen istekleri işler, gerekli iş kurallarını doğrular ve veritabanı işlemlerini koordine eder.
- Doğrudan somut veri erişim nesnelerine bağımlı olmak yerine, `DataAccess` katmanının soyut (interface) yapılarına bağımlıdır (Dependency Inversion).

---

## Klasör Yapısı

```
Business/
├── Abstract/                      # Servis arayüzleri (IProductService, IAuthService vb.)
├── Concrete/                      # Somut servis yöneticileri (ProductManager, AuthManager vb.)
├── BusinessAspects/Autofac/       # İş katmanına özgü AOP Aspect'leri (SecuredOperation)
├── BusinessRules/                 # İş kuralı doğrulama sınıfları (ProductBusinessRules)
├── Constants/                     # Kullanıcıya dönen mesaj sabitleri (Messages.cs)
├── DependencyResolvers/Autofac/   # Autofac modül kaydı (AutofacBusinessModule.cs)
├── Mappings/                      # AutoMapper haritalama profilleri (AutoMapperProfile.cs)
└── ValidationRules/FluentValidation/ # DTO validation sınıfları (ProductValidator.cs vb.)
```

---

## Temel Tasarım Desenleri ve Yaklaşımlar

### 1. Aspect-Oriented Programming (AOP)
Tekrarlayan kod bloklarını (Cross-Cutting Concerns) ana metodlardan ayırarak kod temizliğini sağlar. `Castle DynamicProxy` ve `Autofac` ile implement edilmiştir.

```csharp
[SecuredOperation("admin")]                  // 1. Yetki Kontrolü
[ValidationAspect(typeof(ProductValidator))] // 2. Veri Doğrulaması
[CacheRemoveAspect("IProductService.Get")]   // 3. Cache Temizliği
public async Task<IResult> AddAsync(ProductAddDto productAddDto)
{
    // 4. Ana iş mantığı
}
```

### 2. Guard Clauses & Business Rules Pattern
İş kuralları `BusinessRules` yardımcı sınıfı üzerinden zincirleme olarak çalıştırılır. İlk başarısız kural akışı keser ve hata sonucunu döner. Bu sayede iç içe geçmiş `if` blokları engellenir.

```csharp
var result = BusinessRules.Run(
    await _productBusinessRules.CheckIfProductNameExists(productAddDto.Name)
);

if (result != null) return result; // İlk kural hata verirse doğrudan o hata döner.
```

### 3. Modüler DI (AutofacBusinessModule)
`AutofacBusinessModule` sınıfı, "Manager" ile biten tüm somut sınıfları otomatik olarak bulur, arayüzleriyle eşleştirir ve üzerlerinde AOP interceptor'larını (proxy motorunu) etkinleştirir.

---

## Kurallar ve Standartlar

1. **Servis Geri Dönüş Tipleri:**
   - Servis metodları istemciye (WebApi) doğrudan ham veri veya Entity dönmemelidir. Her zaman `IResult` (veri dönmeyen işlemler için) veya `IDataResult<T>` (veri dönen işlemler için) tipinde sonuçlar dönmelidir.

2. **Validasyon Sorumluluğu:**
   - Veri bütünlüğü kontrolleri (örn: fiyat sıfırdan büyük olmalı, ad boş olamaz vb.) `ValidationRules` altındaki validator sınıflarında `FluentValidation` kullanılarak tanımlanmalı ve metot başlarına aspect olarak eklenmelidir.

3. **İş Kuralı Sorumluluğu:**
   - Veritabanı veya diğer servislerle yapılacak mantıksal sorgulamalar (örn: "Bu isimde başka bir kayıt var mı?") `BusinessRules` klasöründeki sınıflarda tanımlanmalı ve manager metotlarında çağrılmalıdır.
