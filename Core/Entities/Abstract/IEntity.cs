namespace Core.Entities.Abstract
{
    /// <summary>
    /// Tüm entity sınıflarının implement etmesi gereken temel marker interface.
    /// Bu interface sayesinde generic repository yalnızca entity olan sınıflarla çalışır.
    /// 
    /// Kullanım:
    ///   public class Product : IEntity { ... }
    /// </summary>
    public interface IEntity
    {
    }
}
