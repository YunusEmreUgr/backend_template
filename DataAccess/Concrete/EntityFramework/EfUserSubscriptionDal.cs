using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework
{
    public class EfUserSubscriptionDal : EfEntityRepositoryBase<UserSubscription, AppDbContext>, IUserSubscriptionDal
    {
        public EfUserSubscriptionDal(AppDbContext context) : base(context)
        {
        }
    }
}
