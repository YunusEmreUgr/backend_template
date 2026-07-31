using Core.Entities.Concrete.Users;

namespace Business.Abstract
{
    /// <summary>
    /// Kullanıcı veritabanı işlemlerini yöneten iç servis arayüzü.
    /// Genellikle AuthManager veya kullanıcı profil yönetimi tarafından çağrılır.
    /// </summary>
    public interface IUserService
    {
        Task<User?> GetByMail(string email);
        Task<User?> GetById(int userId);
        Task<User?> GetByGoogleId(string googleId);
        Task<User?> GetByAppleId(string appleId);
        Task Add(User user);
        Task Update(User user);
        List<OperationClaim> GetClaims(User user);
    }
}
