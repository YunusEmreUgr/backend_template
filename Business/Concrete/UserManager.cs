using Business.Abstract;
using Core.Entities.Concrete.Users;
using DataAccess.Abstract;

namespace Business.Concrete
{
    /// <summary>
    /// Kullanıcı veritabanı işlemlerini gerçekleştiren somut servis sınıfı.
    /// </summary>
    public class UserManager : IUserService
    {
        private readonly IUserDal _userDal;

        public UserManager(IUserDal userDal)
        {
            _userDal = userDal;
        }

        /// <inheritdoc/>
        public async Task<User?> GetByMail(string email)
        {
            return await _userDal.GetAsync(u => u.Email.ToLower() == email.ToLower());
        }

        /// <inheritdoc/>
        public async Task<User?> GetById(int userId)
        {
            return await _userDal.GetByIdAsync(userId);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByGoogleId(string googleId)
        {
            return await _userDal.GetAsync(u => u.GoogleId == googleId);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByAppleId(string appleId)
        {
            return await _userDal.GetAsync(u => u.AppleId == appleId);
        }

        /// <inheritdoc/>
        public async Task Add(User user)
        {
            await _userDal.AddAsync(user);
        }

        /// <inheritdoc/>
        public async Task Update(User user)
        {
            await _userDal.UpdateAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<OperationClaim>> GetClaimsAsync(User user)
        {
            return await _userDal.GetClaimsAsync(user);
        }
    }
}
