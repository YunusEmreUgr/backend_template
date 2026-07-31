using Core.DataAccess;
using Core.Entities.Concrete.Users;

namespace DataAccess.Abstract
{
    /// <summary>
    /// UserOperationClaim entity'si için veri erişim arayüzü.
    /// </summary>
    public interface IUserOperationClaimDal : IEntityRepository<UserOperationClaim>
    {
    }
}
