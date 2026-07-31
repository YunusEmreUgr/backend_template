using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete.Users;
using DataAccess.Abstract;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// OperationClaim entity'si için Entity Framework somut repository sınıfı.
    /// </summary>
    public class EfOperationClaimDal : EfEntityRepositoryBase<OperationClaim, AppDbContext>, IOperationClaimDal
    {
        public EfOperationClaimDal(AppDbContext context) : base(context)
        {
        }
    }
}
