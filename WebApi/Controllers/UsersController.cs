using Asp.Versioning;
using Business.Abstract;
using Core.Utilities.Exceptions;
using Core.Utilities.Security.Hashing;
using Entities.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApi.Controllers
{
    /// <summary>
    /// Kullanıcı profil işlemlerini (Profil görüntüleme, güncelleme, şifre değiştirme) yöneten Controller.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>Giriş yapmış kullanıcının profil bilgilerini döner.</summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetById(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var profileDto = new UserProfileDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt
            };

            return Ok(new { success = true, data = profileDto });
        }

        /// <summary>Giriş yapmış kullanıcının ad ve soyad bilgilerini günceller.</summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileDto profileDto)
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetById(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            user.FirstName = profileDto.FirstName;
            user.LastName = profileDto.LastName;

            await _userService.Update(user);

            return Ok(new { success = true, message = "Profil bilgileri başarıyla güncellendi." });
        }

        /// <summary>Giriş yapmış kullanıcının şifresini değiştirir.</summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetById(userId);
            if (user == null || user.PasswordHash == null || user.PasswordSalt == null)
            {
                return BadRequest(new { success = false, message = "Kullanıcı veya şifre bilgisi bulunamadı." });
            }

            if (!HashingHelper.VerifyPasswordHash(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest(new { success = false, message = "Mevcut şifre hatalı." });
            }

            HashingHelper.CreatePasswordHash(dto.NewPassword, out byte[] newHash, out byte[] newSalt);
            user.PasswordHash = newHash;
            user.PasswordSalt = newSalt;

            await _userService.Update(user);

            return Ok(new { success = true, message = "Şifreniz başarıyla değiştirildi." });
        }

        /// <summary>Giriş yapmış kullanıcının hesabını siler (Soft Delete).</summary>
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetById(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            user.IsDeleted = true;
            user.Status = false;

            await _userService.Update(user);

            return Ok(new { success = true, message = "Hesabınız başarıyla silindi." });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == "userid")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            throw new UserFriendlyException("Oturum geçersiz. Lütfen tekrar giriş yapın.", ErrorCodes.AuthenticationFailed, StatusCodes.Status401Unauthorized);
        }
    }
}
