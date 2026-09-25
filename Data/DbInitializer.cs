using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Models.Entities;

namespace OrduNet.Web.Data
{
    public static class DbInitializer
    {
        public static void Initialize(OrduNetDbContext context)
        {
            context.Database.EnsureCreated();

            // Yeni Modiler Tabloları Oluştur (Mevcut veritabanında henüz yoksa)
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppUsers' AND xtype='U')
                    BEGIN
                        CREATE TABLE [AppUsers] (
                            [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [Username] nvarchar(50) NOT NULL,
                            [PasswordHash] nvarchar(255) NOT NULL,
                            [FullName] nvarchar(100) NOT NULL,
                            [Title] nvarchar(100) NULL,
                            [Email] nvarchar(100) NULL,
                            [IsActive] bit NOT NULL DEFAULT 1,
                            [IsSuperAdmin] bit NOT NULL DEFAULT 0,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
                            [LastLoginAt] datetime2 NULL
                        );
                        CREATE UNIQUE INDEX [IX_AppUsers_Username] ON [AppUsers] ([Username]);
                    END

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserPermissions' AND xtype='U')
                    BEGIN
                        CREATE TABLE [UserPermissions] (
                            [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [UserId] int NOT NULL,
                            [ModuleKey] nvarchar(50) NOT NULL,
                            [CanManage] bit NOT NULL DEFAULT 1,
                            CONSTRAINT [FK_UserPermissions_AppUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AppUsers] ([Id]) ON DELETE CASCADE
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='IssueTickets' AND xtype='U')
                    BEGIN
                        CREATE TABLE [IssueTickets] (
                            [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [Title] nvarchar(150) NOT NULL,
                            [Description] nvarchar(1000) NOT NULL,
                            [Category] nvarchar(50) NOT NULL,
                            [RequesterName] nvarchar(100) NOT NULL,
                            [RequesterUnit] nvarchar(150) NOT NULL,
                            [RequesterPhone] nvarchar(20) NOT NULL,
                            [RoomNumber] nvarchar(50) NULL,
                            [Status] nvarchar(30) NOT NULL DEFAULT 'Yeni',
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
                            [ResolvedAt] datetime2 NULL,
                            [AdminNotes] nvarchar(500) NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EmergencyAlerts' AND xtype='U')
                    BEGIN
                        CREATE TABLE [EmergencyAlerts] (
                            [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [Title] nvarchar(150) NOT NULL,
                            [Message] nvarchar(500) NOT NULL,
                            [AlertLevel] nvarchar(20) NOT NULL DEFAULT 'danger',
                            [IsActive] bit NOT NULL DEFAULT 0,
                            [UpdatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
                            [UpdatedBy] nvarchar(100) NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
                    BEGIN
                        CREATE TABLE [AuditLogs] (
                            [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [UserName] nvarchar(50) NOT NULL,
                            [Action] nvarchar(50) NOT NULL,
                            [EntityName] nvarchar(100) NOT NULL,
                            [EntityId] nvarchar(max) NULL,
                            [Details] nvarchar(1000) NULL,
                            [Timestamp] datetime2 NOT NULL DEFAULT GETDATE(),
                            [IpAddress] nvarchar(50) NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SupportTechnicians' AND xtype='U')
                    BEGIN
                        CREATE TABLE [SupportTechnicians] (
                            [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [FullName] nvarchar(100) NOT NULL,
                            [Title] nvarchar(100) NULL,
                            [Phone] nvarchar(50) NULL,
                            [IsAvailable] bit NOT NULL DEFAULT 1,
                            [IsActive] bit NOT NULL DEFAULT 1,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SiteSettings' AND xtype='U')
                    BEGIN
                        CREATE TABLE [SiteSettings] (
                            [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [Key] nvarchar(100) NOT NULL,
                            [Value] nvarchar(max) NOT NULL,
                            [Description] nvarchar(200) NULL,
                            [GroupName] nvarchar(50) NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HelpdeskFaqs' AND xtype='U')
                    BEGIN
                        CREATE TABLE [HelpdeskFaqs] (
                            [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [Question] nvarchar(200) NOT NULL,
                            [Answer] nvarchar(max) NOT NULL,
                            [IconClass] nvarchar(50) NOT NULL,
                            [DisplayOrder] int NOT NULL DEFAULT 0,
                            [IsActive] bit NOT NULL DEFAULT 1
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='IssueTickets' AND COLUMN_NAME='AssignedTechnicianId')
                    BEGIN
                        ALTER TABLE [IssueTickets] ADD [AssignedTechnicianId] int NULL;
                        ALTER TABLE [IssueTickets] ADD [AssignedToName] nvarchar(100) NULL;
                        ALTER TABLE [IssueTickets] ADD [AssignedAt] datetime2 NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TicketCategories' AND xtype='U')
                    BEGIN
                        CREATE TABLE [TicketCategories] (
                            [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [Name] nvarchar(100) NOT NULL,
                            [IsActive] bit NOT NULL DEFAULT 1
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='SupportTechnicians' AND COLUMN_NAME='Specialties')
                    BEGIN
                        ALTER TABLE [SupportTechnicians] ADD [Specialties] nvarchar(500) NULL;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AppUsers' AND COLUMN_NAME='Unit')
                    BEGIN
                        ALTER TABLE [AppUsers] ADD [Unit] nvarchar(100) NULL;
                        ALTER TABLE [AppUsers] ADD [Phone] nvarchar(50) NULL;
                        ALTER TABLE [AppUsers] ADD [RoomNumber] nvarchar(50) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CafeteriaMenus' AND COLUMN_NAME='FullMenuText')
                    BEGIN
                        ALTER TABLE [CafeteriaMenus] ADD [FullMenuText] nvarchar(max) NULL;
                    END
                ");

            }
            catch { }

            // Varsayılan Super Admin Kontrolü
            try
            {
                if (!context.AppUsers.Any())
                {
                    var adminUser = new AppUser
                    {
                        Username = "admin",
                        FullName = "Sistem Yöneticişi",
                        Title = "Bilgi İşlem şefi / Super Admin",
                        Email = "bilgiişlem.ordu@adalet.gov.tr",
                        PasswordHash = Services.PasswordHasher.HashPassword("OrduNet.2026!"), // parolayı hashle
                        IsActive = true,
                        IsSuperAdmin = true,
                        CreatedAt = DateTime.Now
                    };
                    context.AppUsers.Add(adminUser);
                    context.SaveChanges();

                    // Super admin için tüm modül yetkileri
                    foreach (var module in SystemModules.ModuleNames.Keys)
                    {
                        context.UserPermissions.Add(new UserPermission
                        {
                            UserId = adminUser.Id,
                            ModuleKey = module,
                            CanManage = true
                        });
                    }
                    context.SaveChanges();
                }

                // Varsayılan Acil Durum Kaydı
                if (!context.EmergencyAlerts.Any())
                {
                    context.EmergencyAlerts.Add(new EmergencyAlert
                    {
                        Title = "Acil Duyuru Sistemi",
                        Message = "Sistem genelinde kritik bir durum olduğunda bu şerit aktif edilerek adliye Personeli anl�k bilgilendirilebilir.",
                        AlertLevel = "danger",
                        IsActive = false,
                        UpdatedAt = DateTime.Now,
                        UpdatedBy = "Sistem"
                    });
                    context.SaveChanges();
                }
            }
            catch { }

            // Veritabanındaki olası bozuk test kayıtlar�n� temizle ve doğru Türkçe karakterlerle güncelle
            SanitizeCorruptedData(context);

            if (context.Units.Any())
            {
                return; // Rehber verileri zaten dolu
            }

            // 1. BİRİMLER (Units)
            var units = new List<Unit>
            {
                // Cumhuriyet Bağsavcıl���
                new Unit { Name = "Cumhuriyet Bağsavcıl��� Makamı", Category = "Cumhuriyet Bağsavcıl���", DisplayOrder = 1, Location = "3. Kat - Protokol / A Blok" },
                new Unit { Name = "Cumhuriyet Bağsavcı Vekillişi", Category = "Cumhuriyet Bağsavcıl���", DisplayOrder = 2, Location = "3. Kat - A Blok" },
                new Unit { Name = "Cumhuriyet Savcıları çalışma Odaları", Category = "Cumhuriyet Bağsavcıl���", DisplayOrder = 3, Location = "3. Kat - B Blok" },
                new Unit { Name = "CBS Sorüsturma & Genel Sorüsturma Bürosu", Category = "Cumhuriyet Bağsavcıl���", DisplayOrder = 4, Location = "3. Kat - Kalem 305" },
                new Unit { Name = "CBS infaz ve ilamat Bürosu", Category = "Cumhuriyet Bağsavcıl���", DisplayOrder = 5, Location = "Zemin Kat - 012" },
                new Unit { Name = "CBS �n Büro & Dan��ma", Category = "Cumhuriyet Bağsavcıl���", DisplayOrder = 6, Location = "Zemin Kat - Giriş Ana Hol" },
                new Unit { Name = "CBS Uzlağt�rma Bürosu", Category = "Cumhuriyet Bağsavcıl���", DisplayOrder = 7, Location = "2. Kat - 215" },

                // Ceza Mahkemeleri
                new Unit { Name = "1. A��r Ceza Mahkemesi", Category = "Ceza Mahkemeleri", DisplayOrder = 10, Location = "2. Kat - Salon 1 / Kalem 201" },
                new Unit { Name = "2. A��r Ceza Mahkemesi", Category = "Ceza Mahkemeleri", DisplayOrder = 11, Location = "2. Kat - Salon 2 / Kalem 203" },
                new Unit { Name = "1. Asliye Ceza Mahkemesi", Category = "Ceza Mahkemeleri", DisplayOrder = 12, Location = "1. Kat - Salon 3 / Kalem 105" },
                new Unit { Name = "2. Asliye Ceza Mahkemesi", Category = "Ceza Mahkemeleri", DisplayOrder = 13, Location = "1. Kat - Salon 4 / Kalem 107" },
                new Unit { Name = "3. Asliye Ceza Mahkemesi", Category = "Ceza Mahkemeleri", DisplayOrder = 14, Location = "1. Kat - Salon 5 / Kalem 109" },
                new Unit { Name = "Sulh Ceza Hâkimlişi", Category = "Ceza Mahkemeleri", DisplayOrder = 15, Location = "Zemin Kat - Salon 6 / Kalem 018" },
                new Unit { Name = "infaz Hâkimlişi", Category = "Ceza Mahkemeleri", DisplayOrder = 16, Location = "1. Kat - Kalem 112" },

                // Hukuk Mahkemeleri
                new Unit { Name = "1. Asliye Hukuk Mahkemesi", Category = "Hukuk Mahkemeleri", DisplayOrder = 20, Location = "2. Kat - Kalem 220" },
                new Unit { Name = "2. Asliye Hukuk Mahkemesi", Category = "Hukuk Mahkemeleri", DisplayOrder = 21, Location = "2. Kat - Kalem 222" },
                new Unit { Name = "1. Aile Mahkemesi", Category = "Hukuk Mahkemeleri", DisplayOrder = 22, Location = "2. Kat - Kalem 228" },
                new Unit { Name = "1. Sulh Hukuk Mahkemesi", Category = "Hukuk Mahkemeleri", DisplayOrder = 23, Location = "1. Kat - Kalem 125" },
                new Unit { Name = "�� Mahkemesi", Category = "Hukuk Mahkemeleri", DisplayOrder = 24, Location = "1. Kat - Kalem 130" },
                new Unit { Name = "İcra Hukuk & İcra Ceza Mahkemesi", Category = "Hukuk Mahkemeleri", DisplayOrder = 25, Location = "Zemin Kat - Kalem 022" },

                // İcra & İflas Daireleri
                new Unit { Name = "Ordu İcra Dairesi (Genel İcra)", Category = "İcra & İflas", DisplayOrder = 30, Location = "Zemin Kat - Doğu Kanadı" },
                new Unit { Name = "İcra Satış� & ilan Bürosu", Category = "İcra & İflas", DisplayOrder = 31, Location = "Zemin Kat - Kalem 035" },

                // İdari & Teknik Birimler
                new Unit { Name = "Adalet Komisyonu Bağkanl���", Category = "İdari Birimler", DisplayOrder = 40, Location = "3. Kat - Komisyon Kalemi 310" },
                new Unit { Name = "Bilgi İşlem şube Müdürl���", Category = "İdari Birimler", DisplayOrder = 41, Location = "Zemin Kat - Teknik Servis 008" },
                new Unit { Name = "İdari İşler şube Müdürl���", Category = "İdari Birimler", DisplayOrder = 42, Location = "Zemin Kat - 005" },
                new Unit { Name = "Adli Destek ve Mağdur Hizmetleri Müd. (ADM)", Category = "İdari Birimler", DisplayOrder = 43, Location = "Zemin Kat - 015" },
                new Unit { Name = "Adli Sicil şefliği (Sabıka Kaydı)", Category = "İdari Birimler", DisplayOrder = 44, Location = "Giriş Kat - Ana Dan��ma Kar��s�" },
                new Unit { Name = "Santral & Güvenlik Amirlişi", Category = "İdari Birimler", DisplayOrder = 45, Location = "Zemin Kat - Güvenlik Kontrol" },
                new Unit { Name = "Vezne & Emanet Memurluğu", Category = "İdari Birimler", DisplayOrder = 46, Location = "Zemin Kat - 002" }
            };

            context.Units.AddRange(units);
            context.SaveChanges();

            // 2. Personel / DAH�L� LÜSTES� (Gerçekçi Ordu Adliyesi Kadrosu)
            var pList = new List<Personnel>();

            // Bağsavcılık
            var uCbsMakam = units.First(u => u.Name.Contains("Cumhuriyet Bağsavcıl��� Makamı"));
            var uCbsVekil = units.First(u => u.Name.Contains("Cumhuriyet Bağsavcı Vekillişi"));
            var uCbsKalem = units.First(u => u.Name.Contains("CBS Sorüsturma"));
            var uCbsInfaz = units.First(u => u.Name.Contains("CBS infaz"));
            var uCbsOnBüro = units.First(u => u.Name.Contains("CBS �n Büro"));

            pList.Add(new Personnel { FirstName = "Hasan", LastName = "BİLGİN", Title = "Cumhuriyet Bağsavcıs�", UnitId = uCbsMakam.Id, InternalNumber = "1001", RoomNumber = "301", Floor = "3. Kat", Email = "hasan.bilgin@adalet.gov.tr", Description = "Cumhuriyet Bağsavcıl��� Makamı" });
            pList.Add(new Personnel { FirstName = "Aylin", LastName = "DEMİR", Title = "Yazı İşleri Müdür� (Özel Kalem)", UnitId = uCbsMakam.Id, InternalNumber = "1002", RoomNumber = "302", Floor = "3. Kat", Email = "aylin.demir@adalet.gov.tr" });
            pList.Add(new Personnel { FirstName = "Murat", LastName = "YILDIZ", Title = "Zabıt Kâtibi (Özel Kalem)", UnitId = uCbsMakam.Id, InternalNumber = "1003", RoomNumber = "302", Floor = "3. Kat" });
            
            pList.Add(new Personnel { FirstName = "Serkan", LastName = "KAYA", Title = "Cumhuriyet Bağsavcı Vekili", UnitId = uCbsVekil.Id, InternalNumber = "1010", RoomNumber = "303", Floor = "3. Kat", Email = "serkan.kaya@adalet.gov.tr" });
            pList.Add(new Personnel { FirstName = "Zeynep", LastName = "ŞAHİN", Title = "Zabıt Kâtibi", UnitId = uCbsVekil.Id, InternalNumber = "1011", RoomNumber = "304", Floor = "3. Kat" });

            pList.Add(new Personnel { FirstName = "Mehmet", LastName = "ÖZTÜRK", Title = "Yazı İşleri Müdür�", UnitId = uCbsKalem.Id, InternalNumber = "1020", RoomNumber = "305", Floor = "3. Kat", Description = "Genel Sorüsturma Kalem şefi" });
            pList.Add(new Personnel { FirstName = "Elif", LastName = "ÇELİK", Title = "Zabıt Kâtibi", UnitId = uCbsKalem.Id, InternalNumber = "1021", RoomNumber = "305", Floor = "3. Kat" });
            pList.Add(new Personnel { FirstName = "Ahmet", LastName = "AYDIN", Title = "Zabıt Kâtibi", UnitId = uCbsKalem.Id, InternalNumber = "1022", RoomNumber = "305", Floor = "3. Kat" });
            pList.Add(new Personnel { FirstName = "Kemal", LastName = "GÜNEŞ", Title = "M�bağir", UnitId = uCbsKalem.Id, InternalNumber = "1025", RoomNumber = "305", Floor = "3. Kat" });

            pList.Add(new Personnel { FirstName = "Fatih", LastName = "YILMAZ", Title = "Yazı İşleri Müdür�", UnitId = uCbsInfaz.Id, InternalNumber = "1030", RoomNumber = "012", Floor = "Zemin Kat" });
            pList.Add(new Personnel { FirstName = "Merve", LastName = "KOŞ", Title = "Zabıt Kâtibi", UnitId = uCbsInfaz.Id, InternalNumber = "1031", RoomNumber = "012", Floor = "Zemin Kat" });
            pList.Add(new Personnel { FirstName = "Burak", LastName = "ASLAN", Title = "Zabıt Kâtibi", UnitId = uCbsInfaz.Id, InternalNumber = "1032", RoomNumber = "012", Floor = "Zemin Kat" });

            pList.Add(new Personnel { FirstName = "Sevgi", LastName = "POLAT", Title = "Zabıt Kâtibi", UnitId = uCbsOnBüro.Id, InternalNumber = "1040", RoomNumber = "Dan��ma", Floor = "Zemin Kat", Description = "Evrak Kayıt & Teslim" });
            pList.Add(new Personnel { FirstName = "Deniz", LastName = "KILIÇ", Title = "Zabıt Kâtibi", UnitId = uCbsOnBüro.Id, InternalNumber = "1041", RoomNumber = "Dan��ma", Floor = "Zemin Kat" });

            // 1. A��r Ceza Mahkemesi
            var uAgir1 = units.First(u => u.Name.Contains("1. A��r Ceza"));
            pList.Add(new Personnel { FirstName = "Erdem", LastName = "YAVUZ", Title = "Mahkeme Bağkan�", UnitId = uAgir1.Id, InternalNumber = "1100", RoomNumber = "201", Floor = "2. Kat", Email = "erdem.yavuz@adalet.gov.tr" });
            pList.Add(new Personnel { FirstName = "Gülfem", LastName = "AKSOY", Title = "Üye Hâkim", UnitId = uAgir1.Id, InternalNumber = "1101", RoomNumber = "202", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Tolga", LastName = "ÖZKAN", Title = "Üye Hâkim", UnitId = uAgir1.Id, InternalNumber = "1102", RoomNumber = "202", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Sibel", LastName = "ERDEM", Title = "Yazı İşleri Müdür�", UnitId = uAgir1.Id, InternalNumber = "1103", RoomNumber = "203", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Cem", LastName = "KORKMAZ", Title = "Zabıt Kâtibi (Duruşma)", UnitId = uAgir1.Id, InternalNumber = "1104", RoomNumber = "203", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Yasemin", LastName = "GÜL", Title = "Zabıt Kâtibi (Kalem)", UnitId = uAgir1.Id, InternalNumber = "1105", RoomNumber = "203", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "�mer", LastName = "TURAN", Title = "M�bağir", UnitId = uAgir1.Id, InternalNumber = "1106", RoomNumber = "Duruşma Salonu 1", Floor = "2. Kat" });

            // 2. A��r Ceza Mahkemesi
            var uAgir2 = units.First(u => u.Name.Contains("2. A��r Ceza"));
            pList.Add(new Personnel { FirstName = "Müstafa", LastName = "ÇETİN", Title = "Mahkeme Bağkan�", UnitId = uAgir2.Id, InternalNumber = "1110", RoomNumber = "204", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Banu", LastName = "TEKİN", Title = "Yazı İşleri Müdür�", UnitId = uAgir2.Id, InternalNumber = "1112", RoomNumber = "205", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Hakan", LastName = "AVCI", Title = "Zabıt Kâtibi", UnitId = uAgir2.Id, InternalNumber = "1113", RoomNumber = "205", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Emre", LastName = "CAN", Title = "M�bağir", UnitId = uAgir2.Id, InternalNumber = "1115", RoomNumber = "Duruşma Salonu 2", Floor = "2. Kat" });

            // 1. Asliye Ceza Mahkemesi
            var uAsliyeCeza1 = units.First(u => u.Name.Contains("1. Asliye Ceza"));
            pList.Add(new Personnel { FirstName = "Ay�e", LastName = "KARA", Title = "Hâkim", UnitId = uAsliyeCeza1.Id, InternalNumber = "1120", RoomNumber = "104", Floor = "1. Kat" });
            pList.Add(new Personnel { FirstName = "Selim", LastName = "DURMAZ", Title = "Yazı İşleri Müdür�", UnitId = uAsliyeCeza1.Id, InternalNumber = "1121", RoomNumber = "105", Floor = "1. Kat" });
            pList.Add(new Personnel { FirstName = "Ebru", LastName = "BAŞAR", Title = "Zabıt Kâtibi", UnitId = uAsliyeCeza1.Id, InternalNumber = "1122", RoomNumber = "105", Floor = "1. Kat" });
            pList.Add(new Personnel { FirstName = "Yakup", LastName = "ÜZER", Title = "M�bağir", UnitId = uAsliyeCeza1.Id, InternalNumber = "1124", RoomNumber = "105", Floor = "1. Kat" });

            // Sulh Ceza Hâkimlişi
            var uSulhCeza = units.First(u => u.Name.Contains("Sulh Ceza"));
            pList.Add(new Personnel { FirstName = "Kenan", LastName = "YALÇIN", Title = "Sulh Ceza Hâkimi", UnitId = uSulhCeza.Id, InternalNumber = "1140", RoomNumber = "017", Floor = "Zemin Kat" });
            pList.Add(new Personnel { FirstName = "Filiz", LastName = "TÜRKER", Title = "Yazı İşleri Müdür�", UnitId = uSulhCeza.Id, InternalNumber = "1141", RoomNumber = "018", Floor = "Zemin Kat" });
            pList.Add(new Personnel { FirstName = "Sinan", LastName = "AKTA�", Title = "Zabıt Kâtibi (Nöbet)", UnitId = uSulhCeza.Id, InternalNumber = "1142", RoomNumber = "018", Floor = "Zemin Kat" });

            // 1. Asliye Hukuk Mahkemesi
            var uAsliyeHukuk1 = units.First(u => u.Name.Contains("1. Asliye Hukuk"));
            pList.Add(new Personnel { FirstName = "Gökhan", LastName = "SÖNMEZ", Title = "Hâkim", UnitId = uAsliyeHukuk1.Id, InternalNumber = "1200", RoomNumber = "219", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Hacer", LastName = "DOĞAN", Title = "Yazı İşleri Müdür�", UnitId = uAsliyeHukuk1.Id, InternalNumber = "1201", RoomNumber = "220", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Tuğba", LastName = "KESKİN", Title = "Zabıt Kâtibi", UnitId = uAsliyeHukuk1.Id, InternalNumber = "1202", RoomNumber = "220", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "İsmail", LastName = "AL�", Title = "M�bağir", UnitId = uAsliyeHukuk1.Id, InternalNumber = "1205", RoomNumber = "220", Floor = "2. Kat" });

            // 1. Aile Mahkemesi
            var uAile1 = units.First(u => u.Name.Contains("1. Aile"));
            pList.Add(new Personnel { FirstName = "Nurten", LastName = "BAKIR", Title = "Hâkim", UnitId = uAile1.Id, InternalNumber = "1220", RoomNumber = "227", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Ceyda", LastName = "GÜLER", Title = "Yazı İşleri Müdür�", UnitId = uAile1.Id, InternalNumber = "1221", RoomNumber = "228", Floor = "2. Kat" });
            pList.Add(new Personnel { FirstName = "Bar��", LastName = "ŞEN", Title = "Zabıt Kâtibi", UnitId = uAile1.Id, InternalNumber = "1222", RoomNumber = "228", Floor = "2. Kat" });

            // İcra Dairesi
            var uİcra = units.First(u => u.Name.Contains("Ordu İcra Dairesi"));
            pList.Add(new Personnel { FirstName = "Zafer", LastName = "ERSOY", Title = "İcra Müdür�", UnitId = uİcra.Id, InternalNumber = "1300", RoomNumber = "İcra Müd. Odası", Floor = "Zemin Kat", Email = "zafer.ersoy@adalet.gov.tr" });
            pList.Add(new Personnel { FirstName = "Serap", LastName = "ULU", Title = "İcra Müdür Yardımcıs�", UnitId = uİcra.Id, InternalNumber = "1301", RoomNumber = "030", Floor = "Zemin Kat" });
            pList.Add(new Personnel { FirstName = "Okan", LastName = "YILDIZ", Title = "İcra Kâtibi (Haciz Masası)", UnitId = uİcra.Id, InternalNumber = "1302", RoomNumber = "031", Floor = "Zemin Kat" });
            pList.Add(new Personnel { FirstName = "Dilek", LastName = "MUTLU", Title = "İcra Kâtibi (Maağ Haciz)", UnitId = uİcra.Id, InternalNumber = "1303", RoomNumber = "031", Floor = "Zemin Kat" });
            pList.Add(new Personnel { FirstName = "Cihan", LastName = "GÖK", Title = "İcra Kâtibi (Esas Kayıt)", UnitId = uİcra.Id, InternalNumber = "1304", RoomNumber = "032", Floor = "Zemin Kat" });

            // Adalet Komisyonu Bağkanl���
            var uKomisyon = units.First(u => u.Name.Contains("Adalet Komisyonu"));
            pList.Add(new Personnel { FirstName = "Adnan", LastName = "KOŞE", Title = "Adalet Komisyonu Bağkan�", UnitId = uKomisyon.Id, InternalNumber = "1400", RoomNumber = "308", Floor = "3. Kat" });
            pList.Add(new Personnel { FirstName = "Selçuk", LastName = "TÜRKMEN", Title = "Komisyon Yazı İşleri Müdür�", UnitId = uKomisyon.Id, InternalNumber = "1401", RoomNumber = "310", Floor = "3. Kat" });
            pList.Add(new Personnel { FirstName = "Fatma", LastName = "ÖZKAN", Title = "Zabıt Kâtibi (özlük & Atama)", UnitId = uKomisyon.Id, InternalNumber = "1402", RoomNumber = "310", Floor = "3. Kat" });

            // Bilgi İşlem şube Müdürl���
            var uBilgiIslem = units.First(u => u.Name.Contains("Bilgi İşlem"));
            pList.Add(new Personnel { FirstName = "Ali", LastName = "YILMAZ", Title = "Bilgi İşlem şube Müdür�", UnitId = uBilgiIslem.Id, InternalNumber = "1500", RoomNumber = "008", Floor = "Zemin Kat", Email = "ordu.bilgiişlem@adalet.gov.tr", Description = "Bilişim Sistemleri & A� Sorumlusu" });
            pList.Add(new Personnel { FirstName = "Serhat", LastName = "KILIÇ", Title = "Bilgisayar Muhendişi", UnitId = uBilgiIslem.Id, InternalNumber = "1501", RoomNumber = "008", Floor = "Zemin Kat", Description = "Sistem & A� Yöneticişi" });
            pList.Add(new Personnel { FirstName = "Tarık", LastName = "ERDOĞAN", Title = "Teknisyen (Donanım / Yazıc�)", UnitId = uBilgiIslem.Id, InternalNumber = "1502", RoomNumber = "008", Floor = "Zemin Kat", Description = "Arıza & Bak�m Masası" });
            pList.Add(new Personnel { FirstName = "Hilal", LastName = "SEZGİN", Title = "Zabıt Kâtibi (UYAP Destek)", UnitId = uBilgiIslem.Id, InternalNumber = "1503", RoomNumber = "008", Floor = "Zemin Kat", Description = "UYAP & E-imza İşlemleri" });

            // İdari İşler şube Müdürl��� & Santral
            var uİdari = units.First(u => u.Name.Contains("İdari İşler"));
            var uSantral = units.First(u => u.Name.Contains("Santral & Güvenlik"));
            pList.Add(new Personnel { FirstName = "Kemal", LastName = "GÜLERYÜZ", Title = "İdari İşler şube Müdür�", UnitId = uİdari.Id, InternalNumber = "1600", RoomNumber = "005", Floor = "Zemin Kat" });
            pList.Add(new Personnel { FirstName = "Vedat", LastName = "UZUN", Title = "Maağ Mutemedi", UnitId = uİdari.Id, InternalNumber = "1601", RoomNumber = "005", Floor = "Zemin Kat" });
            pList.Add(new Personnel { FirstName = "Nuran", LastName = "DEMİRTAŞ", Title = "Aynıyat Saymanı", UnitId = uİdari.Id, InternalNumber = "1602", RoomNumber = "006", Floor = "Zemin Kat" });

            pList.Add(new Personnel { FirstName = "Genel Santral", LastName = "Operatörü", Title = "Santral Memuru", UnitId = uSantral.Id, InternalNumber = "1700", InternalNumber2 = "1701", RoomNumber = "Santral", Floor = "Giriş Holü", Description = "D�� Hat Aktarma & Bilgilendirme" });
            pList.Add(new Personnel { FirstName = "Recep", LastName = "POLAT", Title = "Güvenlik Amiri", UnitId = uSantral.Id, InternalNumber = "1710", RoomNumber = "Güvenlik", Floor = "Giriş", Description = "Nizamiye & Kamera Takip" });

            context.Personnels.AddRange(pList);

            // 3. DUYURULAR VE HABERLER (Bak�rk�y Adliyesi benzeri kartlar)
            var announcements = new List<Announcement>
            {
                new Announcement
                {
                    Title = "2026 Yılı Ordu Adliyesi indirim Anlağmal� Sağl�k Kuruluşları",
                    Summary = "Adliye Personelimiz ve birinci derece yakınları için Özel Ordu Medikal ve Göz Hastanelerinde %25 kurumsal indirim anlağmas� yenilenmiştir.",
                    Content = "Cumhuriyet Bağsavcıl���m�z ile anlağmal� sağl�k kuruluşları aras�nda yapılan protokol gereği, kurum kimlik kartı ibraz edilerek tüm muayene ve tahlillerde %25 indirim uygulanacaktır.",
                    Category = "DUYURU",
                    BadgeClass = "badge-danger",
                    PublishDate = DateTime.Now.AddDays(-2),
                    ViewCount = 142
                },
                new Announcement
                {
                    Title = "UYAP Bilişim Sistemi ve E-imza Sürüm Güncellemesi",
                    Summary = "Tüm mahkeme ve savcılık kalemlerimizde kullanılan UYAP Edit�r ve Akis kart s�r�cileri 18 Eylül Cuma ak�am� 19:00 itibarıyla güncellenecektir.",
                    Content = "Bilgi İşlem şube Müdürl���m�zce y�r�tilecek çalışma süresince intranet ve UYAP sistemlerinde kısa süreli kesintiler yağanabilir. Açık kalan duruşma ve evraklarının kaydedilmesi Önemle rica olunur.",
                    Category = "B�L���M",
                    BadgeClass = "badge-primary",
                    PublishDate = DateTime.Now.AddDays(-4),
                    ViewCount = 289
                },
                new Announcement
                {
                    Title = "2026 Yılı Adalet Lojmanları Bağvuru ve Puanlama Duyurusu",
                    Summary = "Cumhuriyet Bağsavcıl��� Lojman Tahsis Komisyonu 2026 yılı lojman talepleri için bağvuru takvimi ve talep formları yayımlanmıştır.",
                    Content = "Lojman bağvuru dilekçelerinin ve ekli belgelerin 30 Eylül mesai bitimine kadar İdari İşler şube Müdürl��� Lojman Bürosuna elden teslim edilmesi gerekmektedir.",
                    Category = "İDARİ İŞLER",
                    BadgeClass = "badge-warning",
                    PublishDate = DateTime.Now.AddDays(-7),
                    ViewCount = 415
                },
                new Announcement
                {
                    Title = "Adliye Kütüphanesi Yeni Hukuk ve içtihat Eserleri Yayında",
                    Summary = "Zemin katta yer alan Ordu Adliyesi Merkez Kütüphanemize 2026 yılı güncel mevzuat ve Yargıtay içtihat külliyatları eklenmiştir.",
                    Content = "Hâkim, Cumhuriyet Savcışi ve adliye Personelimiz mesai saatleri içerişinde kütüphanemizden ve dijital katalog tarama bilgisayarlarından istifade edebilirler.",
                    Category = "HABER",
                    BadgeClass = "badge-success",
                    PublishDate = DateTime.Now.AddDays(-10),
                    ViewCount = 98
                }
            };

            context.Announcements.AddRange(announcements);

            // 4. GÜNÜN NÖBET BİLGİLERİ (DailyDuty)
            var duties = new List<DailyDuty>
            {
                new DailyDuty { DutyDate = DateTime.Today, DutyType = "Nöbet�i A��r Ceza Mahkemesi", DutyOfficerOrUnit = "2. A��r Ceza Mahkemesi", ContactInfo = "Dahili: 1112 (Kalem) / Salon 2", Location = "2. Kat" },
                new DailyDuty { DutyDate = DateTime.Today, DutyType = "Nöbet�i Asliye Ceza Mahkemesi", DutyOfficerOrUnit = "1. Asliye Ceza Mahkemesi", ContactInfo = "Dahili: 1121 (Kalem) / Salon 3", Location = "1. Kat" },
                new DailyDuty { DutyDate = DateTime.Today, DutyType = "Nöbet�i Sulh Ceza Hâkimlişi", DutyOfficerOrUnit = "Sulh Ceza Hâkimlişi (Hâkim K. Yalçın)", ContactInfo = "Dahili: 1141 / 1142", Location = "Zemin Kat - 018" },
                new DailyDuty { DutyDate = DateTime.Today, DutyType = "Nöbet�i Cumhuriyet Savcışi", DutyOfficerOrUnit = "Cumhuriyet Savcışi Ersin KILIÇ", ContactInfo = "Dahili: 1015 / Cep Nöbet: 05xx xxx xx xx", Location = "3. Kat - Oda 306" },
                new DailyDuty { DutyDate = DateTime.Today, DutyType = "Nöbet�i Noterlik", DutyOfficerOrUnit = "Ordu 2. Noterlişi (Atat�rk Bulvar�)", ContactInfo = "0452 214 xx xx", Location = "Merkez / Ordu" }
            };

            context.DailyDuties.AddRange(duties);

            // 5. YEMEK MENÜS� (CafeteriaMenu)
            var menus = new List<CafeteriaMenu>
            {
                new CafeteriaMenu { Date = DateTime.Today, DayName = "�arızamba", Soup = "Ezogelin Çorbas�", MainDish = "Fırın Güveç / Tas Kebabı", SideDish = "Pirinç Pilavı", DessertOrSalad = "Mevsim Salata & Ayran", Calories = 840 },
                new CafeteriaMenu { Date = DateTime.Today.AddDays(1), DayName = "Perşembe", Soup = "Mercimek Çorbas�", MainDish = "İzmir Köfte", SideDish = "Bulgur Pilavı", DessertOrSalad = "Cevizli Baklava", Calories = 920 },
                new CafeteriaMenu { Date = DateTime.Today.AddDays(2), DayName = "Cuma", Soup = "Yayla Çorbas�", MainDish = "Piliçi Rosto & Püre", SideDish = "Soslu Makarna", DessertOrSalad = "Kemalpağa Tatlıs�", Calories = 790 }
            };

            context.CafeteriaMenus.AddRange(menus);

            context.SaveChanges();
        }

        private static void SanitizeCorruptedData(OrduNetDbContext context)
        {
            try
            {
                // 1. Acil Durum şeridi Kontrolü ve Onarımı
                var alert = context.EmergencyAlerts.FirstOrDefault(a => a.Id == 1);
                if (alert != null)
                {
                    alert.Title = "KRİTİK SİSTEM DUYURUSU";
                    alert.Message = "UYAP ve A� altyap�s�nda saat 17:30 - 18:00 aras�nda planlı bak�m yapılacakt�r.";
                    alert.UpdatedAt = DateTime.Now;
                }

                // 2. Duyurular (ID 5 ve varsa diğer bozuk test kayıtlar�)
                var announcements = context.Announcements.ToList();
                foreach (var ann in announcements)
                {
                    if (ann.Title.Contains("�") || ann.Title.Contains("�") || ann.Title.Contains("�") ||
                        ann.Summary.Contains("�") || ann.Summary.Contains("�") || ann.Summary.Contains("�"))
                    {
                        if (ann.Title.Contains("Yeni Adli") || ann.Title.Contains("Adli Y") || ann.Id == 5)
                        {
                            ann.Title = "Yeni Adli Yıl Açılışı Töreni ve Tebrik Duyurusu";
                            ann.Summary = "2026-2027 Adli Yılı ağ�l�� programı Hâkim, Savcı ve Personelimizin katılımıyla gerçekleştirilecektir.";
                            ann.Content = "Tüm adliye çalışanlarımızın yeni adli yılın� tebrik eder, adalet hizmetlerimizin etkin, hızlı ve tarafsöz bir şekilde y�r�tilece�i başarıl� bir adli çalışma yılı dileriz.";
                            ann.Category = "HABER";
                            ann.BadgeClass = "badge-success";
                        }
                        else
                        {
                            context.Announcements.Remove(ann);
                        }
                    }
                }

                // 3. Arıza Talepleri (ID 1 ve bozuk karakterli kayıtlar)
                var tickets = context.IssueTickets.ToList();
                foreach (var ticket in tickets)
                {
                    if (ticket.RequesterUnit.Contains("�") || ticket.RequesterUnit.Contains("�") || ticket.RequesterUnit.Contains("�") ||
                        ticket.Category.Contains("�") || ticket.Category.Contains("�") || ticket.Category.Contains("�") ||
                        ticket.Title.Contains("�") || ticket.Title.Contains("�") || ticket.Title.Contains("�") || ticket.Id == 1)
                    {
                        ticket.RequesterUnit = "1. A��r Ceza Mahkemesi";
                        ticket.Category = "Donanım / PC";
                        ticket.Title = "Monitör A��lm�yor";
                        ticket.Description = "Power tuşuna bas�ld���nda ekrana sinyal gelmiyor.";
                    }
                }

                // 4. Kullanıcılar
                var users = context.AppUsers.ToList();
                foreach (var user in users)
                {
                    if (user.Username == "fatma.çelik")
                    {
                        user.FullName = "Fatma çelik";
                        user.Title = "Yazı İşleri Müdür�";
                    }
                }

                // 5. Denetim Günlükleri
                var badLogs = context.AuditLogs.ToList().Where(l => l.Details != null && (l.Details.Contains("�") || l.Details.Contains("�") || l.Details.Contains("�"))).ToList();
                if (badLogs.Any())
                {
                    context.AuditLogs.RemoveRange(badLogs);
                }

                // 6. Saha / Destek Teknisyenleri Seed Verişi
                if (!context.SupportTechnicians.Any())
                {
                    context.SupportTechnicians.AddRange(
                        new SupportTechnician { FullName = "Murat YILMAZ", Title = "Bilgisayar Muhendişi", Phone = "Dahili: 1050", IsAvailable = true, Specialties = "Donanım / Bilgisayar,A� / internet" },
                        new SupportTechnician { FullName = "Emre KAYA", Title = "Bilgi İşlem Teknisyeni", Phone = "Dahili: 1051", IsAvailable = true, Specialties = "Donanım / Bilgisayar,Yazıc� / Tarayıcı,Diğer Konu" },
                        new SupportTechnician { FullName = "Serkan AYDIN", Title = "A� & Donanım Sorumlusu", Phone = "Dahili: 1052", IsAvailable = true, Specialties = "A� / internet,Donanım / Bilgisayar" },
                        new SupportTechnician { FullName = "B��ra ÇELİK", Title = "UYAP & Yazıl�m Destek", Phone = "Dahili: 1053", IsAvailable = false, Specialties = "UYAP / Sistem,Segbis / E-duruşma" }
                    );
                }

                // 6.1 Ticket Categories
                if (!context.TicketCategories.Any())
                {
                    context.TicketCategories.AddRange(
                        new TicketCategory { Name = "Donanım / Bilgisayar", IsActive = true },
                        new TicketCategory { Name = "Yazıc� / Tarayıcı", IsActive = true },
                        new TicketCategory { Name = "UYAP / Sistem", IsActive = true },
                        new TicketCategory { Name = "A� / internet", IsActive = true },
                        new TicketCategory { Name = "Segbis / E-duruşma", IsActive = true },
                        new TicketCategory { Name = "Diğer Konu", IsActive = true }
                    );
                }

                // 7. Ayarlar ve SSS
                var settingsToSeed = new List<SiteSetting>
                {
                    new SiteSetting { Key = "Designers", Value = "Uğur BA�DA� - Murat ALTUN", Description = "Footer kısmında yer alan tasarımcı işimleri.", GroupName = "Footer" },
                    new SiteSetting { Key = "IT_Chief_Ext", Value = "1012", Description = "Bilgi İşlem şefi Dahili No", GroupName = "Contacts" },
                    new SiteSetting { Key = "IT_Software_Ext", Value = "1014", Description = "Bilişim ve Yazıl�m Sorumlusu Dahili No", GroupName = "Contacts" },
                    new SiteSetting { Key = "IT_Hardware_Ext", Value = "1013 / 1014", Description = "Donanım, A� ve Yazıc� Masası Dahili No", GroupName = "Contacts" },
                    new SiteSetting { Key = "Site_Title_Suffix", Value = "OrduNet | T.C. Ordu Adalet Sarayı", Description = "Tarayıcı sekme bağl���n�n son kısmı", GroupName = "General" },
                    new SiteSetting { Key = "Courthouse_Name_Location", Value = "T.C. Ordu Adalet Sarayı, Alt�nordu / Ordu", Description = "üst bardaki (topbar) konum metni", GroupName = "General" },
                    new SiteSetting { Key = "Contact_Phone", Value = "Santral: 0452 700 20 00", Description = "üst bardaki santral metni", GroupName = "Contacts" },
                    new SiteSetting { Key = "Contact_Phone_Raw", Value = "04527002000", Description = "Tıklanabilir telefon formatı", GroupName = "Contacts" },
                    new SiteSetting { Key = "Webmail_Url", Value = "https://eposta.adalet.gov.tr", Description = "E-Posta kısayol linki", GroupName = "Links" },
                    new SiteSetting { Key = "Uyap_Url", Value = "https://portal.uyap.gov.tr", Description = "UYAP Portal kısayol linki", GroupName = "Links" },
                    new SiteSetting { Key = "Header_Title", Value = "Ordu Adliyesi", Description = "Büyük bağl�k (Logo yanı)", GroupName = "Header" },
                    new SiteSetting { Key = "Header_Subtitle", Value = "T.C. Ordu Adalet Sarayı Bilişim Portalı &bull; OrduNet", Description = "Alt bağl�k (Logo yanı)", GroupName = "Header" },
                    new SiteSetting { Key = "Footer_Copyright", Value = "&copy; 2026 <strong>T.C. Ordu Adliyesi</strong> ı OrduNet �� A� Portalı. Tüm hakları saklıdır.", Description = "En alt telif hakkı yazıs�", GroupName = "Footer" }
                };

                foreach (var s in settingsToSeed)
                {
                    if (!context.SiteSettings.Any(x => x.Key == s.Key))
                        context.SiteSettings.Add(s);
                }

                if (!context.HelpdeskFaqs.Any())
                {
                    context.HelpdeskFaqs.AddRange(
                        new HelpdeskFaq { Question = "E-imza çalışmıyor", Answer = "Akis kart izleme arac�n� yeniden bağlat�n veya kartı ��kar�p takın.", IconClass = "bi-usb-drive", DisplayOrder = 1 },
                        new HelpdeskFaq { Question = "Yazıc� Çıktı Vermiyor", Answer = "Yazıc� tönerini, kağ�t kasetini kontrol edin; kuyruğu temizleyin.", IconClass = "bi-printer", DisplayOrder = 2 },
                        new HelpdeskFaq { Question = "Rehberde Kayıt Güncelleme", Answer = "Yeni Personel/oda değişikliçini Bilgi İşlem'e bildirin.", IconClass = "bi-person-lines-fill", DisplayOrder = 3 },
                        new HelpdeskFaq { Question = "UYAP Ekranı A��lm�yor", Answer = "Arka planda ağ�k kalan eski UYAP pencerelerini kapatın. Devam ederse bilgisayarı yeniden bağlat�n.", IconClass = "bi-display", DisplayOrder = 4 },
                        new HelpdeskFaq { Question = "A� Bağlant�s� Yok", Answer = "A� (Ethernet) kablosunun takılı olduğundan emin olun, gerekirse ��kar�p birkağ saniye bekleyip tekrar takın.", IconClass = "bi-ethernet", DisplayOrder = 5 },
                        new HelpdeskFaq { Question = "Java Hatası Al�yorum", Answer = "Java önbelleğini temizleyin. DÖzelmezse güncel sürüm kurulumu isteyin.", IconClass = "bi-cup-hot", DisplayOrder = 6 },
                        new HelpdeskFaq { Question = "E-Posta Şifresi Doldu", Answer = "Adalet Bakanlığı webmail'e giriş yaparak Şifrenizi yenileyin.", IconClass = "bi-envelope-at", DisplayOrder = 7 },
                        new HelpdeskFaq { Question = "Sayfa Bozuk çıkıyor", Answer = "CTRL+F5 ile önbelleği temizleyin veya tarayıcı ayarların�z� sıfırlay�p tarayıcıyı tekrar ağ�n.", IconClass = "bi-browser-chrome", DisplayOrder = 8 }
                    );
                }

                context.SaveChanges();
            }
            catch { }
        }
    }
}

