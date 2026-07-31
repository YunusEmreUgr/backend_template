# Tests Katmanı (Testing Layer)

Bu katman, backend uygulamasının tüm iş mantığını (Business) ve API uç noktalarını (Controllers) test etmek amacıyla **xUnit**, **Moq**, **FluentAssertions** ve **Microsoft.AspNetCore.Mvc.Testing** kütüphaneleri kullanılarak tasarlanmış test katmanıdır.

---

## Klasör Yapısı

```text
Tests/
├── Unit/                       # Birim (Unit) Testleri
│   └── Business/               # Business katmanının birim testleri
│       ├── AuthManagerTests.cs # Giriş/Kayıt süreçlerinin testleri
│       └── ProductManagerTests.cs # Ürün/Sayfalama süreçlerinin testleri
└── Integration/                # Entegrasyon (Integration) Testleri
    └── HealthCheckTests.cs     # API ve DB in-memory entegrasyon testleri
```

---

## Kullanılan Kütüphaneler ve Amaçları

1. **xUnit:** .NET için geliştirilmiş modern, hızlı ve yaygın olarak tercih edilen test framework'üdür. Her test metodu `[Fact]` veya `[Theory]` öznitelikleri (attribute) ile işaretlenir.
2. **Moq:** Test edilecek sınıfların dış bağımlılıklarını (örneğin veritabanı erişim sınıflarını `IProductDal` veya harici servisleri) bellekte taklit etmek (mocking) amacıyla kullanılır. Bu sayede testler veritabanına bağlanmadan, tamamen izole ve milisaniyeler seviyesinde çalışır.
3. **FluentAssertions:** Testlerin doğrulama (assert) aşamasının insan diline en yakın biçimde yazılmasını sağlar. Kodun okunabilirliğini ve anlaşılırlığını artırır.
   * *Geleneksel:* `Assert.True(result.Success);`
   * *Fluent:* `result.Success.Should().BeTrue();`
4. **Microsoft.AspNetCore.Mvc.Testing:** API uygulamasını bilgisayarın belleğinde (in-memory) gerçek bir sunucu gibi ayağa kaldırarak, HTTP istekleri atıp yanıtları test etmemizi sağlayan `WebApplicationFactory` altyapısını barındırır.

---

## Testlerin Çalıştırılması

Tüm testleri konsoldan veya IDE üzerindeki Test Explorer'dan çalıştırabilirsiniz.

### Terminal (Console) ile Çalıştırma:
Proje ana dizinindeyken aşağıdaki komutu çalıştırmanız yeterlidir:
```bash
dotnet test
```

### Kod Kapsama (Test Coverage) Raporu Almak İçin:
```bash
dotnet test /p:CollectCoverage=true
```

---

## Kurumsal Test Tasarım Prensipleri

* **AAA (Arrange, Act, Assert) Düzeni:** Her test metodu üç aşamadan oluşur:
  * **Arrange (Hazırlık):** Mock nesnelerin kurulması, parametrelerin ve girdi verilerinin hazırlanması.
  * **Act (Eylem):** Test edilecek metodun çağrılması.
  * **Assert (Doğrulama):** Çıktının, beklenen durumla uyuşup uyuşmadığının kontrol edilmesi.
* **Bağımsızlık:** Her test kendi içinde izole çalışır. Bir testin başarısı veya başarısızlığı diğer testleri asla etkilemez.
* **İzolasyon (Mocking):** Birim testlerinde hiçbir şekilde gerçek veritabanı bağlantısı veya harici ağ çağrısı (API call) yapılmaz.
