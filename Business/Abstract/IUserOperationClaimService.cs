namespace Business.Abstract
{
    /// <summary>
    /// Kullanıcı rol ve yetkilendirme eşleştirme servisi arayüzü.
    /// Yeni kayıt olan kullanıcılara varsayılan yetki (örn: "User") ataması için kullanılır.
    /// </summary>
    public interface IUserOperationClaimService
    {
        /// <summary>
        /// Yeni kayıt olan kullanıcıya varsayılan yetki/rol ataması yapar.
        /// </summary>
        Task AddUserClaim(int userId);
    }
}
