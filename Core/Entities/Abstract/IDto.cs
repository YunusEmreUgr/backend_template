namespace Core.Entities.Abstract
{
    /// <summary>
    /// Tüm DTO (Data Transfer Object) sınıflarının implement etmesi gereken marker interface.
    /// DTO'lar, katmanlar arası veri taşımak için kullanılan nesnelerdir.
    /// Entity'lerden farklı olarak veritabanı ile doğrudan ilişkileri yoktur.
    /// 
    /// Kullanım:
    ///   public class ProductAddDto : IDto { ... }
    /// </summary>
    public interface IDto
    {
    }
}
