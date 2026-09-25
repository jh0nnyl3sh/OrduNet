using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Data;
using OrduNet.Web.Models.Entities;
using OrduNet.Web.Models.ViewModels;
using OrduNet.Web.Services;
using OrduNet.Web.Models.Constants;

namespace OrduNet.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly OrduNetDbContext _context;
        private readonly IExcelService _excelService;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private readonly IAuditService _auditService;

        public AdminController(OrduNetDbContext context, IExcelService excelService, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, IAuditService auditService)
        {
            _context = context;
            _excelService = excelService;
            _cache = cache;
            _auditService = auditService;
        }

        private void LogAudit(string action, string entityName, string? entityId, string details)
        {
            _auditService.LogAudit(User.Identity?.Name ?? "Bilinmeyen", action, entityName, entityId, details, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        // ==========================================
        // 1. DASHBOARD
        // ==========================================
        public async Task<IActionResult> Index()
        {
            ViewBag.PersonnelCount = await _context.Personnels.CountAsync();
            ViewBag.UnitCount = await _context.Units.CountAsync();
            ViewBag.AnnouncementCount = await _context.Announcements.CountAsync();
            ViewBag.DutyCount = await _context.DailyDuties.CountAsync();
            ViewBag.TicketCount = await _context.IssueTickets.CountAsync(t => t.Status == TicketStatuses.New || t.Status == TicketStatuses.InProgress);
            ViewBag.UserCount = await _context.AppUsers.CountAsync();

            var recentPersonnel = await _context.Personnels
                .Include(p => p.Unit)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentTickets = await _context.IssueTickets
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.EmergencyAlert = await _context.EmergencyAlerts.FirstOrDefaultAsync();

            return View(recentPersonnel);
        }

        // ==========================================
        // 6. ACİL DURUM / KIRMIZI BANT YÖNETİMİ
        // ==========================================

        public async Task<IActionResult> EmergencyAlert()
        {
            if (!User.HasModulePermission(SystemModules.Announcements))
                return RedirectToAction("AccessDenied", "Account");

            var alert = await _context.EmergencyAlerts.FirstOrDefaultAsync() ?? new EmergencyAlert();
            return View(alert);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmergencyAlert(EmergencyAlert model)
        {
            if (!User.HasModulePermission(SystemModules.Announcements))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var existing = await _context.EmergencyAlerts.FirstOrDefaultAsync();
                if (existing == null)
                {
                    model.UpdatedAt = DateTime.Now;
                    model.UpdatedBy = User.Identity?.Name ?? "Admin";
                    _context.EmergencyAlerts.Add(model);
                }
                else
                {
                    existing.Title = model.Title;
                    existing.Message = model.Message;
                    existing.AlertLevel = model.AlertLevel;
                    existing.IsActive = model.IsActive;
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = User.Identity?.Name ?? "Admin";
                }

                LogAudit("Acil Bant Güncelleme", "EmergencyAlert", null, $"Acil durum bandı durumu: {(model.IsActive ? "AÇIK" : "KAPALI")}");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Acil durum duyuru şeridi ayarları kaydedildi.";
                return RedirectToAction(nameof(EmergencyAlert));
            }

            return View(model);
        }

        // ==========================================
        // 6.2. DUYURU & HABER YÖNETİMİ
        // ==========================================
        public async Task<IActionResult> Announcements(string? search, string? Category)
        {
            if (!User.HasModulePermission(SystemModules.Announcements))
                return RedirectToAction("AccessDenied", "Account");

            var query = _context.Announcements.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(a => a.Title.Contains(s) || a.Summary.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(Category) && Category != "Tümü")
            {
                query = query.Where(a => a.Category == Category);
            }

            ViewBag.Search = search;
            ViewBag.SelectedCategory = Category ?? "Tümü";
            ViewBag.Categories = await _context.Announcements.Select(a => a.Category).Distinct().ToListAsync();
            ViewBag.TotalCount = await _context.Announcements.CountAsync();
            ViewBag.ActiveCount = await _context.Announcements.CountAsync(a => a.IsActive);

            var list = await query.OrderByDescending(a => a.PublishDate).ToListAsync();
            return View(list);
        }

        public IActionResult CreateAnnouncement()
        {
            if (!User.HasModulePermission(SystemModules.Announcements))
                return RedirectToAction("AccessDenied", "Account");

            return View(new Announcement { PublishDate = DateTime.Now, IsActive = true, Category = "DUYURU", BadgeClass = "badge-danger" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(Announcement model)
        {
            if (!User.HasModulePermission(SystemModules.Announcements))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(model.BadgeClass))
                {
                    model.BadgeClass = model.Category switch
                    {
                        "DUYURU" => "badge-danger",
                        "HABER" => "badge-success",
                        "BİLİŞİM" => "badge-primary",
                        "İDARİ İŞLER" => "badge-warning",
                        _ => "badge-primary"
                    };
                }

                _context.Announcements.Add(model);
                LogAudit("Duyuru Ekleme", "Announcement", null, $"'{model.Title}' başlıklı yeni duyuru yayınlandı.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Duyuru başarıyla eklendi.";
                return RedirectToAction(nameof(Announcements));
            }

            return View(model);
        }

        public async Task<IActionResult> EditAnnouncement(int id)
        {
            if (!User.HasModulePermission(SystemModules.Announcements))
                return RedirectToAction("AccessDenied", "Account");

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
                return NotFound();

            return View(announcement);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAnnouncement(Announcement model)
        {
            if (!User.HasModulePermission(SystemModules.Announcements))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var existing = await _context.Announcements.FindAsync(model.Id);
                if (existing == null)
                    return NotFound();

                existing.Title = model.Title;
                existing.Summary = model.Summary;
                existing.Content = model.Content;
                existing.Category = model.Category;
                existing.BadgeClass = model.BadgeClass;
                existing.PublishDate = model.PublishDate;
                existing.IsActive = model.IsActive;

                LogAudit("Duyuru Güncelleme", "Announcement", model.Id.ToString(), $"Duyuru #{model.Id} güncellendi.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Duyuru başarıyla güncellendi.";
                return RedirectToAction(nameof(Announcements));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            if (!User.HasModulePermission(SystemModules.Announcements))
                return RedirectToAction("AccessDenied", "Account");

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
                LogAudit("Duyuru Silme", "Announcement", id.ToString(), $"'{announcement.Title}' başlıklı duyuru silindi.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Duyuru başarıyla silindi.";
            }

            return RedirectToAction(nameof(Announcements));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAnnouncementStatus(int id)
        {
            if (!User.HasModulePermission(SystemModules.Announcements))
                return RedirectToAction("AccessDenied", "Account");

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                announcement.IsActive = !announcement.IsActive;
                LogAudit("Duyuru Durumu", "Announcement", id.ToString(), $"Duyuru #{id} durumu {(announcement.IsActive ? "YAYINDA" : "PASİF")} yapıldı.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Duyuru durumu {(announcement.IsActive ? "yayına alındı" : "yayından kaldırıldı")}.";
            }

            return RedirectToAction(nameof(Announcements));
        }

        // ==========================================
        // 7. KULLANICI & YETKİ YÖNETİMİ (Super ADMİN)
        // ==========================================
        public async Task<IActionResult> Users()
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var users = await _context.AppUsers
                .Include(u => u.Permissions)
                .OrderByDescending(u => u.IsSuperAdmin)
                .ThenBy(u => u.Username)
                .ToListAsync();

            return View(users);
        }

        public IActionResult CreateUser()
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var existing = await _context.AppUsers.AnyAsync(u => u.Username == model.Username.Trim());
                if (existing)
                {
                    ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılmaktadır.");
                    return View(model);
                }

                var user = new AppUser
                {
                    Username = model.Username.Trim(),
                    FullName = model.FullName.Trim(),
                    Title = model.Title,
                    Email = model.Email,
                    PasswordHash = PasswordHasher.HashPassword(model.Password),
                    IsActive = model.IsActive,
                    IsSuperAdmin = model.IsSuperAdmin,
                    CreatedAt = DateTime.Now
                };

                _context.AppUsers.Add(user);
                await _context.SaveChangesAsync();

                // Modül yetkilerini kaydet
                if (model.IsSuperAdmin)
                {
                    foreach (var module in SystemModules.ModuleNames.Keys)
                    {
                        _context.UserPermissions.Add(new UserPermission
                        {
                            UserId = user.Id,
                            ModuleKey = module,
                            CanManage = true
                        });
                    }
                }
                else
                {
                    foreach (var module in model.SelectedModules)
                    {
                        _context.UserPermissions.Add(new UserPermission
                        {
                            UserId = user.Id,
                            ModuleKey = module,
                            CanManage = true
                        });
                    }
                }

                LogAudit("Kullanıcı Oluşturma", "AppUser", user.Id.ToString(), $"{user.Username} adlı yeni kullanıcı oluşturuldu.");
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{user.FullName} kullanıcısı oluşturuldu.";
                return RedirectToAction(nameof(Users));
            }

            return View(model);
        }

        public async Task<IActionResult> EditUser(int id)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var user = await _context.AppUsers
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Title = user.Title,
                Email = user.Email,
                IsActive = user.IsActive,
                IsSuperAdmin = user.IsSuperAdmin,
                SelectedModules = user.Permissions.Where(p => p.CanManage).Select(p => p.ModuleKey).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var user = await _context.AppUsers
                    .Include(u => u.Permissions)
                    .FirstOrDefaultAsync(u => u.Id == model.Id);

                if (user == null)
                {
                    return NotFound();
                }

                user.FullName = model.FullName.Trim();
                user.Title = model.Title;
                user.Email = model.Email;
                user.IsActive = model.IsActive;
                user.IsSuperAdmin = model.IsSuperAdmin;

                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    user.PasswordHash = PasswordHasher.HashPassword(model.NewPassword);
                }

                // Yetkileri güncelle
                _context.UserPermissions.RemoveRange(user.Permissions);

                if (model.IsSuperAdmin)
                {
                    foreach (var module in SystemModules.ModuleNames.Keys)
                    {
                        _context.UserPermissions.Add(new UserPermission
                        {
                            UserId = user.Id,
                            ModuleKey = module,
                            CanManage = true
                        });
                    }
                }
                else
                {
                    foreach (var module in model.SelectedModules)
                    {
                        _context.UserPermissions.Add(new UserPermission
                        {
                            UserId = user.Id,
                            ModuleKey = module,
                            CanManage = true
                        });
                    }
                }

                LogAudit("Kullanıcı Güncelleme", "AppUser", user.Id.ToString(), $"{user.Username} kullanıcısının bilgileri ve yetkileri güncellendi.");
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{user.FullName} kullanıcısının bilgileri güncellendi.";
                return RedirectToAction(nameof(Users));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var user = await _context.AppUsers.FindAsync(id);
            if (user != null && user.Username != "admin")
            {
                user.IsActive = !user.IsActive;
                LogAudit("Durum Güncelleme", "AppUser", id.ToString(), $"{user.Username} hesabı {(user.IsActive ? "AKTİF" : "PASİF")} yapıldı.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{user.FullName} hesabı {(user.IsActive ? "aktif" : "pasif")} duruma getirildi.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var user = await _context.AppUsers.FindAsync(id);
            if (user != null && user.Username != "admin")
            {
                var permissions = await _context.UserPermissions.Where(p => p.UserId == id).ToListAsync();
                _context.UserPermissions.RemoveRange(permissions);
                _context.AppUsers.Remove(user);
                LogAudit("Kullanıcı Silme", "AppUser", id.ToString(), $"{user.Username} kullanıcısı sistemden silindi.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{user.FullName} kullanıcısı başarıyla silindi.";
            }

            return RedirectToAction(nameof(Users));
        }
        // ==========================================
        // NÖBETÇİ BİRİMLER (DAILY DUTIES) YÖNETİMİ
        // ==========================================
        public async Task<IActionResult> DailyDuties()
        {
            if (!User.HasModulePermission(SystemModules.Duties))
                return RedirectToAction("AccessDenied", "Account");

            var duties = await _context.DailyDuties
                .OrderByDescending(d => d.DutyDate)
                .ThenBy(d => d.DutyType)
                .ToListAsync();

            return View(duties);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDailyDuty(DailyDuty duty)
        {
            if (!User.HasModulePermission(SystemModules.Duties))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                _context.DailyDuties.Add(duty);
                LogAudit("Ekleme", "DailyDuty", null, $"{duty.DutyType} nöbet kaydı oluşturuldu.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Nöbet kaydı başarıyla eklendi.";
            }
            return RedirectToAction(nameof(DailyDuties));
        }

        [HttpGet]
        public async Task<IActionResult> EditDailyDuty(int id)
        {
            if (!User.HasModulePermission(SystemModules.Duties))
                return RedirectToAction("AccessDenied", "Account");

            var duty = await _context.DailyDuties.FindAsync(id);
            if (duty == null)
            {
                TempData["ErrorMessage"] = "Nöbet kaydı bulunamadı.";
                return RedirectToAction(nameof(DailyDuties));
            }
            return View(duty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDailyDuty(int id, DailyDuty duty)
        {
            if (!User.HasModulePermission(SystemModules.Duties))
                return RedirectToAction("AccessDenied", "Account");

            if (id != duty.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(duty);
                    LogAudit("Güncelleme", "DailyDuty", id.ToString(), $"{duty.DutyType} nöbet kaydı güncellendi.");
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Nöbet kaydı başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DailyDuties.Any(e => e.Id == id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(DailyDuties));
            }
            return View(duty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDailyDuty(int id)
        {
            if (!User.HasModulePermission(SystemModules.Duties))
                return RedirectToAction("AccessDenied", "Account");

            var duty = await _context.DailyDuties.FindAsync(id);
            if (duty != null)
            {
                _context.DailyDuties.Remove(duty);
                LogAudit("Silme", "DailyDuty", id.ToString(), $"{duty.DutyType} nöbet kaydı silindi.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Nöbet kaydı silindi.";
            }
            return RedirectToAction(nameof(DailyDuties));
        }


        // ==========================================
        // 8. TEK TIKLA VERİTABANI YEDEKLEME (SNAPSHOT)
        // ==========================================
        public async Task<IActionResult> BackupDatabase()
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var backupData = new
            {
                BackupDate = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "SuperAdmin",
                System = "OrduNet Bilişim Portalı",
                Units = await _context.Units.AsNoTracking().ToListAsync(),
                Personnels = await _context.Personnels.AsNoTracking().ToListAsync(),
                Announcements = await _context.Announcements.AsNoTracking().ToListAsync(),
                DailyDuties = await _context.DailyDuties.AsNoTracking().ToListAsync(),
                CafeteriaMenus = await _context.CafeteriaMenus.AsNoTracking().ToListAsync(),
                IssueTickets = await _context.IssueTickets.AsNoTracking().ToListAsync(),
                EmergencyAlerts = await _context.EmergencyAlerts.AsNoTracking().ToListAsync()
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var jsonString = JsonSerializer.Serialize(backupData, jsonOptions);
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

            LogAudit("Yedekleme", "Database", null, "Sistem veritabanı tam JSON yedeği indirildi.");
            await _context.SaveChangesAsync();

            var fileName = $"OrduNet_Yedek_{DateTime.Now:yyyyMMdd_HHmm}.json";
            return File(fileBytes, "application/json", fileName);
        }

        // ==========================================
        // 9. SİSTEM DENETİM GÜNLÜKLERİ (AUDIT LOGS)
        // ==========================================
        public async Task<IActionResult> AuditLogs()
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(200)
                .ToListAsync();

            return View(logs);
        }

        // ==========================================
        // 10. SİSTEM AYARLARI (Super ADMİN)
        // ==========================================
        public async Task<IActionResult> SiteSettings()
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var settings = await _context.SiteSettings.OrderBy(s => s.GroupName).ToListAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(Dictionary<int, string> settingsData)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            foreach (var kvp in settingsData)
            {
                var setting = await _context.SiteSettings.FindAsync(kvp.Key);
                if (setting != null)
                {
                    setting.Value = kvp.Value;
                }
            }

            LogAudit("Sistem Ayarları", "SiteSettings", null, "Sistem ayarları güncellendi.");
            await _context.SaveChangesAsync();
            
            _cache.Remove("SiteSettingsCache");

            TempData["SuccessMessage"] = "Sistem ayarları başarıyla güncellendi.";
            return RedirectToAction(nameof(SiteSettings));
        }

        // ==========================================
        // 11. HIZLI YARDIM / SSS (Super ADMİN)
        // ==========================================
        public async Task<IActionResult> HelpdeskFaqs()
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var faqs = await _context.HelpdeskFaqs.OrderBy(f => f.DisplayOrder).ToListAsync();
            return View(faqs);
        }

        public IActionResult CreateFaq()
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");
            return View(new HelpdeskFaq());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFaq(HelpdeskFaq faq)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                _context.HelpdeskFaqs.Add(faq);
                await _context.SaveChangesAsync();
                LogAudit("SSS Ekleme", "HelpdeskFaq", faq.Id.ToString(), $"Yeni Hızlı Yardım (SSS) Eklendi: {faq.Question}");
                TempData["SuccessMessage"] = "Kayıt başarıyla eklendi.";
                return RedirectToAction(nameof(HelpdeskFaqs));
            }
            return View(faq);
        }

        public async Task<IActionResult> EditFaq(int id)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");
            var faq = await _context.HelpdeskFaqs.FindAsync(id);
            if (faq == null) return NotFound();
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFaq(HelpdeskFaq faq)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                _context.Update(faq);
                await _context.SaveChangesAsync();
                LogAudit("SSS Güncelleme", "HelpdeskFaq", faq.Id.ToString(), $"Hızlı Yardım (SSS) Güncellendi: {faq.Question}");
                TempData["SuccessMessage"] = "Kayıt başarıyla güncellendi.";
                return RedirectToAction(nameof(HelpdeskFaqs));
            }
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFaq(int id)
        {
            if (!User.IsSuperAdmin())
                return RedirectToAction("AccessDenied", "Account");
            
            var faq = await _context.HelpdeskFaqs.FindAsync(id);
            if (faq != null)
            {
                _context.HelpdeskFaqs.Remove(faq);
                await _context.SaveChangesAsync();
                LogAudit("SSS Silme", "HelpdeskFaq", id.ToString(), $"Hızlı Yardım (SSS) Silindi: {faq.Question}");
                TempData["SuccessMessage"] = "Kayıt başarıyla silindi.";
            }
            return RedirectToAction(nameof(HelpdeskFaqs));
        }
    }
}

