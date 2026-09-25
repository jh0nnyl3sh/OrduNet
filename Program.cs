using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Data;
using OrduNet.Web.Services;
using OrduNet.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı ve Servis Kayıtlarç
builder.Services.AddDbContext<OrduNetDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Kimlik Doğrulama (Cookie Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "OrduNet.Auth";
        options.Cookie.HttpOnly = true;
    });

// Add MVC Controllers & Views
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Türkçe Karakterlerin ve Unicode Metinlerin HTML Entity Olarak Bozulmasını önle
builder.Services.AddSingleton<System.Text.Encodings.Web.HtmlEncoder>(
    System.Text.Encodings.Web.HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All));

// Türkçe Kültür ve Dil Ayarları
var turkishCulture = new System.Globalization.CultureInfo("tr-TR");
turkishCulture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
turkishCulture.DateTimeFormat.LongDatePattern = "dd MMMM yyyy, dddd";

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(turkishCulture),
    SupportedCultures = new[] { turkishCulture },
    SupportedUICultures = new[] { turkishCulture }
});

// 2. Veritabanı Bağlatma ve Seed Verişi Yükleme
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<OrduNetDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı bağlatçlçrken hata olüstu.");
    }
}

// 3. HTTP üstek Hattı Yapılandçrmasç
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<TicketHub>("/ticketHub");

app.Run();

