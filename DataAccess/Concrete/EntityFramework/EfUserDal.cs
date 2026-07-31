using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete.Users;
using DataAccess.Abstract;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// User entity'si için Entity Framework somut repository sınıfı.
    /// </summary>
    public class EfUserDal : EfEntityRepositoryBase<User, AppDbContext>, IUserDal
    {
        private readonly AppDbContext _context;

        public EfUserDal(AppDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Kullanıcının yetkili olduğu rolleri/talepleri Getirir.
        /// LINQ Join sorgusu ile UserOperationClaims ve OperationClaims tablolarını birleştirir.
        /// </summary>
        public List<OperationClaim> GetClaims(User user)
        {
            var result = from operationClaim in _context.OperationClaims
                         join userOperationClaim in _context.UserOperationClaims
                             on operationClaim.OperationClaimId equals userOperationClaim.OperationClaimId
                         where userOperationClaim.UserId == user.UserId
                         select new OperationClaim
                         {
                             OperationClaimId = operationClaim.OperationClaimId,
                             OperationClaimName = operationClaim.OperationClaimName
                         };

            return result.ToList();
        }
    }
}
