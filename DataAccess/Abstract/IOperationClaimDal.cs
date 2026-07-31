using Core.DataAccess;
using Core.Entities.Concrete.Users;

namespace DataAccess.Abstract
{
    /// <summary>
    /// OperationClaim entity'si için veri erişim arayüzü.
    /// </summary>
    public interface IOperationClaimDal : IEntityRepository<OperationClaim>
    {
    }
}
