using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Models.Entities;

namespace OrduNet.Web.Data
{
    public class OrduNetDbContext : DbContext
    {
        public OrduNetDbContext(DbContextOptions<OrduNetDbContext> options) : base(options)
        {
        }

        public DbSet<Unit> Units { get; set; }
        public DbSet<Personnel> Personnels { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<DailyDuty> DailyDuties { get; set; }
        public DbSet<CafeteriaMenu> CafeteriaMenus { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }
        public DbSet<HelpdeskFaq> HelpdeskFaqs { get; set; }
        public DbSet<TicketCategory> TicketCategories { get; set; }
        public DbSet<IssueTicket> IssueTickets { get; set; }
        public DbSet<SupportTechnician> SupportTechnicians { get; set; }
        public DbSet<EmergencyAlert> EmergencyAlerts { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Unit configuration
            modelBuilder.Entity<Unit>()
                .HasMany(u => u.PersonnelList)
                .WithOne(p => p.Unit)
                .HasForeignKey(p => p.UnitId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Unit>()
                .HasIndex(u => u.Category);

            // Personnel indexes for fast search
            modelBuilder.Entity<Personnel>()
                .HasIndex(p => p.FirstName);

            modelBuilder.Entity<Personnel>()
                .HasIndex(p => p.LastName);

            modelBuilder.Entity<Personnel>()
                .HasIndex(p => p.InternalNumber);

            modelBuilder.Entity<Personnel>()
                .HasIndex(p => p.Title);
        }
    }
}
