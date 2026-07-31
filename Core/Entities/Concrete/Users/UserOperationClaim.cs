using Core.Entities.Abstract;

namespace Core.Entities.Concrete.Users
{
    /// <summary>
    /// Kullanıcı-Yetki ilişki tablosu.
    /// Bir kullanıcının hangi operasyon yetkilerine (rollere) sahip olduğunu tanımlar.
    /// 
    /// AuthManager kayıt sırasında varsayılan "User" rolünü otomatik atar.
    /// Admin panelden daha yüksek roller atanabilir.
    /// </summary>
    public class UserOperationClaim : IEntity
    {
        public int UserOperationClaimId { get; set; }

        /// <summary>İlgili kullanıcının ID'si (FK → Users)</summary>
        public int UserId { get; set; }

        /// <summary>Atanan yetkinin ID'si (FK → OperationClaims)</summary>
        public int OperationClaimId { get; set; }
    }
}
