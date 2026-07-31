using Core.Entities.Concrete.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        Task<List<OperationClaim>> GetClaimsAsync(User user);
    }
}
