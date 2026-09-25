using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Data;
using OrduNet.Web.Models;
using OrduNet.Web.Models.Entities;
using OrduNet.Web.Models.ViewModels;
using OrduNet.Web.Models.Constants;
using Microsoft.AspNetCore.SignalR;
using OrduNet.Web.Hubs;

namespace OrduNet.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly OrduNetDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IHubContext<TicketHub> _ticketHub;

        public HomeController(OrduNetDbContext context, ILogger<HomeController> logger, IHubContext<TicketHub> ticketHub)
        {
            _context = context;
            _logger = logger;
            _ticketHub = ticketHub;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var announcements = await _context.Announcements
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.PublishDate)
                .Take(6)
                .ToListAsync();

            var duties = await _context.DailyDuties
                .Where(d => d.DutyDate >= today)
                .OrderBy(d => d.DutyDate)
                .Take(5)
                .ToListAsync();

            // Sadece bugünün tarihine ait yemek menüs�n� getir
            var todayMenu = await _context.CafeteriaMenus
                .FirstOrDefaultAsync(m => m.Date.Date == today);


            var emergencyAlert = await _context.EmergencyAlerts
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.UpdatedAt)
                .FirstOrDefaultAsync();

            var model = new HomeDashboardViewModel
            {
                Announcements = announcements,
                DailyDuties = duties,
                TodayMenu = todayMenu,
                TotalPersonnel = await _context.Personnels.CountAsync(p => p.IsActive),
                TotalUnits = await _context.Units.CountAsync(u => u.IsActive),
                ActiveEmergencyAlert = emergencyAlert
            };

            return View(model);
        }

        // GET: /Home/AnnouncementDetail/5 veya /Duyuru/5
        [HttpGet]
        [Route("Home/AnnouncementDetail/{id:int}")]
        [Route("Duyuru/{id:int}")]
        public async Task<IActionResult> AnnouncementDetail(int id)
        {
            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

            if (announcement == null)
            {
                // Giriş yapmış yetkili kullanıcılar pasif veya taslak duyuruları da görebilsin
                if (User.Identity?.IsAuthenticated == true)
                {
                    announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == id);
                }

                if (announcement == null)
                {
                    return NotFound();
                }
            }

            // Göruntülenme sayişini artır
            try
            {
                announcement.ViewCount++;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Duyuru göruntülenme sayişi artırillamadı. ID: {Id}", id);
            }

            // Yan sütunda gösterilecek diğer güncel duyurular
            var otherAnnouncements = await _context.Announcements
                .Where(a => a.IsActive && a.Id != id)
                .OrderByDescending(a => a.PublishDate)
                .Take(5)
                .ToListAsync();

            ViewBag.OtherAnnouncements = otherAnnouncements;

            return View(announcement);
        }


        // GET: /Home/SubmitIssueTicket
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SubmitIssueTicket()
        {
            ViewBag.Categories = await _context.TicketCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            
            var model = new CreateTicketViewModel();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    var user = await _context.AppUsers.FindAsync(userId);
                    if (user != null)
                    {
                        model.RequesterName = user.FullName;
                        model.RequesterUnit = user.Unit ?? "";
                        model.RequesterPhone = user.Phone ?? "";
                        model.RoomNumber = user.RoomNumber;
                    }
                }
            }

            return View(model);
        }

        // POST: /Home/SubmitIssueTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SubmitIssueTicket(CreateTicketViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Aynı kişiye ait, aynı konuda ağ�k/işlemde olan talep var m� kontrolü
                var hasActiveTicket = await _context.IssueTickets
                    .AnyAsync(t => t.RequesterName == model.RequesterName.Trim() 
                                && t.Category == model.Category 
                                && (t.Status == TicketStatuses.New || t.Status == TicketStatuses.InProgress));

                if (hasActiveTicket)
                {
                    ViewBag.Categories = await _context.TicketCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
                    TempData["ErrorMessage"] = "Bu konuda henüz sonuilanmam�� ağ�k bir arıza talebiniz bulunmaktadır. Mevcut talebiniz kapatılmadan aynı konuda yeni talep olüsturamazs�n�z.";
                    return View(model);
                }

                var ticket = new IssueTicket
                {
                    Title = model.Title.Trim(),
                    Description = model.Description.Trim(),
                    Category = model.Category,
                    RequesterName = model.RequesterName.Trim(),
                    RequesterUnit = model.RequesterUnit.Trim(),
                    RequesterPhone = model.RequesterPhone.Trim(),
                    RoomNumber = model.RoomNumber?.Trim(),
                    Status = TicketStatuses.New,
                    CreatedAt = DateTime.Now
                };

                // SMART ROUTING LOGIC (Otomatik Atama)
                var activeTechs = await _context.SupportTechnicians
                    .Where(t => t.IsActive && t.IsAvailable && t.Specialties != null && t.Specialties.Contains(model.Category))
                    .ToListAsync();

                if (activeTechs.Any())
                {
                    // 20 dakika bekleme kuralı kaldırıldı, rastgele uygün teknisyene atanır
                    var random = new Random();
                    var selectedTech = activeTechs[random.Next(activeTechs.Count)];
                    
                    ticket.AssignedTechnicianId = selectedTech.Id;
                    ticket.AssignedToName = selectedTech.FullName;
                    ticket.AssignedAt = DateTime.Now;
                    ticket.Status = TicketStatuses.InProgress; // Atandığı için direkt işlemde yapabiliriz
                }

                _context.IssueTickets.Add(ticket);
                await _context.SaveChangesAsync();

                // Yeni arıza bildirimi için SignalR Üzerinden yayın yap
                await _ticketHub.Clients.All.SendAsync("ReceiveTicketNotification", ticket.Id, ticket.RequesterName, ticket.Category);

                TempData["SuccessMessage"] = $"Arıza talebiniz #{ticket.Id} takip numarasıyla olüsturuldu. Bilgi İşlem ekibimiz en kısa sürede ilgilenecektir.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.TicketCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            TempData["ErrorMessage"] = "Lütfen arıza formundaki zorunlu alanları eksiksiz doldurunuz.";
            return View(model);
        }

        public async Task<IActionResult> About()
        {
            var faqs = await _context.HelpdeskFaqs
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();

            return View(faqs);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

