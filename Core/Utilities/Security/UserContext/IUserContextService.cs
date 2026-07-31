namespace Core.Utilities.Security.UserContext
{
    /// <summary>
    /// Mevcut HTTP request'indeki kimlik doğrulanmış kullanıcının bilgilerine
    /// servis katmanından erişim sağlar.
    /// 
    /// JWT token içindeki claim'leri okur ve strongly-typed bilgi döner.
    /// Constructor injection yerine bu servis inject edilir.
    /// 
    /// Kullanım:
    ///   var userId = _userContextService.GetUserId();
    ///   var email = _userContextService.GetUserEmail();
    /// 
    /// Servis kayıt: Autofac modülünde InstancePerLifetimeScope olarak kayıtlı.
    /// </summary>
    public interface IUserContextService
    {
        /// <summary>Token'daki kullanıcı ID'sini döner. (ClaimTypes.NameIdentifier)</summary>
        int GetUserId();

        /// <summary>Token'daki email adresini döner.</summary>
        string GetUserEmail();

        /// <summary>Token'daki kullanıcı adını (FirstName + LastName) döner.</summary>
        string GetUserName();

        // TODO: Uygulamaya özgü ek claim'ler buraya eklenebilir
        // Örnek: int GetTenantId();
    }
}
