# DataAccess Katmanı

## Genel Bakış

**DataAccess (Veri Erişim) Katmanı**, uygulamanın veritabanı (Database) işlemlerini gerçekleştirdiği katmandır. 

Clean Architecture prensiplerine göre bu katman:
- `Core` ve `Entities` katmanlarına bağımlıdır.
- İş kurallarının veritabanı teknolojisinden (PostgreSQL, MSSQL vb.) bağımsız olmasını sağlar.
- Entity Framework Core teknolojisini ve Repository Pattern tasarım desenini kullanır.

---

## Klasör Yapısı

```
DataAccess/
├── Abstract/
│   ├── IProductDal.cs             # Product veri erişim arayüzü
│   ├── IUserDal.cs                # User veri erişim arayüzü (GetClaims)
│   ├── IOperationClaimDal.cs      # Rol veri erişim arayüzü
│   └── IUserOperationClaimDal.cs  # Kullanıcı-Rol eşleşme veri erişim arayüzü
│
└── Concrete/EntityFramework/
    ├── AppDbContext.cs            # EF Core DbContext nesnesi
    ├── AppDbContextFactory.cs     # Design-time migrations factory
    ├── EfProductDal.cs            # EF Core Product implementasyonu
    ├── EfUserDal.cs               # EF Core User implementasyonu
    ├── EfOperationClaimDal.cs     # EF Core OperationClaim implementasyonu
    ├── EfUserOperationClaimDal.cs # EF Core UserOperationClaim implementasyonu
    └── RefreshTokenRepository.cs  # RefreshToken somut işlemler
```

---

## Temel Bileşenler

### 1. DbContext (AppDbContext)
Entity Framework Core tablosu ve veritabanı yapılandırmasını yönetir.
- **Global Query Filters:** Soft-Delete özelliği barındıran entity'ler için `!x.IsDeleted` filtresini otomatik olarak uygular.
- **Restrict Behavior:** İlişkisel veri silme işlemlerinde cascade engellenir.
- **Auto-Timestamps:** `SaveChangesAsync` metodu override edilerek eklenen kayıtlara `CreatedAt`, güncellenen kayıtlara `UpdatedAt` alanları otomatik set edilir.

### 2. Repository Pattern (DAL - Data Access Layer)
Her entity için veri tabanı işlemleri `IEntityRepository<T>` arayüzünden türeyen özel DAL arayüzleri ile tanımlanır. Somut sınıflar ise `EfEntityRepositoryBase` sınıfından miras alarak kod tekrarını önler.

```csharp
public class EfProductDal : EfEntityRepositoryBase<Product, AppDbContext>, IProductDal
{
    public EfProductDal(AppDbContext context) : base(context)
    {
    }
}
```

---

## Kurallar ve Standartlar

1. **Bağlantı Dizesi (Connection String):**
   - Üretim ortamında bağlantı dizeleri kesinlikle kod içinde hardcoded olarak saklanmamalı, `appsettings.json` veya çevre değişkenleri (environment variables) üzerinden yönetilmelidir.
   - Tasarım zamanı migration oluşturmak için `AppDbContextFactory` sınıfı kullanılır.

2. **Sorgu Performansı:**
   - Veri okuma işlemlerinde varsayılan olarak `AsNoTracking()` kullanılmaktadır (bu özellik generic repository base'de mevcuttur). Eğer veriler üzerinde update işlemi yapılacaksa context'in track etmesi için `GetByIdAsync` veya tracking'i açık özel sorgular yazılmalıdır.

3. **İndeksleme (Indexing):**
   - Sıkça sorgulanan, filtrelenen veya sıralanan alanlar (örneğin `CreatedAt`, `CategoryId` gibi) `OnModelCreating` içinde mutlaka indekslenmelidir.
