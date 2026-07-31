using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Product entity'si için Entity Framework somut repository sınıfı.
    /// EfEntityRepositoryBase'den generic CRUD işlemlerini miras alır.
    /// </summary>
    public class EfProductDal : EfEntityRepositoryBase<Product, AppDbContext>, IProductDal
    {
        public EfProductDal(AppDbContext context) : base(context)
        {
        }
    }
}
