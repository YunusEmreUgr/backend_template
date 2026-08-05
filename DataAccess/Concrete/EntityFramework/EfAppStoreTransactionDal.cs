using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework
{
    public class EfAppStoreTransactionDal : EfEntityRepositoryBase<AppStoreTransaction, AppDbContext>, IAppStoreTransactionDal
    {
        public EfAppStoreTransactionDal(AppDbContext context) : base(context)
        {
        }
    }
}
