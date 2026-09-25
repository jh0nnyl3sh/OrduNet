using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Data;
using OrduNet.Web.Models.Entities;
using OrduNet.Web.Models.ViewModels;
using OrduNet.Web.Services;

namespace OrduNet.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly OrduNetDbContext _context;

        public AccountController(OrduNetDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.AppUsers
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(u => u.Username == model.Username.Trim());

            if (user == null || !PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Bu kullanıcı hesabı devre dışı bırakılmıştır.");
                return View(model);
            }

            // Claim'leri oluştur
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.IsSuperAdmin ? "SuperAdmin" : "Editor")
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            // Modül yetkilerini claim olarak ekle
            foreach (var perm in user.Permissions.Where(p => p.CanManage))
            {
                claims.Add(new Claim("ModulePermission", perm.ModuleKey));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Giriş zamanını güncelle & Log kaydet
            user.LastLoginAt = DateTime.Now;
            _context.AuditLogs.Add(new AuditLog
            {
                UserName = user.Username,
                Action = "Giriş",
                EntityName = "AppUser",
                EntityId = user.Id.ToString(),
                Details = $"{user.FullName} yönetim paneline giriş yaptı.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name ?? "Bilinmeyen";

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            try
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = userName,
                    Action = "Çıkış",
                    EntityName = "AppUser",
                    Details = "Yönetim panelinden güvenli çıkış yapıldı.",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();
            }
            catch { }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == model.Username.Trim());
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Bu kullanıcı adı / sicil zaten sistemde kayıtlı.");
                return View(model);
            }

            var newUser = new AppUser
            {
                Username = model.Username.Trim(),
                FullName = model.FullName.Trim(),
                PasswordHash = PasswordHasher.HashPassword(model.Password),
                Unit = model.Unit.Trim(),
                Phone = model.Phone?.Trim(),
                RoomNumber = model.RoomNumber?.Trim(),
                IsActive = true,
                IsSuperAdmin = false, // Standart kullanıcı
                CreatedAt = DateTime.Now
            };

            _context.AppUsers.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Hesabınız başarıyla oluşturuldu. Giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                var user = await _context.AppUsers.FindAsync(userId);
                if (user != null)
                {
                    var model = new ProfileViewModel
                    {
                        FullName = user.FullName,
                        Unit = user.Unit ?? "",
                        Phone = user.Phone,
                        RoomNumber = user.RoomNumber
                    };
                    return View(model);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                var user = await _context.AppUsers.FindAsync(userId);
                if (user != null)
                {
                    user.FullName = model.FullName.Trim();
                    user.Unit = model.Unit.Trim();
                    user.Phone = model.Phone?.Trim();
                    user.RoomNumber = model.RoomNumber?.Trim();

                    if (!string.IsNullOrWhiteSpace(model.NewPassword))
                    {
                        user.PasswordHash = PasswordHasher.HashPassword(model.NewPassword);
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
                    
                    // Update claims could be done here by re-signing in, but for these fields it's not strictly necessary.
                }
            }

            return View(model);
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
