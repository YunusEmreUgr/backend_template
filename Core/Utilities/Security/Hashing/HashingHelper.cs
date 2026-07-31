using System.Security.Cryptography;
using System.Text;

namespace Core.Utilities.Security.Hashing
{
    /// <summary>
    /// HMACSHA512 tabanlı şifre hash/doğrulama yardımcısı.
    /// 
    /// Güvenlik özellikleri:
    ///   - Her kullanıcı için benzersiz salt üretilir → Rainbow table saldırılarına karşı koruma
    ///   - HMACSHA512 → Güçlü hash algoritması (512-bit çıktı)
    ///   - Salt ve hash ayrı ayrı saklanır
    /// 
    /// NOT: Google/Apple OAuth ile giriş yapan kullanıcıların PasswordHash ve PasswordSalt'u null olur.
    /// Veritabanında nullable olarak tanımlanmalıdır.
    /// </summary>
    public static class HashingHelper
    {
        /// <summary>
        /// Şifreyi hash'ler ve salt üretir.
        /// Yeni kayıt oluştururken çağrılır.
        /// 
        /// Örnek:
        ///   HashingHelper.CreatePasswordHash(password, out byte[] hash, out byte[] salt);
        ///   user.PasswordHash = hash;
        ///   user.PasswordSalt = salt;
        /// </summary>
        public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            // Salt: HMACSHA512'nin otomatik ürettiği key (her instance için benzersiz)
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        /// <summary>
        /// Girilen şifrenin hash'i, saklanan hash ile eşleşiyor mu kontrol eder.
        /// Login işleminde çağrılır.
        /// 
        /// Örnek:
        ///   bool isValid = HashingHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt);
        /// </summary>
        public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            // Aynı salt ile hash'le ve karşılaştır
            using var hmac = new HMACSHA512(passwordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }
    }
}
