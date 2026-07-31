# WebApi Katmanı

## Genel Bakış

**WebApi Katmanı**, uygulamanın dış dünya ile iletişim kurduğu (HTTP protokolü üzerinden REST API olarak) en dış katmandır. 

Clean Architecture prensiplerine göre bu katman:
- `Business`, `Entities` ve `Core` katmanlarına bağımlıdır.
- Doğrudan `DataAccess` katmanına referans vermemelidir (tüm veritabanı akışı Business üzerinden interface'lerle yürütülür).
- İstekleri (HTTP Requests) alır, kimlik doğrulaması (Authentication), yetkilendirme (Authorization) ve hız sınırı (Rate Limiting) gibi güvenlik filtrelerinden geçirdikten sonra işlenmek üzere `Business` katmanına aktarır.

---

## Klasör Yapısı

```
WebApi/
├── Controllers/
│   ├── AuthController.cs          # Kayıt, Giriş ve Token süreçleri
│   └── ProductsController.cs      # Örnek Ürün (Product) CRUD endpoint'leri
│
├── Filters/
│   └── UserFriendlyResultFilter.cs # Hatalı sonuçları standartlaştıran filtre
│
├── Middleware/
│   └── UserStatusMiddleware.cs    # Kullanıcı ban kontrolü yapan middleware
│
├── Program.cs                     # API yapılandırması ve pipeline başlangıcı
└── appsettings.json               # Konfigürasyon dosyası
```

---

## Temel Yapılandırmalar ve Pipeline Sırası

`Program.cs` içinde HTTP istek-yanıt hattı (request pipeline) şu sıra ile çalışır:

1. **ExceptionMiddleware:** Uygulamadaki tüm hataları en üstte yakalayarak standart JSON hata formatına (`ErrorResponse`) dönüştürür.
2. **SerilogRequestLogging:** HTTP isteklerinin süre, durum kodu vb. metriklerini loglar.
3. **CORS:** İzin verilen güvenli kökenlerin (origins) erişimini denetler.
4. **RateLimiter:** DDoS ve Brute-Force koruması için istek sıklığını sınırlar (örneğin giriş sayfası için 15 dakikada en fazla 10 istek).
5. **Authentication (JWT):** Bearer token doğrulaması gerçekleştirir.
6. **UserStatusMiddleware:** Giriş yapmış kullanıcının ban/askı durumunu kontrol eder (Memory Cache ile performanslı kontrol).
7. **Authorization:** Kullanıcının rol bazlı yetkisini kontrol eder.
8. **Controllers:** İstek ilgili endpoint'e yönlendirilir.

---

## Kurallar ve Standartlar

1. **REST Standartları:**
   - Endpoint'ler kaynak bazlı adlandırılmalıdır (Örn: `GET /api/products` - liste, `POST /api/products` - ekleme).
   - HTTP Metotları amacına uygun kullanılmalıdır (`GET` okuma, `POST` ekleme, `PUT` güncelleme, `DELETE` silme).
   - Dönüş tipleri HTTP durum kodları ile uyumlu olmalıdır (Başarılı kayıt: `201 Created`, Başarılı okuma: `200 OK`, Bulunamadı: `404 NotFound`).

2. **Yetkilendirme:**
   - API denetleyicileri (Controllers) kendi işlerini yapmaz, sadece istek yönlendirir. Rol yetkilendirme yeteneği AOP ile Business katmanına kaydırılmıştır (`[SecuredOperation]`).

3. **Güvenli Refresh Token Yönetimi:**
   - XSS saldırılarından korunmak için `RefreshToken` istemciye (Javascript) düz metin olarak verilmez; `HttpOnly`, `Secure` ve `SameSite=Strict` özelliklerine sahip güvenli bir **HTTP Cookie** olarak set edilir.
