using Core.Entities.Abstract;

namespace Core.Entities.Concrete.Users
{
    /// <summary>
    /// Sistemde tanımlı yetki/rol.
    /// 
    /// Örnek değerler:
    ///   - "Admin"     → Tam yetkili sistem yöneticisi
    ///   - "Moderator" → İçerik moderatörü
    ///   - "User"      → Standart kullanıcı (varsayılan)
    /// 
    /// [SecuredOperation("Admin,Moderator")] attribute'u ile servis metodlarında kullanılır.
    /// </summary>
    public class OperationClaim : IEntity
    {
        public int OperationClaimId { get; set; }
        public string OperationClaimName { get; set; } = null!;
    }
}
