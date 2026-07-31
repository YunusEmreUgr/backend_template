using Core.Entities.Concrete.Users;

namespace Core.DataAccess
{
    /// <summary>
    /// RefreshToken'ın veritabanı işlemleri için özel repository interface.
    /// 
    /// IEntityRepository'den ayrı tutulmuştur çünkü RefreshToken'ın
    /// özel işlemleri (token hash ile arama gibi) bulunmaktadır.
    /// Bu interface Autofac modülünde direkt olarak register edilir.
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>Yeni refresh token ekler.</summary>
        Task AddAsync(RefreshToken token);

        /// <summary>
        /// Token hash değerine göre token bulur.
        /// Güvenlik gereği plaintext token değil hash değeri ile aranır.
        /// </summary>
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

        /// <summary>Mevcut token'ı günceller (revoke işlemi için).</summary>
        Task UpdateAsync(RefreshToken token);
    }
}
