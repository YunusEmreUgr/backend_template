using Core.DataAccess;
using Core.Entities.Concrete.Users;

namespace DataAccess.Abstract
{
    /// <summary>
    /// User entity'sine özgü veri erişim arayüzü.
    /// Kullanıcının sahip olduğu rolleri/yetkileri (OperationClaims) çekmek için özel metod barındırır.
    /// </summary>
    public interface IUserDal : IEntityRepository<User>
    {
        /// <summary>
        /// Kullanıcının sahip olduğu tüm OperationClaim yetki/rol listesini döner.
        /// Giriş ve yetki kontrollerinde (SecuredOperation) kullanılır.
        /// </summary>
        List<OperationClaim> GetClaims(User user);
    }
}
