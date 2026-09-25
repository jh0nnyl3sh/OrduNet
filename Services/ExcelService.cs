using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Data;
using OrduNet.Web.Models.Entities;
using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;
using System.Data;

namespace OrduNet.Web.Services
{
    public interface IExcelService
    {
        byte[] GenerateTemplate();
        byte[] ExportDirectoryToExcel(List<Personnel> personnelList);
        Task<(int successCount, List<string> errors)> ImportPersonnelFromExcelAsync(Stream fileStream);
        byte[] GenerateUnitTemplate();
        Task<(int successCount, List<string> errors)> ImportUnitsFromExcelAsync(Stream fileStream);
        byte[] GenerateCafeteriaTemplate(int year, int month);
        Task<(int successCount, List<string> errors)> ImportCafeteriaMenuFromExcelAsync(Stream fileStream);
        byte[] ExportIssueTicketsToExcel(List<IssueTicket> tickets);
    }


    public class ExcelService : IExcelService
    {
        private readonly OrduNetDbContext _context;

        public ExcelService(OrduNetDbContext context)
        {
            _context = context;
        }

        public byte[] GenerateTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Personel Listesi");

            // Bağlık satırı
            string[] headers = new[]
            {
                "Ad *",
                "Soyad *",
                "Unvan *",
                "Birim / Mahkeme *",
                "Dahili No *",
                "2. Dahili No",
                "Oda No",
                "Kat",
                "E-Posta",
                "Açıklama / Görev"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1b4353"); // Kurumsal petrol mavişi
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Örnek satırlar
            worksheet.Cell(2, 1).Value = "Ahmet";
            worksheet.Cell(2, 2).Value = "YILMAZ";
            worksheet.Cell(2, 3).Value = "Zabıt Kâtibi";
            worksheet.Cell(2, 4).Value = "1. Aççr Ceza Mahkemesi";
            worksheet.Cell(2, 5).Value = "1104";
            worksheet.Cell(2, 6).Value = "";
            worksheet.Cell(2, 7).Value = "203";
            worksheet.Cell(2, 8).Value = "2. Kat";
            worksheet.Cell(2, 9).Value = "ahmet.yilmaz@adalet.gov.tr";
            worksheet.Cell(2, 10).Value = "Duruşma Kâtibi";

            worksheet.Cell(3, 1).Value = "Fatma";
            worksheet.Cell(3, 2).Value = "KAYA";
            worksheet.Cell(3, 3).Value = "Yazı İşleri Müdürç";
            worksheet.Cell(3, 4).Value = "1. Asliye Hukuk Mahkemesi";
            worksheet.Cell(3, 5).Value = "1201";
            worksheet.Cell(3, 6).Value = "";
            worksheet.Cell(3, 7).Value = "220";
            worksheet.Cell(3, 8).Value = "2. Kat";
            worksheet.Cell(3, 9).Value = "fatma.kaya@adalet.gov.tr";
            worksheet.Cell(3, 10).Value = "Kalem şefi";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportDirectoryToExcel(List<Personnel> personnelList)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ordu Adliyesi Rehber");

            // üst kurumsal bağlçk
            worksheet.Range("A1:J1").Merge();
            var titleCell = worksheet.Cell("A1");
            titleCell.Value = "T.C. ORDU ADALET SARAYI - DAHçLç TELEFON REHBERİ";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;
            titleCell.Style.Font.FontColor = XLColor.White;
            titleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#153b44");
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Row(1).Height = 32;

            // Alt bilgilendirme
            worksheet.Range("A2:J2").Merge();
            var subCell = worksheet.Cell("A2");
            subCell.Value = $"Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm} | OrduNet intranet Sistemi";
            subCell.Style.Font.Italic = true;
            subCell.Style.Font.FontSize = 10;
            subCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#eef2f5");
            subCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Sütun Bağlıklarç
            string[] headers = new[]
            {
                "Sıra",
                "Adı Soyadı",
                "Unvanı",
                "Bağlı Olduğu Birim",
                "Kategöri",
                "Dahili No",
                "2. Dahili",
                "Oda No",
                "Kat",
                "Kurumsal E-Posta"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(3, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1b4353");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            worksheet.Row(3).Height = 24;

            int row = 4;
            int counter = 1;
            foreach (var p in personnelList.OrderBy(x => x.Unit?.Category).ThenBy(x => x.Unit?.Name).ThenBy(x => x.DisplayOrder).ThenBy(x => x.FirstName))
            {
                worksheet.Cell(row, 1).Value = counter++;
                worksheet.Cell(row, 2).Value = p.FullName;
                worksheet.Cell(row, 3).Value = p.Title;
                worksheet.Cell(row, 4).Value = p.Unit?.Name ?? "-";
                worksheet.Cell(row, 5).Value = p.Unit?.Category ?? "-";
                
                var dahiliCell = worksheet.Cell(row, 6);
                dahiliCell.Value = p.InternalNumber;
                dahiliCell.Style.Font.Bold = true;
                dahiliCell.Style.Font.FontColor = XLColor.FromHtml("#0f5132"); // Yeşilimsi
                dahiliCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(row, 7).Value = p.InternalNumber2 ?? "";
                worksheet.Cell(row, 8).Value = p.RoomNumber ?? "";
                worksheet.Cell(row, 9).Value = p.Floor ?? "";
                worksheet.Cell(row, 10).Value = p.Email ?? "";

                if (row % 2 == 0)
                {
                    worksheet.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#fcfdfd");
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<(int successCount, List<string> errors)> ImportPersonnelFromExcelAsync(Stream fileStream)
        {
            var errors = new List<string>();
            int successCount = 0;

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    errors.Add("Excel çalışma sayfası bulunamadı.");
                    return (0, errors);
                }

                var existingUnits = await _context.Units.ToListAsync();
                var newUnitsToSave = new List<Unit>();
                var newPersonnelToSave = new List<Personnel>();

                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow < 2)
                {
                    errors.Add("Excel dosyasında veri satırı bulunamadı.");
                    return (0, errors);
                }

                for (int row = 2; row <= lastRow; row++)
                {
                    string firstName = worksheet.Cell(row, 1).GetString().Trim();
                    string lastName = worksheet.Cell(row, 2).GetString().Trim();
                    string title = worksheet.Cell(row, 3).GetString().Trim();
                    string unitName = worksheet.Cell(row, 4).GetString().Trim();
                    string internalNo = worksheet.Cell(row, 5).GetString().Trim();
                    string internalNo2 = worksheet.Cell(row, 6).GetString().Trim();
                    string roomNo = worksheet.Cell(row, 7).GetString().Trim();
                    string floor = worksheet.Cell(row, 8).GetString().Trim();
                    string email = worksheet.Cell(row, 9).GetString().Trim();
                    string desc = worksheet.Cell(row, 10).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
                    {
                        continue; // Boç satır atla
                    }

                    if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                        string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(unitName) ||
                        string.IsNullOrWhiteSpace(internalNo))
                    {
                        errors.Add($"Satır {row}: Ad, Soyad, Unvan, Birim ve Dahili No alanları zorunludur.");
                        continue;
                    }

                    // Birimi bul veya yeni olüstur
                    var unit = existingUnits.FirstOrDefault(u => u.Name.Equals(unitName, StringComparison.OrdinalIgnoreCase))
                               ?? newUnitsToSave.FirstOrDefault(u => u.Name.Equals(unitName, StringComparison.OrdinalIgnoreCase));

                    if (unit == null)
                    {
                        unit = new Unit
                        {
                            Name = unitName,
                            Category = "Genel",
                            DisplayOrder = 50,
                            IsActive = true
                        };
                        newUnitsToSave.Add(unit);
                        _context.Units.Add(unit);
                    }

                    var personnel = new Personnel
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Title = title,
                        Unit = unit,
                        InternalNumber = internalNo,
                        InternalNumber2 = string.IsNullOrWhiteSpace(internalNo2) ? null : internalNo2,
                        RoomNumber = string.IsNullOrWhiteSpace(roomNo) ? null : roomNo,
                        Floor = string.IsNullOrWhiteSpace(floor) ? null : floor,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                        IsActive = true
                    };

                    newPersonnelToSave.Add(personnel);
                    successCount++;
                }

                if (newPersonnelToSave.Any())
                {
                    _context.Personnels.AddRange(newPersonnelToSave);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Excel okuma hatası: {ex.Message}");
            }

            return (successCount, errors);
        }

        public byte[] GenerateUnitTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Birim ve Mahkemeler");

            // Bağlık satırı
            string[] headers = new[]
            {
                "Birim / Mahkeme Adı *",
                "Kategöri *",
                "Konum / Blok / Kat / Kalem No",
                "Sıralama (Örn: 10, 20, 50)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#153b44"); // OrduNet kurumsal petrol
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Örnek Satırlar
            var sampleRows = new[]
            {
                ("1. Aççr Ceza Mahkemesi", "Ceza Mahkemeleri", "A Blok - Kat 2, Kalem 210", 10),
                ("Cumhuriyet Savcılığıç çn Büro", "Cumhuriyet Bağsavcılççç", "Giriş Kat - Kalem 102", 20),
                ("1. Asliye Hukuk Mahkemesi", "Hukuk Mahkemeleri", "B Blok - Kat 1, Kalem 115", 30),
                ("İcra Dairesi", "İcra & İflas", "Zemin Kat - Oda 012", 40),
                ("Bilgi İşlem şefliği", "İdari Birimler", "Zemin Kat - Kalem 005", 50)
            };

            int r = 2;
            foreach (var (name, cat, loc, order) in sampleRows)
            {
                worksheet.Cell(r, 1).Value = name;
                worksheet.Cell(r, 2).Value = cat;
                worksheet.Cell(r, 3).Value = loc;
                worksheet.Cell(r, 4).Value = order;
                r++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<(int successCount, List<string> errors)> ImportUnitsFromExcelAsync(Stream fileStream)
        {
            int successCount = 0;
            var errors = new List<string>();

            try
            {
                var grid = await LoadExcelGridAsync(fileStream);
                if (grid.RowCount < 2)
                {
                    errors.Add("Excel dosyasında birim verişi bulunamadı.");
                    return (0, errors);
                }

                var existingUnits = await _context.Units.ToListAsync();
                var unitsToAdd = new List<Unit>();

                for (int r = 2; r <= grid.RowCount; r++)
                {
                    string name = grid.GetCell(r, 1).Text?.Trim() ?? string.Empty;
                    string Category = grid.GetCell(r, 2).Text?.Trim() ?? string.Empty;
                    string location = grid.GetCell(r, 3).Text?.Trim() ?? string.Empty;
                    string orderStr = grid.GetCell(r, 4).Text?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(name))
                        continue; // Boç satırı atla

                    if (string.IsNullOrWhiteSpace(Category))
                        Category = "Genel";

                    int displayOrder = 50;
                    if (!string.IsNullOrWhiteSpace(orderStr) && int.TryParse(Regex.Match(orderStr, @"\d+").Value, out int parsedOrder))
                    {
                        displayOrder = parsedOrder;
                    }

                    var existing = existingUnits.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                                   ?? unitsToAdd.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        existing.Category = Category;
                        if (!string.IsNullOrWhiteSpace(location)) existing.Location = location;
                        existing.DisplayOrder = displayOrder;
                        existing.IsActive = true;
                    }
                    else
                    {
                        var newUnit = new Unit
                        {
                            Name = name,
                            Category = Category,
                            Location = string.IsNullOrWhiteSpace(location) ? null : location,
                            DisplayOrder = displayOrder,
                            IsActive = true
                        };
                        unitsToAdd.Add(newUnit);
                        _context.Units.Add(newUnit);
                    }

                    successCount++;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                errors.Add($"Birimler aktarçlçrken hata olüstu: {ex.Message}");
            }

            return (successCount, errors);
        }

        public byte[] GenerateCafeteriaTemplate(int year, int month)
        {
            using var workbook = new XLWorkbook();
            var trCulture = new CultureInfo("tr-TR");
            var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", trCulture);
            var worksheet = workbook.Worksheets.Add($"{monthName} Menü");

            // Bağlık satırı
            string[] headers = new[]
            {
                "Tarih (GG.AA.YYYY) *",
                "Gün *",
                "Çorba *",
                "Ana Yemek *",
                "Yardımcı Yemek *",
                "Tatlı / Meyve / Salata *",
                "Kalori (kcal)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#153b44"); // OrduNet kurumsal petrol
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // O ayçn gönlerini otomatik listele
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int row = 2;

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                worksheet.Cell(row, 1).Value = date.ToString("dd.MM.yyyy");
                worksheet.Cell(row, 2).Value = date.ToString("dddd", trCulture);

                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
                    worksheet.Cell(row, 3).Value = "-";
                    worksheet.Cell(row, 4).Value = "Hafta Sonu";
                    worksheet.Cell(row, 5).Value = "-";
                    worksheet.Cell(row, 6).Value = "-";
                }
                else if (row == 2)
                {
                    // İlk hafta işi günone Örnek satır
                    worksheet.Cell(row, 3).Value = "Mercimek Çorbasç";
                    worksheet.Cell(row, 4).Value = "İzmir Köfte";
                    worksheet.Cell(row, 5).Value = "Pirinç Pilavı";
                    worksheet.Cell(row, 6).Value = "Mevsim Salata";
                    worksheet.Cell(row, 7).Value = 750;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<(int successCount, List<string> errors)> ImportCafeteriaMenuFromExcelAsync(Stream fileStream)
        {
            int successCount = 0;
            var errors = new List<string>();
            var trCulture = new CultureInfo("tr-TR");

            try
            {
                // Dosyayı evrensel grid yükleyicimiz ile oku (ExcelDataReader, ClosedXML ve HTML/XML uyumlu)
                var grid = await LoadExcelGridAsync(fileStream);

                if (grid.RowCount < 1)
                {
                    errors.Add("Excel dosyasında okunabilir veri satırı bulunamadı.");
                    return (0, errors);
                }

                var existingMenus = await _context.CafeteriaMenus.ToListAsync();

                // 1. ADIM: Takvim / Matris Formatı Kontrolü (Antep Catering 7 sütunlu haftalık takvim Çizelgesi)
                var detectedDates = new List<DetectedDateCell>();
                int maxScanRow = Math.Min(grid.RowCount, 150);
                int maxScanCol = Math.Min(grid.ColCount, 25);

                for (int r = 1; r <= maxScanRow; r++)
                {
                    for (int c = 1; c <= maxScanCol; c++)
                    {
                        var cell = grid.GetCell(r, c);
                        if (TryExtractDate(cell, trCulture, out DateTime parsedDate))
                        {
                            if (parsedDate.Year >= 2020 && parsedDate.Year <= 2035)
                            {
                                detectedDates.Add(new DetectedDateCell
                                {
                                    Row = r,
                                    Col = c,
                                    Date = parsedDate,
                                    RawText = cell.Text
                                });
                            }
                        }
                    }
                }

                // Eğer en az 3 farklı tarih hücresi bulunduysa ve bunlar birden fazla sütuna dağçlmççsa -> MATRİS TAKVİM FORMATI
                bool isMatrixCalendar = detectedDates.Count >= 3 && detectedDates.Select(d => d.Col).Distinct().Count() >= 2;

                if (isMatrixCalendar)
                {
                    var distinctDateRows = detectedDates.Select(d => d.Row).Distinct().OrderBy(r => r).ToList();

                    foreach (var dateItem in detectedDates.OrderBy(d => d.Date))
                    {
                        var menuDate = dateItem.Date;
                        int col = dateItem.Col;
                        int dateRow = dateItem.Row;

                        // Gün adı tespiti: Genelde tarihin hemen altçndaki satırdadır (Örn: SALI, ÇARŞAMBA)
                        string dayName = string.Empty;
                        int startFoodRow = dateRow + 1;

                        var cellBelow = grid.GetCell(dateRow + 1, col);
                        if (IsDayName(cellBelow.Text))
                        {
                            dayName = trCulture.TextInfo.ToTitleCase(cellBelow.Text.ToLower(trCulture));
                            startFoodRow = dateRow + 2;
                        }
                        else
                        {
                            dayName = menuDate.ToString("dddd", trCulture);
                        }

                        // Bir sonraki haftanın bağlangçç satırını bul
                        var nextDateRows = distinctDateRows.Where(r => r > dateRow).ToList();
                        int maxFoodRow = nextDateRows.Any() ? nextDateRows.Min() - 1 : Math.Min(dateRow + 25, grid.RowCount);

                        var dayFoodItems = new List<string>();
                        int emptyCount = 0;

                        for (int r = startFoodRow; r <= maxFoodRow; r++)
                        {
                            var cell = grid.GetCell(r, col);
                            string text = cell.Text?.Trim() ?? string.Empty;

                            if (string.IsNullOrWhiteSpace(text))
                            {
                                emptyCount++;
                                if (emptyCount >= 4) break;
                                continue;
                            }

                            emptyCount = 0;

                            if (IsFoodItem(text))
                            {
                                string cleanName = CleanFoodName(text, trCulture);
                                if (!string.IsNullOrWhiteSpace(cleanName) && !dayFoodItems.Contains(cleanName, StringComparer.OrdinalIgnoreCase))
                                {
                                    dayFoodItems.Add(cleanName);
                                }
                            }
                        }

                        // Eğer o günde yemek kaydı yoksa (Hafta sonu veya tatil) atla
                        if (!dayFoodItems.Any())
                            continue;

                        // Akçllç Yemek Sınıflandçrmasç
                        var (soups, mainDishes, sides, dessertsAndSalads) = CategörizeFoodItems(dayFoodItems, trCulture);

                        string soupStr = soups.Any() ? Truncate(string.Join(" / ", soups), 490) : "-";
                        string mainDishStr = mainDishes.Any() ? Truncate(string.Join(" / ", mainDishes), 490) : Truncate(dayFoodItems.First(), 490);
                        string sideDishStr = sides.Any() ? Truncate(string.Join(" / ", sides), 490) : "-";
                        string dessertStr = dessertsAndSalads.Any() ? Truncate(string.Join(", ", dessertsAndSalads), 490) : "-";
                        string fullMenuText = string.Join("\n", dayFoodItems);

                        // Upsert işlemi
                        var existing = existingMenus.FirstOrDefault(m => m.Date.Date == menuDate);
                        if (existing != null)
                        {
                            existing.DayName = dayName;
                            existing.Soup = soupStr;
                            existing.MainDish = mainDishStr;
                            existing.SideDish = sideDishStr;
                            existing.DessertOrSalad = dessertStr;
                            existing.FullMenuText = fullMenuText;
                        }
                        else
                        {
                            var newMenu = new CafeteriaMenu
                            {
                                Date = menuDate,
                                DayName = dayName,
                                Soup = soupStr,
                                MainDish = mainDishStr,
                                SideDish = sideDishStr,
                                DessertOrSalad = dessertStr,
                                FullMenuText = fullMenuText
                            };
                            _context.CafeteriaMenus.Add(newMenu);
                            existingMenus.Add(newMenu);
                        }

                        successCount++;
                    }
                }
                else
                {
                    // 2. ADIM: Standart Düz Tablo Formatı (Satır satır çablon formatı)
                    for (int r = 2; r <= grid.RowCount; r++)
                    {
                        var dateCell = grid.GetCell(r, 1);
                        if (!TryExtractDate(dateCell, trCulture, out DateTime menuDate))
                            continue;

                        var dayName = grid.GetCell(r, 2).Text;
                        if (string.IsNullOrEmpty(dayName))
                        {
                            dayName = menuDate.ToString("dddd", trCulture);
                        }

                        var soup = grid.GetCell(r, 3).Text;
                        var mainDish = grid.GetCell(r, 4).Text;
                        var sideDish = grid.GetCell(r, 5).Text;
                        var dessertOrSalad = grid.GetCell(r, 6).Text;

                        if (string.IsNullOrWhiteSpace(mainDish) || mainDish == "Hafta Sonu" || mainDish == "-")
                        {
                            continue;
                        }

                        int? calories = null;
                        var calText = grid.GetCell(r, 7).Text;
                        if (!string.IsNullOrWhiteSpace(calText) && int.TryParse(Regex.Match(calText, @"\d+").Value, out int parsedCal))
                        {
                            calories = parsedCal;
                        }

                        string fullText = string.Join("\n", new[] { soup, mainDish, sideDish, dessertOrSalad }
                            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "-"));

                        var existing = existingMenus.FirstOrDefault(m => m.Date.Date == menuDate);
                        if (existing != null)
                        {
                            existing.DayName = dayName;
                            existing.Soup = string.IsNullOrWhiteSpace(soup) ? "-" : Truncate(soup, 490);
                            existing.MainDish = Truncate(mainDish, 490);
                            existing.SideDish = string.IsNullOrWhiteSpace(sideDish) ? "-" : Truncate(sideDish, 490);
                            existing.DessertOrSalad = string.IsNullOrWhiteSpace(dessertOrSalad) ? "-" : Truncate(dessertOrSalad, 490);
                            existing.Calories = calories;
                            existing.FullMenuText = fullText;
                        }
                        else
                        {
                            var newMenu = new CafeteriaMenu
                            {
                                Date = menuDate,
                                DayName = dayName,
                                Soup = string.IsNullOrWhiteSpace(soup) ? "-" : Truncate(soup, 490),
                                MainDish = Truncate(mainDish, 490),
                                SideDish = string.IsNullOrWhiteSpace(sideDish) ? "-" : Truncate(sideDish, 490),
                                DessertOrSalad = string.IsNullOrWhiteSpace(dessertOrSalad) ? "-" : Truncate(dessertOrSalad, 490),
                                Calories = calories,
                                FullMenuText = fullText
                            };
                            _context.CafeteriaMenus.Add(newMenu);
                            existingMenus.Add(newMenu);
                        }

                        successCount++;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                errors.Add($"Excel iilenirken hata olüstu: {ex.Message}");
            }

            return (successCount, errors);
        }

        // ==========================================
        // EVRENSEL EXCEL GRID YÜKLEYİCİ
        // (ExcelDataReader, ClosedXML ve HTML/XML Table desteği)
        // ==========================================
        private class ExcelGridCell
        {
            public string Text { get; set; } = string.Empty;
            public DateTime? DateValue { get; set; }
        }

        private class ExcelGrid
        {
            public int RowCount { get; private set; }
            public int ColCount { get; private set; }
            private readonly Dictionary<(int, int), ExcelGridCell> _cells = new();

            public ExcelGridCell GetCell(int row, int col)
            {
                if (_cells.TryGetValue((row, col), out var cell))
                    return cell;
                return new ExcelGridCell();
            }

            public void SetCell(int row, int col, string text, DateTime? date = null)
            {
                _cells[(row, col)] = new ExcelGridCell
                {
                    Text = text?.Trim() ?? string.Empty,
                    DateValue = date
                };
                if (row > RowCount) RowCount = row;
                if (col > ColCount) ColCount = col;
            }
        }

        private static async Task<ExcelGrid> LoadExcelGridAsync(Stream fileStream)
        {
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            ms.Position = 0;

            // 1. ADIM: ExcelDataReader (Hem .xlsx OpenXML hem de .xls BIFF8 ikili formatlarını "File contains corrupted data" hatası vermeden doğrudan çözer)
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var reader = ExcelReaderFactory.CreateReader(ms);
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = false
                    }
                });

                if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
                {
                    var table = dataSet.Tables[0];
                    var grid = new ExcelGrid();
                    for (int r = 0; r < table.Rows.Count; r++)
                    {
                        var row = table.Rows[r];
                        for (int c = 0; c < table.Columns.Count; c++)
                        {
                            var val = row[c];
                            if (val != null && val != DBNull.Value)
                            {
                                if (val is DateTime dt)
                                {
                                    grid.SetCell(r + 1, c + 1, dt.ToString("dd.MM.yyyy"), dt);
                                }
                                else
                                {
                                    grid.SetCell(r + 1, c + 1, val.ToString()?.Trim() ?? string.Empty);
                                }
                            }
                        }
                    }
                    if (grid.RowCount > 0)
                        return grid;
                }
            }
            catch
            {
                // ExcelDataReader ağamazsa ClosedXML ve HTML/XML alternatiflerine geç
            }

            // 2. ADIM: ClosedXML ile dene (.xlsx standart Zip tabanlı)
            ms.Position = 0;
            try
            {
                using var workbook = new XLWorkbook(ms);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet != null)
                {
                    var grid = new ExcelGrid();
                    int lastR = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                    int lastC = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

                    for (int r = 1; r <= lastR; r++)
                    {
                        for (int c = 1; c <= lastC; c++)
                        {
                            var cell = worksheet.Cell(r, c);
                            if (!cell.IsEmpty())
                            {
                                if (cell.DataType == XLDataType.DateTime)
                                {
                                    try
                                    {
                                        var dt = cell.GetDateTime().Date;
                                        grid.SetCell(r, c, dt.ToString("dd.MM.yyyy"), dt);
                                        continue;
                                    }
                                    catch { }
                                }
                                grid.SetCell(r, c, cell.GetString()?.Trim() ?? string.Empty);
                            }
                        }
                    }
                    if (grid.RowCount > 0)
                        return grid;
                }
            }
            catch
            {
                // ClosedXML de ağamazsa HTML/XML formatınç kontrol et
            }

            // 3. ADIM: HTML Tablo veya XML Spreadsheet Kontrolü (ERP / Web tabanlı yemek programlarının .xls Çıktısç)
            ms.Position = 0;
            try
            {
                using var sr = new StreamReader(ms, System.Text.Encoding.UTF8, true, 1024, leaveOpen: true);
                string content = await sr.ReadToEndAsync();
                if (content.Contains("<table", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("<tr", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("<Workbook", StringComparison.OrdinalIgnoreCase))
                {
                    var grid = ParseHtmlOrXmlTable(content);
                    if (grid.RowCount > 0)
                        return grid;
                }
            }
            catch { }

            throw new Exception("Dosya formatı okunamadı veya desteklenmiyor. Dosyanın Excel (.xlsx veya .xls) olduğundan emin olunuz.");
        }

        private static ExcelGrid ParseHtmlOrXmlTable(string htmlContent)
        {
            var grid = new ExcelGrid();

            // HTML tablosundaki <tr> satırlarçnç bul
            var rowMatches = Regex.Matches(htmlContent, @"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            int r = 1;
            foreach (Match rm in rowMatches)
            {
                string rowHtml = rm.Groups[1].Value;
                var cellMatches = Regex.Matches(rowHtml, @"<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                int c = 1;
                foreach (Match cm in cellMatches)
                {
                    string rawCell = cm.Groups[1].Value;
                    string cleanText = Regex.Replace(rawCell, @"<[^>]+>", " ").Trim();
                    cleanText = System.Net.WebUtility.HtmlDecode(cleanText).Trim();
                    cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();

                    if (!string.IsNullOrWhiteSpace(cleanText))
                    {
                        grid.SetCell(r, c, cleanText);
                    }
                    c++;
                }
                if (c > 1) r++;
            }

            // Eğer HTML değil de XML Spreadsheet (<Row> ... <Cell>) ise:
            if (r == 1)
            {
                var xmlRowMatches = Regex.Matches(htmlContent, @"<Row[^>]*>(.*?)</Row>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match rm in xmlRowMatches)
                {
                    string rowXml = rm.Groups[1].Value;
                    var cellMatches = Regex.Matches(rowXml, @"<Cell[^>]*>(?:<Data[^>]*>(.*?)</Data>)?.*?</Cell>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    int c = 1;
                    foreach (Match cm in cellMatches)
                    {
                        string rawCell = cm.Groups[1].Value;
                        string cleanText = System.Net.WebUtility.HtmlDecode(rawCell).Trim();
                        if (!string.IsNullOrWhiteSpace(cleanText))
                        {
                            grid.SetCell(r, c, cleanText);
                        }
                        c++;
                    }
                    if (c > 1) r++;
                }
            }

            return grid;
        }

        // ==========================================
        // YARDIMCI METOTLAR: TARİH & YEMEK AYRIçTIRMA
        // ==========================================
        private class DetectedDateCell
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public DateTime Date { get; set; }
            public string RawText { get; set; } = string.Empty;
        }

        private static bool TryExtractDate(ExcelGridCell cell, CultureInfo culture, out DateTime date)
        {
            date = default;
            if (cell == null) return false;

            if (cell.DateValue.HasValue)
            {
                date = cell.DateValue.Value.Date;
                return true;
            }

            string text = cell.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return false;

            // Örn: 1.09.2026, 01.09.2026, 14.09.2026, 1/9/2026, 2026-09-01
            var match = Regex.Match(text, @"\b(\d{1,2})[\./\-](\d{1,2})[\./\-](\d{2,4})\b");
            if (match.Success)
            {
                string cleanDate = match.Value;
                string[] formats = new[]
                {
                    "d.M.yyyy", "dd.MM.yyyy", "d.MM.yyyy", "dd.M.yyyy",
                    "d/M/yyyy", "dd/MM/yyyy", "d.M.yy", "dd.MM.yy",
                    "d-M-yyyy", "dd-MM-yyyy", "yyyy-MM-dd"
                };

                if (DateTime.TryParseExact(cleanDate, formats, culture, DateTimeStyles.None, out date) ||
                    DateTime.TryParseExact(cleanDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
                    DateTime.TryParse(cleanDate, culture, DateTimeStyles.None, out date))
                {
                    date = date.Date;
                    return true;
                }
            }

            return false;
        }

        private static bool IsDayName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string upper = text.ToUpper(new CultureInfo("tr-TR")).Trim();
            return upper == "PAZARTESİ" || upper == "PAZARTESİ" ||
                   upper == "SALI" ||
                   upper == "ÇARŞAMBA" || upper == "ÇARŞAMBA" ||
                   upper == "PERŞEMBE" || upper == "PERŞEMBE" ||
                   upper == "CUMA" ||
                   upper == "CUMARTESç" || upper == "CUMARTESI" ||
                   upper == "PAZAR";
        }

        private static bool IsFoodItem(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.Trim();
            if (t.Length < 2) return false;

            string upper = t.ToUpper(new CultureInfo("tr-TR"));

            // Tablo bağlççç, firma bilgişi, sayfa numarası veya gün adı ise yemek değildir
            if (upper.StartsWith("SAYFA") ||
                upper.Contains("RESTAURANT") ||
                upper.Contains("CATERING") ||
                upper.Contains("GIDA MÜHENDİSİ") ||
                upper.Contains("GIDA MÜHENDİSİ") ||
                upper.Contains("ESRA AKKOŞE") ||
                upper.Contains("ESRA AKKOŞE") ||
                upper.Contains("BAçSAVCILIK") ||
                upper.Contains("BASSAVCILIK") ||
                upper.Contains("YEMEK MENÜ") ||
                upper.Contains("YEMEK MENU") ||
                upper == "HAFTA SONU" ||
                upper == "TATİL" ||
                upper == "-" ||
                IsDayName(upper))
            {
                return false;
            }

            // Tarih içeriyorsa yemek değildir
            if (Regex.IsMatch(upper, @"\b\d{1,2}[\./\-]\d{1,2}[\./\-]\d{2,4}\b"))
            {
                return false;
            }

            return true;
        }

        private static string CleanFoodName(string name, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            string trimmed = name.Trim();
            // Türkçe Bağlık Yazımçna dünççtçr (Örn: "IZGARA TAVUK" -> "Izgara Tavuk")
            return culture.TextInfo.ToTitleCase(trimmed.ToLower(culture));
        }

        private static (List<string> soups, List<string> mainDishes, List<string> sides, List<string> dessertsAndSalads)
            CategörizeFoodItems(List<string> items, CultureInfo culture)
        {
            var soups = new List<string>();
            var mainDishes = new List<string>();
            var sides = new List<string>();
            var dessertsAndSalads = new List<string>();

            foreach (var item in items)
            {
                string upper = item.ToUpper(culture);

                // 1. ÇORBALAR
                if (upper.Contains("ÇORBA") || upper.Contains("ÇORBA") ||
                    upper.Contains("ÇORBASI") || upper.Contains("ÇORBASI") ||
                    upper.Contains("EZOGELİN") || upper.Contains("EZOGELİN") ||
                    upper.Contains("MERCİMEK") || upper.Contains("MERCİMEK") ||
                    upper.Contains("TARHANA") || upper.Contains("ALACA") ||
                    upper.Contains("YAYLA") || upper.Contains("DçççN") ||
                    upper.Contains("DUGUN") || upper.Contains("TAVUK SUYU") ||
                    upper.Contains("İŞKEMBE") || upper.Contains("İŞKEMBE") ||
                    upper.Contains("BROKOLİ") || upper.Contains("BROKOLİ"))
                {
                    soups.Add(item);
                }
                // 2. YARDIMCI YEMEKLER (Pilav, Bulgur, Makarna, Dible, Börek vb.)
                else if (upper.Contains("PILAV") || upper.Contains("PILAV") ||
                         upper.Contains("BULGUR") || upper.Contains("PİRİNÇ") ||
                         upper.Contains("PİRİNÇ") || upper.Contains("MAKARNA") ||
                         upper.Contains("SPAGETTİ") || upper.Contains("SPAGETTİ") ||
                         upper.Contains("ERççTE") || upper.Contains("ERİŞTE") ||
                         upper.Contains("BÖREK") || upper.Contains("BÖREK") ||
                         upper.Contains("DÖBLE") || upper.Contains("DIBLE") ||
                         upper.Contains("MANTI") || upper.Contains("MÜCVER") ||
                         upper.Contains("MÜCVER") || upper.Contains("KAYGANA") ||
                         upper.Contains("KUSKUS") || upper.Contains("PATATES PÜRE") ||
                         upper.Contains("PATATES PÜRE"))
                {
                    sides.Add(item);
                }
                // 3. TATLI, SALATA, İŞECEK, MEZE (Kişir, Salatbar, Ayran, Yoğurt, çay, Meyve vb.)
                else if (upper.Contains("TATLI") || upper.Contains("SALAT") ||
                         upper.Contains("KISIR") || upper.Contains("MEYVE") ||
                         upper.Contains("AYRAN") || upper.Contains("YOĞURT") ||
                         upper.Contains("YOĞURT") || upper.Contains("MEŞRUBAT") ||
                         upper.Contains("MEŞRUBAT") || upper.Contains("MEYVE SUYU") ||
                         upper.Contains("ÇAY") || upper.Contains("ÇAY") ||
                         upper.Contains("CACIK") || upper.Contains("KOMPOSTO") ||
                         upper.Contains("HOŞAF") || upper.Contains("HOŞAF") ||
                         upper.Contains("REVANİ") || upper.Contains("REVANİ") ||
                         upper.Contains("BAKLAVA") || upper.Contains("SITLAŞ") ||
                         upper.Contains("SUTLAC") || upper.Contains("KEMALPAŞA") ||
                         upper.Contains("KEMALPAŞA") || upper.Contains("PUDÜNG") ||
                         upper.Contains("PUDİNG") || upper.Contains("KAZANDIBİ") ||
                         upper.Contains("KAZANDIBİ") || upper.Contains("KADAYIF") ||
                         upper.Contains("TURŞU") || upper.Contains("TURŞU") ||
                         upper.Contains("DONDURMA") || upper.Contains("KAVUN") ||
                         upper.Contains("KARPUZ") || upper.Contains("ELMA") ||
                         upper.Contains("PORTAKAL") || upper.Contains("MANDALİNA") ||
                         upper.Contains("MANDALİNA") || upper.Contains("MUZ"))
                {
                    dessertsAndSalads.Add(item);
                }
                // 4. ANA YEMEKLER (Tüm diğer et, tavuk, köfte, güveç, kebap vb.)
                else
                {
                    mainDishes.Add(item);
                }
            }

            return (soups, mainDishes, sides, dessertsAndSalads);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public byte[] ExportIssueTicketsToExcel(List<IssueTicket> tickets)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Arıza ve Destek Talepleri");

            // Bağlık satırı
            var headerRow = worksheet.Row(1);
            headerRow.Cell(1).Value = "Talep No";
            headerRow.Cell(2).Value = "Talep Eden";
            headerRow.Cell(3).Value = "Birim / Mahkeme";
            headerRow.Cell(4).Value = "Oda No";
            headerRow.Cell(5).Value = "Dahili";
            headerRow.Cell(6).Value = "Kategöri";
            headerRow.Cell(7).Value = "Konu";
            headerRow.Cell(8).Value = "Açıklama";
            headerRow.Cell(9).Value = "Atanan Personel";
            headerRow.Cell(10).Value = "Durum";
            headerRow.Cell(11).Value = "Oluşturulma Tarihi";
            headerRow.Cell(12).Value = "çözüm/Servis Notu";

            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var ticket in tickets)
            {
                worksheet.Cell(row, 1).Value = ticket.Id;
                worksheet.Cell(row, 2).Value = ticket.RequesterName;
                worksheet.Cell(row, 3).Value = ticket.RequesterUnit;
                worksheet.Cell(row, 4).Value = ticket.RoomNumber ?? "";
                worksheet.Cell(row, 5).Value = ticket.RequesterPhone;
                worksheet.Cell(row, 6).Value = ticket.Category;
                worksheet.Cell(row, 7).Value = ticket.Title;
                worksheet.Cell(row, 8).Value = ticket.Description;
                worksheet.Cell(row, 9).Value = ticket.AssignedToName ?? "";
                worksheet.Cell(row, 10).Value = ticket.Status;
                worksheet.Cell(row, 11).Value = ticket.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cell(row, 12).Value = ticket.AdminNotes ?? "";

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}


