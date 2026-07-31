using Business.Abstract;
using Core.Entities.Concrete.Users;
using DataAccess.Abstract;

namespace Business.Concrete
{
    /// <summary>
    /// Kullanıcı rollerini yöneten somut servis sınıfı.
    /// </summary>
    public class UserOperationClaimManager : IUserOperationClaimService
    {
        private readonly IUserOperationClaimDal _userOperationClaimDal;

        public UserOperationClaimManager(IUserOperationClaimDal userOperationClaimDal)
        {
            _userOperationClaimDal = userOperationClaimDal;
        }

        /// <inheritdoc/>
        public async Task AddUserClaim(int userId)
        {
            // Yeni kayıt olan her kullanıcıya varsayılan olarak 1 ID'li rol atanır (örn: "User")
            await _userOperationClaimDal.AddAsync(new UserOperationClaim 
            { 
                UserId = userId, 
                OperationClaimId = 1 
            });
        }
    }
}
