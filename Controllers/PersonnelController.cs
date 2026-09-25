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
using Microsoft.AspNetCore.Http;

namespace OrduNet.Web.Controllers
{
    [Authorize]
    public class PersonnelController : Controller
    {
        private readonly OrduNetDbContext _context;
        private readonly IExcelService _excelService;
        private readonly IAuditService _auditService;

        public PersonnelController(OrduNetDbContext context, IExcelService excelService, IAuditService auditService)
        {
            _context = context;
            _excelService = excelService;
            _auditService = auditService;
        }

        private void LogAudit(string action, string entityName, string? entityId, string details)
        {
            _auditService.LogAudit(User.Identity?.Name ?? "Bilinmeyen", action, entityName, entityId, details, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        public async Task<IActionResult> Index(string? search, int? unitId)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            var query = _context.Personnels
                .Include(p => p.Unit)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p =>
                    p.FirstName.Contains(s) ||
                    p.LastName.Contains(s) ||
                    p.Title.Contains(s) ||
                    p.InternalNumber.Contains(s) ||
                    (p.RoomNumber != null && p.RoomNumber.Contains(s)));
            }

            if (unitId.HasValue && unitId.Value > 0)
            {
                query = query.Where(p => p.UnitId == unitId.Value);
            }

            ViewBag.Units = await _context.Units.OrderBy(u => u.Name).ToListAsync();
            ViewBag.Search = search;
            ViewBag.SelectedUnitId = unitId;

            var list = await query
                .OrderBy(p => p.Unit != null ? p.Unit.Name : "")
                .ThenBy(p => p.DisplayOrder)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            return View(list);
        }

        public async Task<IActionResult> CreatePersonnel()
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            ViewBag.Units = new SelectList(await _context.Units.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
            return View(new Personnel { IsActive = true, DisplayOrder = 10 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePersonnel(Personnel model)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                _context.Personnels.Add(model);
                await _context.SaveChangesAsync();

                LogAudit("Ekleme", "Personnel", model.Id.ToString(), $"{model.FullName} rehbere eklendi. Dahili: {model.InternalNumber}");
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{model.FullName} başarıyla rehbere eklendi.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Units = new SelectList(await _context.Units.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", model.UnitId);
            return View(model);
        }

        public async Task<IActionResult> EditPersonnel(int id)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            var personnel = await _context.Personnels.FindAsync(id);
            if (personnel == null)
            {
                return NotFound();
            }

            ViewBag.Units = new SelectList(await _context.Units.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", personnel.UnitId);
            return View(personnel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPersonnel(Personnel model)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                _context.Personnels.Update(model);
                LogAudit("Güncelleme", "Personnel", model.Id.ToString(), $"{model.FullName} bilgileri güncellendi.");
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{model.FullName} bilgileri güncellendi.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Units = new SelectList(await _context.Units.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", model.UnitId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePersonnel(int id)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            var personnel = await _context.Personnels.FindAsync(id);
            if (personnel != null)
            {
                personnel.IsActive = false;
                LogAudit("Silme (Soft Delete)", "Personnel", id.ToString(), $"{personnel.FullName} rehberden silindi.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{personnel.FullName} rehberden silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult DownloadTemplate()
        {
            var fileBytes = _excelService.GenerateTemplate();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ordu_Adliyesi_Rehber_Sablon.xlsx");
        }

        public async Task<IActionResult> ExportExcel()
        {
            var list = await _context.Personnels.Include(p => p.Unit).Where(p => p.IsActive).ToListAsync();
            var fileBytes = _excelService.ExportDirectoryToExcel(list);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Ordu_Adliyesi_Rehber_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public IActionResult ExcelImport()
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcelImport(IFormFile? excelFile)
        {
            if (!User.HasModulePermission(SystemModules.Directory))
                return RedirectToAction("AccessDenied", "Account");

            if (excelFile == null || excelFile.Length == 0)
            {
                ModelState.AddModelError("", "Lütfen geçerli bir Excel (.xlsx) dosyası seçiniz.");
                return View();
            }

                        if (excelFile.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                ModelState.AddModelError("", "Geğersiz dosya formatı. Lütfen gerçek bir Excel (.xlsx) dosyası yükleyin.");
                return View();
            }
            if (!excelFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Yalnızca Excel 2007+ (.xlsx) formatındaki dosyalar desteklenmektedir.");
                return View();
            }

            using var stream = excelFile.OpenReadStream();
            var (successCount, errors) = await _excelService.ImportPersonnelFromExcelAsync(stream);

            ViewBag.SuccessCount = successCount;
            ViewBag.Errors = errors;

            if (successCount > 0)
            {
                LogAudit("Excel Aktarımı", "Personnel", null, $"Excel dosyasından {successCount} adet Personel toplu aktarıldı.");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{successCount} Personel kaydı başarıyla aktarıldı.";
            }

            return View();
        }
    }
}

