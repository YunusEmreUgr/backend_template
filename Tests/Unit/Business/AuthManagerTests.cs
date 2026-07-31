using Business.Abstract;
using Business.Concrete;
using Business.Constants;
using Core.Entities.Concrete.Users;
using Core.Utilities.Results;
using Core.Utilities.Security.JWT;
using Entities.Dtos.Auth;
using FluentAssertions;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Unit.Business
{
    /// <summary>
    /// AuthManager sınıfı için kurumsal standartlarda birim (unit) testleri.
    /// Bağımlılıklar Moq kütüphanesi ile taklit edilmiştir.
    /// </summary>
    public class AuthManagerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ITokenHelper> _tokenHelperMock;
        private readonly Mock<IUserOperationClaimService> _userOperationClaimServiceMock;
        private readonly AuthManager _authManager;

        public AuthManagerTests()
        {
            // 1. Bağımlılıkların taklit edilmesi (Mocking)
            _userServiceMock = new Mock<IUserService>();
            _tokenHelperMock = new Mock<ITokenHelper>();
            _userOperationClaimServiceMock = new Mock<IUserOperationClaimService>();

            // 2. Test edilecek asıl sınıfın taklit nesnelerle oluşturulması (SUT - System Under Test)
            _authManager = new AuthManager(
                _userServiceMock.Object, 
                _tokenHelperMock.Object, 
                _userOperationClaimServiceMock.Object);
        }

        [Fact]
        public async Task Register_ShouldCreateUser_WhenUserDoesNotExist()
        {
            // Arrange (Hazırlık)
            var registerDto = new UserForRegisterDto
            {
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "Password123!"
            };

            _userServiceMock.Setup(x => x.Add(It.IsAny<User>())).Returns(Task.CompletedTask);
            _userOperationClaimServiceMock.Setup(x => x.AddUserClaim(It.IsAny<int>())).Returns(Task.CompletedTask);

            // Act (Eylem)
            var result = await _authManager.Register(registerDto, registerDto.Password);

            // Assert (Doğrulama)
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Email.Should().Be(registerDto.Email);
            result.Data.FirstName.Should().Be(registerDto.FirstName);
            result.Data.LastName.Should().Be(registerDto.LastName);

            // Veritabanı ekleme ve rol atama servislerinin 1 kere çağrıldığından emin ol
            _userServiceMock.Verify(x => x.Add(It.IsAny<User>()), Times.Once);
            _userOperationClaimServiceMock.Verify(x => x.AddUserClaim(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task UserExists_ShouldReturnError_WhenUserAlreadyExists()
        {
            // Arrange
            var email = "existing@example.com";
            var existingUser = new User { Email = email };
            _userServiceMock.Setup(x => x.GetByMail(email)).ReturnsAsync(existingUser);

            // Act
            var result = await _authManager.UserExists(email);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be(Messages.UserAlreadyExists);
        }

        [Fact]
        public async Task UserExists_ShouldReturnSuccess_WhenUserDoesNotExist()
        {
            // Arrange
            var email = "new@example.com";
            _userServiceMock.Setup(x => x.GetByMail(email)).ReturnsAsync((User?)null);

            // Act
            var result = await _authManager.UserExists(email);

            // Assert
            result.Success.Should().BeTrue();
        }
    }
}
