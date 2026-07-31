namespace Business.Constants
{
    /// <summary>
    /// Uygulama genelinde kullanıcıya dönülecek mesajların merkezi listesi.
    /// Dil desteği (Localization) eklenmek istenirse bu sınıf altyapısı genişletilebilir.
    /// </summary>
    public static class Messages
    {
        // ─── Genel Kimlik Doğrulama Mesajları ──────────────────────────────────
        public const string UserRegistered = "Kullanıcı başarıyla kayıt oldu.";
        public const string UserNotFound = "Kullanıcı bulunamadı.";
        public const string PasswordError = "Parola hatalı.";
        public const string SuccessfulLogin = "Sisteme giriş başarılı.";
        public const string UserAlreadyExists = "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten mevcut.";
        public const string AuthorizationDenied = "Bu işlemi gerçekleştirmek için yetkiniz yok.";
        public const string AccessTokenCreated = "Erişim anahtarı başarıyla oluşturuldu.";
        public const string UserBanned = "Hesabınız askıya alınmıştır. Lütfen yönetici ile iletişime geçin.";

        // ─── Örnek Ürün (Product) Mesajları ────────────────────────────────────
        public const string ProductAdded = "Ürün başarıyla eklendi.";
        public const string ProductDeleted = "Ürün başarıyla silindi.";
        public const string ProductUpdated = "Ürün başarıyla güncellendi.";
        public const string ProductsListed = "Ürünler başarıyla listelendi.";
        public const string ProductListed = "Ürün detayları getirildi.";
        public const string ProductNotFound = "Aradığınız ürün bulunamadı.";
        public const string ProductNameAlreadyExists = "Bu isimde bir ürün zaten mevcut.";
    }
}
