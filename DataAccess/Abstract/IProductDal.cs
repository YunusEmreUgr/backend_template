using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Product entity'sine özgü veri erişim arayüzü.
    /// IEntityRepository'den gelen temel CRUD operasyonlarını miras alır.
    /// İleride Product'a özel SQL/Linq sorguları buraya tanımlanır.
    /// </summary>
    public interface IProductDal : IEntityRepository<Product>
    {
        // Örnek: Task<List<Product>> GetProductsWithDetailsAsync();
    }
}
