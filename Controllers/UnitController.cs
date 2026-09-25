
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
    public class UnitController : Controller
    {
        private readonly OrduNetDbContext _context;
        private readonly IExcelService _excelService;
        private readonly IAuditService _auditService;

        public UnitController(OrduNetDbContext context, IExcelService excelService, IAuditService auditService)
        {
            _context = context;
            _excelService = excelService;
            _auditService = auditService;
        }

        private void LogAudit(string action, string entityName, string? entityId, string details)
        {
            _auditService.LogAudit(User.Identity?.Name ?? "Bilinmeyen", action, entityName, entityId, details, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        // 3. BİRİM YÖNETİMİ
        // ==========================================
        public async Task<IActionResult> Index()
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            var units = await _context.Units
                .Include(u => u.PersonnelList)
                .OrderBy(u => u.Category)
                .ThenBy(u => u.DisplayOrder)
                .ThenBy(u => u.Name)
                .ToListAsync();

            return View(units);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUnit(Unit unit)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                _context.Units.Add(unit);
                LogAudit("Ekleme", "Unit", null, $"{unit.Name} birimi olüsturuldu.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{unit.Name} birimi eklendi.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            var unit = await _context.Units.Include(u => u.PersonnelList).FirstOrDefaultAsync(u => u.Id == id);
            if (unit != null)
            {
                if (unit.PersonnelList.Any())
                {
                    TempData["ErrorMessage"] = $"Bu birime bağl� {unit.PersonnelList.Count} Personel bulunmaktadır. Önce Personelleri aktarınız veya siliniz.";
                }
                else
                {
                    _context.Units.Remove(unit);
                    LogAudit("Silme", "Unit", id.ToString(), $"{unit.Name} birimi silindi.");
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{unit.Name} birimi silindi.";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditUnit(int id)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            var unit = await _context.Units.FindAsync(id);
            if (unit == null)
            {
                TempData["ErrorMessage"] = "Birim bulunamadı.";
                return RedirectToAction("Index");
            }
            return View(unit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUnit(int id, Unit unit)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            if (id != unit.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(unit);
                    LogAudit("Güncelleme", "Unit", id.ToString(), $"{unit.Name} birimi güncellendi.");
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Birim başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Units.Any(e => e.Id == id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction("Index");
            }
            return View(unit);
        }

        [HttpGet]
        public IActionResult DownloadUnitTemplate()
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            var fileBytes = _excelService.GenerateUnitTemplate();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Birim_Mahkeme_Sablonu.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadUnitsExcel(IFormFile? file)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen geçerli bir Excel dosyası (.xlsx veya .xls) seçiniz.";
                return RedirectToAction("Index");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls" && ext != ".xlsm")
            {
                TempData["ErrorMessage"] = "Yalnızca Excel (.xlsx veya .xls) formatındaki dosyaları yükleyebilirsiniz.";
                return RedirectToAction("Index");
            }

            using var stream = file.OpenReadStream();
            var (successCount, errors) = await _excelService.ImportUnitsFromExcelAsync(stream);

            if (successCount > 0)
            {
                LogAudit("Birim Excel Aktarımı", "Unit", null, $"Excel'den {successCount} adet birim/mahkeme başarıyla aktarıldı.");
                TempData["SuccessMessage"] = $"{successCount} adet birim / mahkeme kaydı başarıyla iilendi.";
            }

            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join("<br/>", errors.Take(5));
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        
    }
}

