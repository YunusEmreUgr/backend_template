using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete.Users;
using DataAccess.Abstract;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// UserOperationClaim entity'si için Entity Framework somut repository sınıfı.
    /// </summary>
    public class EfUserOperationClaimDal : EfEntityRepositoryBase<UserOperationClaim, AppDbContext>, IUserOperationClaimDal
    {
        public EfUserOperationClaimDal(AppDbContext context) : base(context)
        {
        }
    }
}
