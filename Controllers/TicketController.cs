
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Data;
using OrduNet.Web.Models.Entities;
using OrduNet.Web.Models.ViewModels;
using OrduNet.Web.Models.Constants;
using OrduNet.Web.Services;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace OrduNet.Web.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly OrduNetDbContext _context;
        private readonly IExcelService _excelService;
        private readonly IAuditService _auditService;

        public TicketController(OrduNetDbContext context, IExcelService excelService, IAuditService auditService)
        {
            _context = context;
            _excelService = excelService;
            _auditService = auditService;
        }

        private void LogAudit(string action, string entityName, string? entityId, string details)
        {
            _auditService.LogAudit(User.Identity?.Name ?? "Bilinmeyen", action, entityName, entityId, details, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        // 5. ARIZA TAKİP (HELPDESK / TICKET) YÖNETİMİ
        // ==========================================
        public async Task<IActionResult> Index(string? status, int page = 1)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var query = _context.IssueTickets
                .Include(t => t.AssignedTechnician)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "Tümü")
            {
                query = query.Where(t => t.Status == status);
            }

            ViewBag.SelectedStatus = status ?? "Tümü";
            ViewBag.TotalCount = await _context.IssueTickets.CountAsync();
            ViewBag.OpenCount = await _context.IssueTickets.CountAsync(t => t.Status == TicketStatuses.New);
            ViewBag.InProgressCount = await _context.IssueTickets.CountAsync(t => t.Status == TicketStatuses.InProgress);
            ViewBag.ResolvedCount = await _context.IssueTickets.CountAsync(t => t.Status == TicketStatuses.Resolved);

            // Arıza atamalarında seçilecek aktif teknik personeller
            ViewBag.Technicians = await _context.SupportTechnicians
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.IsAvailable) // Önce görevde olanlar
                .ThenBy(t => t.FullName)
                .ToListAsync();

            // Sayfalama (Pagination) mantığı
            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(tickets);
        }

        public async Task<IActionResult> ExportIssueTicketsExcel(string? status)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var query = _context.IssueTickets
                .Include(t => t.AssignedTechnician)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "Tümü")
            {
                query = query.Where(t => t.Status == status);
            }

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            
            var fileBytes = _excelService.ExportIssueTicketsToExcel(tickets);
            var fileName = $"Ariza_Talepleri_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            
            LogAudit("Excel Aktarımı", "IssueTicket", null, $"{tickets.Count} adet arıza talebi Excel'e aktarıldı.");
            
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTicketStatus(int id, string status, string? adminNotes)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var ticket = await _context.IssueTickets.FindAsync(id);
            if (ticket != null)
            {
                ticket.Status = status;
                ticket.AdminNotes = adminNotes;
                if (status == TicketStatuses.Resolved)
                {
                    ticket.ResolvedAt = DateTime.Now;
                }

                LogAudit("Arıza Güncelleme", "IssueTicket", id.ToString(), $"Talep #{id} durumu '{status}' yapıldı.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Talep #{id} durumu '{status}' olarak güncellendi.";
            }

            return RedirectToAction("Index");
        }

        // Arızayı Kendi Üzerine Al
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTicketToSelf(int id)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var ticket = await _context.IssueTickets.FindAsync(id);
            if (ticket == null)
                return NotFound();

            // Oturum açan kullanıcının adı
            var currentUserName = User.Identity?.Name ?? "Yetkili Personel";
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == currentUserName);
            var displayName = user?.FullName ?? currentUserName;

            // Teknisyen tablosunda bu isimle eşleşen varsa ID'sini bağla
            var matchedTech = await _context.SupportTechnicians
                .FirstOrDefaultAsync(t => t.IsActive && t.FullName == displayName);

            ticket.AssignedTechnicianId = matchedTech?.Id;
            ticket.AssignedToName = displayName;
            ticket.AssignedAt = DateTime.Now;

            if (ticket.Status == TicketStatuses.New)
            {
                ticket.Status = TicketStatuses.InProgress;
            }

            LogAudit("Arıza Üzerine Alma", "IssueTicket", id.ToString(), $"Talep #{id} ({displayName}) tarafından üzerine alındı.");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Talep #{id} başarıyla üzerinize alındı ve 'İşlemde' durumuna getirildi.";
            return RedirectToAction("Index");
        }

        // Arızayı Başka Saha Personeline Ata
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTicket(int id, int technicianId, string? notes)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var ticket = await _context.IssueTickets.FindAsync(id);
            var technician = await _context.SupportTechnicians.FindAsync(technicianId);

            if (ticket == null || technician == null)
                return NotFound();

            ticket.AssignedTechnicianId = technician.Id;
            ticket.AssignedToName = technician.FullName;
            ticket.AssignedAt = DateTime.Now;

            if (ticket.Status == TicketStatuses.New)
            {
                ticket.Status = TicketStatuses.InProgress;
            }

            if (!string.IsNullOrWhiteSpace(notes))
            {
                ticket.AdminNotes = string.IsNullOrWhiteSpace(ticket.AdminNotes)
                    ? $"[Atama Notu: {notes}]"
                    : $"{ticket.AdminNotes} | [Atama Notu: {notes}]";
            }

            LogAudit("Arıza Atama", "IssueTicket", id.ToString(), $"Talep #{id}, {technician.FullName} personeline atandı.");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Talep #{id}, {technician.FullName} personeline başarıyla atandı.";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 5.1. TEKNİK SERVİS EKİBİ & İZİN YÖNETİMİ
        // ==========================================
        public async Task<IActionResult> SupportTechnicians()
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var technicians = await _context.SupportTechnicians
                .Where(t => t.IsActive)
                .OrderBy(t => t.FullName)
                .ToListAsync();

            ViewBag.TicketCategories = await _context.TicketCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

            return View(technicians);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTechnicianAvailability(int id)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var tech = await _context.SupportTechnicians.FindAsync(id);
            if (tech != null)
            {
                tech.IsAvailable = !tech.IsAvailable;
                LogAudit("Teknisyen Durumu", "SupportTechnician", id.ToString(), $"{tech.FullName} durumu {(tech.IsAvailable ? "GÖREVDE" : "İZİNLİ")} yapıldı.");
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{tech.FullName} durumu '{(tech.IsAvailable ? "Görevde" : "İzinli")}' olarak güncellendi.";
            }

            return RedirectToAction(nameof(SupportTechnicians));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupportTechnician(SupportTechnician model, string[] SelectedSpecialties)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.IsActive = true;
                
                if (SelectedSpecialties != null && SelectedSpecialties.Length > 0)
                {
                    model.Specialties = string.Join(",", SelectedSpecialties);
                }

                _context.SupportTechnicians.Add(model);
                await _context.SaveChangesAsync();

                LogAudit("Teknisyen Ekleme", "SupportTechnician", model.Id.ToString(), $"{model.FullName} teknik ekibe eklendi.");
                TempData["SuccessMessage"] = $"{model.FullName} teknik ekibe başarıyla eklendi.";
                return RedirectToAction(nameof(SupportTechnicians));
            }

            TempData["ErrorMessage"] = "Lütfen personel ad soyad alanını doldurunuz.";
            return RedirectToAction(nameof(SupportTechnicians));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupportTechnician(int id, string FullName, string Title, string Phone, bool IsAvailable, string[] SelectedSpecialties)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var tech = await _context.SupportTechnicians.FindAsync(id);
            if (tech != null)
            {
                tech.FullName = FullName;
                tech.Title = Title;
                tech.Phone = Phone;
                tech.IsAvailable = IsAvailable;
                
                if (SelectedSpecialties != null && SelectedSpecialties.Length > 0)
                {
                    tech.Specialties = string.Join(",", SelectedSpecialties);
                }
                else
                {
                    tech.Specialties = null;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{tech.FullName} bilgileri güncellendi.";
            }

            return RedirectToAction(nameof(SupportTechnicians));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupportTechnician(int id)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var tech = await _context.SupportTechnicians.FindAsync(id);
            if (tech != null)
            {
                // Soft delete
                tech.IsActive = false;
                LogAudit("Teknisyen Silme", "SupportTechnician", id.ToString(), $"{tech.FullName} teknik ekipten çıkarıldı.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{tech.FullName} teknik ekipten silindi.";
            }

            return RedirectToAction(nameof(SupportTechnicians));
        }
        // ==========================================
        // 5.1. BİLET KATEGORİLERİ YÖNETİMİ
        // ==========================================
        public async Task<IActionResult> TicketCategories()
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var categories = await _context.TicketCategories.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicketCategory(TicketCategory model)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                model.IsActive = true;
                _context.TicketCategories.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kategori eklendi.";
            }
            return RedirectToAction(nameof(TicketCategories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTicketCategory(TicketCategory model)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var cat = await _context.TicketCategories.FindAsync(model.Id);
                if (cat != null)
                {
                    cat.Name = model.Name;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Kategori güncellendi.";
                }
            }
            return RedirectToAction(nameof(TicketCategories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTicketCategory(int id)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var cat = await _context.TicketCategories.FindAsync(id);
            if (cat != null)
            {
                cat.IsActive = !cat.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kategori durumu güncellendi.";
            }
            return RedirectToAction(nameof(TicketCategories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTicketCategory(int id)
        {
            if (!User.HasModulePermission(SystemModules.IssueTracker))
                return RedirectToAction("AccessDenied", "Account");

            var cat = await _context.TicketCategories.FindAsync(id);
            if (cat != null)
            {
                _context.TicketCategories.Remove(cat);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kategori silindi.";
            }
            return RedirectToAction(nameof(TicketCategories));
        }


        // ==========================================
        
    }
}
