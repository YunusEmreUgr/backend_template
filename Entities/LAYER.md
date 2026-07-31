# Entities Katmanı

## Genel Bakış

**Entities (Domain) Katmanı**, uygulamanın iş modellerini (Entities) ve bu modellerle ilişkili veri taşıma nesnelerini (DTOs) barındırır. 

Clean Architecture prensiplerine göre bu katman:
- Dış dünyaya (veritabanı, sunucu, kütüphaneler) bağımlı değildir.
- Sadece `Core` katmanına bağımlıdır (Core'da tanımlı `IEntity` ve `IDto` arayüzlerini implement eder).
- İş kurallarının üzerinde çalıştığı veri yapılarını tanımlar.

---

## Klasör Yapısı

```
Entities/
├── Concrete/
│   └── Product.cs                 # Örnek iş modeli (Entity)
└── Dtos/
    ├── Auth/
    │   ├── UserForLoginDto.cs     # Kullanıcı giriş bilgileri
    │   └── UserForRegisterDto.cs  # Kullanıcı kayıt bilgileri
    └── Product/
        ├── ProductAddDto.cs       # Ürün ekleme veri modeli
        └── ProductUpdateDto.cs    # Ürün güncelleme veri modeli
```

---

## Temel Bileşenler

### 1. Concrete (Somut Varlıklar)
Veritabanında birer tabloya karşılık gelen sınıflardır. `IEntity` arayüzünü implement ederler.

```csharp
public class Product : IEntity
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    // ...
}
```

*Not: Sistemdeki `User`, `OperationClaim`, `UserOperationClaim` ve `RefreshToken` gibi genel güvenlik entity'leri yeniden kullanılabilirlik ve bağımsızlık amacıyla `Core` katmanının altındaki `Entities/Concrete/Users` klasöründe yer almaktadır.*

### 2. Dtos (Data Transfer Objects)
Client-Server veya katmanlar arası veri transferlerinde (örneğin Controller'dan Business Manager'a veri taşırken) kullanılan hafif (lightweight) sınıflardır. `IDto` arayüzünü implement ederler.
Uygulamanın veritabanı şemasını dış dünyaya açmasını engeller, güvenlik ve esneklik sağlar.

```csharp
public class ProductAddDto : IDto
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
}
```

---

## Kurallar ve Standartlar

1. **İsimlendirme Standartları:**
   - Entity sınıfları tekil isimlerle adlandırılmalıdır (Örn: `Product`, `Category`).
   - DTO sınıfları hangi amaçla kullanıldığını belirtmeli ve sonuna `Dto` eki almalıdır (Örn: `ProductAddDto`, `UserForRegisterDto`).

2. **Tip Güvenliği ve Nullability:**
   - `.NET 8/9` standartlarına uygun olarak `nullable reference types` (`#nullable enable`) aktif kullanılmalıdır. Null olamayacak referans tipleri `null!` veya default değerlerle tanımlanmalıdır.

3. **Bağımlılık Durumu:**
   - Bu katmana hiçbir harici paket (EF Core, Autofac, AutoMapper vb.) referans olarak eklenmemelidir. Sadece ham C# sınıfları olmalıdır.
