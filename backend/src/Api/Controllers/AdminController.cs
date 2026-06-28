using GestionStagesMEN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using ClosedXML.Excel;
using GestionStagesMEN.Core.Entities;
using GestionStagesMEN.Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace GestionStagesMEN.Api.Controllers;

public class AdminCreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
}

public class TestEmailDto
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class AdminUpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GestionStagesMEN.Core.Interfaces.IEmailService _email;

    public AdminController(AppDbContext ctx, UserManager<ApplicationUser> userManager, GestionStagesMEN.Core.Interfaces.IEmailService email)
    {
        _ctx = ctx;
        _userManager = userManager;
        _email = email;
    }

    [AllowAnonymous]
    [HttpGet("check-me")]
    public IActionResult CheckMe()
    {
        return Ok(new { 
            Message = "AdminController est accessible !",
            IsAuthenticated = User.Identity?.IsAuthenticated,
            UserName = User.Identity?.Name,
            Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList()
        });
    }

    /// <summary>GET /api/admin/users — Liste des utilisateurs avec pagination</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var total = await _ctx.Users.CountAsync();
        var items = await _ctx.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new {
                u.Id,
                u.Email,
                u.FullName,
                u.IsActive,
                u.CreatedAt,
                Role = _ctx.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(_ctx.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .FirstOrDefault() ?? "No Role"
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>GET /api/admin/users/{id} — Détail d'un utilisateur</summary>
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _ctx.Users
            .Select(u => new {
                u.Id,
                u.Email,
                u.FullName,
                u.IsActive,
                u.CreatedAt,
                Role = _ctx.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(_ctx.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .FirstOrDefault() ?? "No Role"
            })
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null) return BadRequest(new { errors = new[] { "Cet email est déjà utilisé." } });

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, dto.Role);

        // Création du profil associé
        if (dto.Role == "Student")
        {
            _ctx.Students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CNE = "AUTO_" + new Random().Next(10000, 99999),
                Filiere = "Non renseigné",
                Etablissement = "Créé par Admin",
                Promotion = "Non renseigné"
            });
        }
        else if (dto.Role == "Encadrant")
        {
            _ctx.Supervisors.Add(new Supervisor
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                NomComplet = dto.FullName,
                Fonction = "Encadrant (Auto)",
                Service = "Non renseigné"
            });
        }
        else if (dto.Role == "School")
        {
            _ctx.Schools.Add(new School
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                NomEtablissement = dto.FullName,
                Adresse = "Non renseigné",
                Telephone = "Non renseigné",
                EmailContact = dto.Email
            });
        }

        await _ctx.SaveChangesAsync();

        return Ok(new { message = "Utilisateur créé avec succès" });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser([FromRoute] Guid id, [FromBody] AdminUpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Utilisateur introuvable." });

        user.FullName = dto.FullName ?? user.FullName;
        user.IsActive = dto.IsActive;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) return BadRequest(new { message = "Erreur lors de la mise à jour de l'utilisateur.", errors = updateResult.Errors });

        if (!string.IsNullOrEmpty(dto.Role) && dto.Role != "No Role")
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            
            // Remove roles that are not the new role
            var rolesToRemove = currentRoles.Where(r => r != dto.Role).ToList();
            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }
            
            // Add the new role if not already in it
            if (!currentRoles.Contains(dto.Role))
            {
                var addResult = await _userManager.AddToRoleAsync(user, dto.Role);
                if (!addResult.Succeeded) return BadRequest(new { message = "Erreur lors de l'ajout du rôle.", errors = addResult.Errors });
            }
        }

        return Ok(new { message = "Utilisateur mis à jour" });
    }

    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetUserPassword([FromRoute] Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Utilisateur introuvable." });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        string newPassword = "Temp@" + new Random().Next(1000, 9999) + "!";
        
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded) return BadRequest(new { message = "Erreur de réinitialisation.", errors = result.Errors.Select(e => e.Description) });

        return Ok(new { password = newPassword });
    }

    [AllowAnonymous]
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailDto dto)
    {
        await _email.SendEmailAsync(dto.To, dto.Subject, dto.Body);
        return Ok(new { message = "Email envoyé ou simulé (consultez la console)." });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            TotalStudents = await _ctx.Students.CountAsync(),
            TotalOffers = await _ctx.InternshipOffers.CountAsync(),
            ActiveInternships = await _ctx.Internships.CountAsync(i => i.Statut == Core.Enums.InternshipStatus.EnCours),
            TotalAgreements = await _ctx.InternshipAgreements.CountAsync(),
            InternshipsByYear = await _ctx.Internships
                .GroupBy(i => i.DateDebutEffective.Year)
                .Select(g => new { Year = g.Key.ToString(), Count = g.Count() })
                .OrderBy(g => g.Year)
                .ToListAsync(),
            AgreementsStatus = await _ctx.InternshipAgreements
                .GroupBy(a => a.Statut)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(),
            ReportsByType = await _ctx.InternshipReports
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                .ToListAsync()
        };
        return Ok(stats);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs()
    {
        var logs = await _ctx.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(100)
            .ToListAsync();
        return Ok(logs);
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _ctx.AppSettings.ToListAsync();
        return Ok(settings);
    }

    [HttpPut("settings/{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] string value)
    {
        var setting = await _ctx.AppSettings.FirstOrDefaultAsync(s => s.Cle == key);
        if (setting == null) return NotFound();
        setting.Valeur = value;
        await _ctx.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>POST /api/admin/import-historical — Importer les stagiaires historiques depuis Excel</summary>
    [HttpPost("import-historical")]
    [AllowAnonymous]
    public async Task<IActionResult> ImportHistorical([FromQuery] string filePath = @"C:\Users\pc gz\Desktop\saadboutahlil\PROJETS\stage info 23-26.xlsx")
    {
        if (!System.IO.File.Exists(filePath))
        {
            return BadRequest(new { message = $"Le fichier Excel spécifié n'existe pas : {filePath}" });
        }

        var logBuilder = new StringBuilder();
        var rows = new List<ExcelRowData>();

        try
        {
            using var workbook = new XLWorkbook(filePath);

            // 1. Lire Feuil1
            var sheet1 = workbook.Worksheets.FirstOrDefault(w => w.Name == "Feuil1" || w.Name == "Sheet1" || w.Name.Contains("1"));
            if (sheet1 != null)
            {
                var usedRange = sheet1.RangeUsed();
                if (usedRange != null)
                {
                    var rowCount = usedRange.RowCount();
                    for (int r = 2; r <= rowCount; r++)
                    {
                        var row = sheet1.Row(r);
                        var nom = row.Cell(1).Value.ToString();
                        var prenom = row.Cell(2).Value.ToString();
                        var date = row.Cell(3).Value.ToString();
                        var info = row.Cell(4).Value.ToString();

                        if (string.IsNullOrWhiteSpace(nom) && string.IsNullOrWhiteSpace(prenom))
                            continue;

                        rows.Add(new ExcelRowData
                        {
                            RowNum = r,
                            SheetName = sheet1.Name,
                            NomRaw = nom,
                            PrenomRaw = prenom,
                            NomClean = CleanName(nom),
                            PrenomClean = CleanName(prenom),
                            DateRaw = date,
                            InfoRaw = info
                        });
                    }
                }
            }

            // 2. Lire Feuil2
            var sheet2 = workbook.Worksheets.FirstOrDefault(w => w.Name == "Feuil2" || w.Name == "Sheet2" || w.Name.Contains("2"));
            if (sheet2 != null)
            {
                var usedRange = sheet2.RangeUsed();
                if (usedRange != null)
                {
                    var rowCount = usedRange.RowCount();
                    for (int r = 2; r <= rowCount; r++)
                    {
                        var row = sheet2.Row(r);
                        var nom = row.Cell(1).Value.ToString();
                        var prenom = row.Cell(2).Value.ToString();
                        var date = row.Cell(3).Value.ToString();
                        var info = row.Cell(4).Value.ToString();
                        var phone = row.Cell(5).Value.ToString();

                        if (string.IsNullOrWhiteSpace(nom) && string.IsNullOrWhiteSpace(prenom))
                            continue;

                        rows.Add(new ExcelRowData
                        {
                            RowNum = r,
                            SheetName = sheet2.Name,
                            NomRaw = nom,
                            PrenomRaw = prenom,
                            NomClean = CleanName(nom),
                            PrenomClean = CleanName(prenom),
                            DateRaw = date,
                            InfoRaw = info,
                            PhoneRaw = phone,
                            PhoneClean = CleanPhone(phone)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erreur lors de la lecture ClosedXML du fichier Excel", error = ex.Message });
        }

        var processedRows = new List<ExcelRowData>();
        var ignoredRows = new List<ExcelRowData>();

        foreach (var r in rows)
        {
            if (string.IsNullOrEmpty(r.NomClean) || string.IsNullOrEmpty(r.PrenomClean))
            {
                r.Errors.Add("Nom ou prénom manquant ou invalide.");
                ignoredRows.Add(r);
                continue;
            }

            var dateRes = ParseDates(r.DateRaw);
            if (!dateRes.success)
            {
                r.Year = dateRes.year;
                r.StartDate = dateRes.start;
                r.EndDate = dateRes.end;
                r.Errors.Add(dateRes.error);
                ignoredRows.Add(r);
                continue;
            }

            r.Year = dateRes.year;
            r.StartDate = dateRes.start;
            r.EndDate = dateRes.end;
            processedRows.Add(r);
        }

        var groups = processedRows
            .GroupBy(r => new { r.NomClean, r.PrenomClean, r.Year })
            .ToList();

        var importedCount = 0;
        var duplicateCount = 0;

        logBuilder.AppendLine("=== RAPPORT D'IMPORTATION DES STAGIAIRES HISTORIQUES ===");
        logBuilder.AppendLine($"Date de l'import : {DateTime.Now}");
        logBuilder.AppendLine($"Fichier source : {filePath}");
        logBuilder.AppendLine($"Total lignes lues (sans en-têtes ni lignes vides) : {rows.Count}");
        logBuilder.AppendLine($"Lignes avec erreurs de formatage ou ignorées : {ignoredRows.Count}");
        logBuilder.AppendLine($"Groupes d'étudiants uniques à importer : {groups.Count}");
        logBuilder.AppendLine();

        logBuilder.AppendLine("--- LIGNES IGNORÉES ---");
        foreach (var r in ignoredRows)
        {
            logBuilder.AppendLine($"[FEUILLE: {r.SheetName} - LIGNE: {r.RowNum}] Nom brut: '{r.NomRaw}' Prénom brut: '{r.PrenomRaw}' - Erreurs: {string.Join(", ", r.Errors)}");
        }
        logBuilder.AppendLine();

        logBuilder.AppendLine("--- DÉROULEMENT DE L'IMPORTATION ---");

        using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            // Assurer que la colonne IsArchived existe
            await _ctx.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Internships') AND name = 'IsArchived')
                BEGIN
                    ALTER TABLE Internships ADD IsArchived BIT NOT NULL DEFAULT 0;
                END");

            // Assurer que la direction existe
            var direction = await _ctx.Directions.FirstOrDefaultAsync(d => d.Sigle == "DSI") 
                            ?? await _ctx.Directions.FirstOrDefaultAsync();
            if (direction == null)
            {
                direction = new Direction
                {
                    Id = Guid.NewGuid(),
                    Nom = "Direction des Systèmes d'Information",
                    Sigle = "DSI",
                    Email = "dsi@men.gov.ma"
                };
                _ctx.Directions.Add(direction);
                await _ctx.SaveChangesAsync();
            }

            // Assurer que l'offre existe
            var offer = await _ctx.InternshipOffers.FirstOrDefaultAsync(o => o.Titre == "Offre d'archivage historique");
            if (offer == null)
            {
                offer = new InternshipOffer
                {
                    Id = Guid.NewGuid(),
                    DirectionId = direction.Id,
                    Titre = "Offre d'archivage historique",
                    Description = "Offre automatique servant à rattacher les stages historiques importés d'Excel.",
                    Competences = "N/A",
                    DateDebut = new DateOnly(2023, 1, 1),
                    DateFin = new DateOnly(2026, 12, 31),
                    NombrePostes = 9999,
                    Lieu = "Rabat",
                    Statut = OfferStatus.Archivee
                };
                _ctx.InternshipOffers.Add(offer);
                await _ctx.SaveChangesAsync();
            }

            // Charger le compteur CNE
            var lastArchCne = await _ctx.Students
                .Where(s => s.CNE.StartsWith("ARCH_"))
                .Select(s => s.CNE)
                .ToListAsync();
            int maxSeq = 0;
            foreach (var c in lastArchCne)
            {
                if (int.TryParse(c.Replace("ARCH_", ""), out int seq))
                {
                    if (seq > maxSeq) maxSeq = seq;
                }
            }
            int cneCounter = maxSeq + 1;

            foreach (var group in groups)
            {
                var nomClean = group.Key.NomClean;
                var prenomClean = group.Key.PrenomClean;
                var year = group.Key.Year;

                var infos = group.Select(g => g.InfoRaw).Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
                var infoMerged = string.Join(" / ", infos);

                var phoneMerged = group.Select(g => g.PhoneClean).FirstOrDefault(p => !string.IsNullOrEmpty(p)) ?? "";

                var startDate = group.Select(g => g.StartDate).FirstOrDefault(d => d.HasValue);
                var endDate = group.Select(g => g.EndDate).FirstOrDefault(d => d.HasValue);

                if (group.Count() > 1)
                {
                    duplicateCount += (group.Count() - 1);
                    logBuilder.AppendLine($"[FUSION - DOUBLON] {ToTitleCase(prenomClean)} {ToTitleCase(nomClean)} ({year}) fusionné à partir de {group.Count()} lignes.");
                }

                // 1. ApplicationUser
                var cleanNomForEmail = NormalizeForEmail(nomClean);
                var cleanPrenomForEmail = NormalizeForEmail(prenomClean);
                var userEmail = $"{cleanNomForEmail}.{cleanPrenomForEmail}@{year}.archive.men.gov.ma";
                var user = await _userManager.FindByEmailAsync(userEmail);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = userEmail,
                        Email = userEmail,
                        FullName = $"{ToTitleCase(prenomClean)} {ToTitleCase(nomClean)}",
                        EmailConfirmed = true,
                        IsActive = false,
                        LockoutEnabled = true,
                        PasswordHash = null,
                        PhoneNumber = string.IsNullOrEmpty(phoneMerged) ? null : phoneMerged,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    var createRes = await _userManager.CreateAsync(user);
                    if (!createRes.Succeeded)
                    {
                        logBuilder.AppendLine($"[ERREUR] Impossible de créer l'utilisateur {userEmail} : {string.Join(", ", createRes.Errors.Select(e => e.Description))}");
                        continue;
                    }
                    await _userManager.AddToRoleAsync(user, "Student");
                }

                // 2. Student
                var student = await _ctx.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student == null)
                {
                    var cne = $"ARCH_{cneCounter++:D6}";
                    student = new Student
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        CNE = cne,
                        Filiere = "Historique",
                        Promotion = year == "Inconnue" ? "Inconnue" : $"Promotion {year}",
                        Etablissement = "Inconnu (Historique)"
                    };
                    _ctx.Students.Add(student);
                    await _ctx.SaveChangesAsync();
                }

                // 3. Application
                var app = await _ctx.InternshipApplications.FirstOrDefaultAsync(a => a.StudentId == student.Id && a.OfferId == offer.Id);
                if (app == null)
                {
                    app = new InternshipApplication
                    {
                        Id = Guid.NewGuid(),
                        StudentId = student.Id,
                        OfferId = offer.Id,
                        Statut = ApplicationStatus.Acceptee,
                        Message = "Candidature historique créée lors de l'importation."
                    };
                    _ctx.InternshipApplications.Add(app);
                    await _ctx.SaveChangesAsync();
                }

                // 4. Agreement
                var agreement = await _ctx.InternshipAgreements.FirstOrDefaultAsync(a => a.ApplicationId == app.Id);
                if (agreement == null)
                {
                    agreement = new InternshipAgreement
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = app.Id,
                        DateDebut = startDate ?? new DateOnly(int.TryParse(year, out int yStart) ? yStart : 2025, 1, 1),
                        DateFin = endDate ?? new DateOnly(int.TryParse(year, out int yEnd) ? yEnd : 2025, 12, 31),
                        Statut = AgreementStatus.Signee,
                        Missions = string.IsNullOrWhiteSpace(infoMerged) ? "Stage historique" : infoMerged,
                        SignatureEtudiantAt = DateTime.UtcNow,
                        SignatureRHAt = DateTime.UtcNow,
                        SignatureEcoleAt = DateTime.UtcNow
                    };
                    _ctx.InternshipAgreements.Add(agreement);
                    await _ctx.SaveChangesAsync();
                }

                // 5. Internship
                var internship = await _ctx.Internships.FirstOrDefaultAsync(i => i.AgreementId == agreement.Id);
                if (internship == null)
                {
                    internship = new Internship
                    {
                        Id = Guid.NewGuid(),
                        AgreementId = agreement.Id,
                        Sujet = "Stage non renseigné (import historique)",
                        DescriptionDetaillee = string.IsNullOrWhiteSpace(infoMerged) ? "Aucune information de dossier complémentaire." : $"Documents mentionnés: {infoMerged}",
                        DateDebutEffective = startDate ?? new DateOnly(int.TryParse(year, out int yStart2) ? yStart2 : 2025, 1, 1),
                        DateFinEffective = endDate,
                        Statut = InternshipStatus.Termine,
                        IsArchived = true,
                        DemarreAt = DateTime.UtcNow,
                        TermineAt = DateTime.UtcNow
                    };
                    _ctx.Internships.Add(internship);
                    await _ctx.SaveChangesAsync();
                }

                importedCount++;
                logBuilder.AppendLine($"[SUCCÈS] Importé : {ToTitleCase(prenomClean)} {ToTitleCase(nomClean)} ({year}) -> Email: {userEmail} | CNE: {student.CNE}");
            }

            await transaction.CommitAsync();
            logBuilder.AppendLine();
            logBuilder.AppendLine("=== FIN DE L'IMPORTATION ===");
            logBuilder.AppendLine($"Total importés avec succès : {importedCount}");
            logBuilder.AppendLine($"Total doublons fusionnés : {duplicateCount}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logBuilder.AppendLine($"[ERREUR CRITIQUE] L'importation a échoué et a été annulée (rollback) : {ex.Message}");
            logBuilder.AppendLine(ex.StackTrace);
            
            var errLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"import_error_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await System.IO.File.WriteAllTextAsync(errLogPath, logBuilder.ToString(), Encoding.UTF8);

            return StatusCode(500, new { 
                message = "Une erreur est survenue lors de l'importation.", 
                error = ex.Message,
                logFile = errLogPath
            });
        }

        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"import_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        await System.IO.File.WriteAllTextAsync(logPath, logBuilder.ToString(), Encoding.UTF8);

        return Ok(new {
            message = "L'importation historique a été effectuée avec succès.",
            importedCount,
            duplicateCount,
            logFile = logPath,
            details = logBuilder.ToString()
        });
    }

    private class ExcelRowData
    {
        public int RowNum { get; set; }
        public string SheetName { get; set; } = "";
        public string NomRaw { get; set; } = "";
        public string PrenomRaw { get; set; } = "";
        public string NomClean { get; set; } = "";
        public string PrenomClean { get; set; } = "";
        public string DateRaw { get; set; } = "";
        public string InfoRaw { get; set; } = "";
        public string PhoneRaw { get; set; } = "";
        public string PhoneClean { get; set; } = "";
        public string Year { get; set; } = "Inconnue";
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    private static string CleanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var clean = Regex.Replace(name, @"\s+", " ").Trim();
        clean = Regex.Replace(clean, @"\+D\d+.*$", "");
        clean = Regex.Replace(clean, @"[^a-zA-Z\s\-\'\u00C0-\u017F]", "");
        return clean.Trim().ToLowerInvariant();
    }

    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(input.ToLowerInvariant());
    }

    private static string CleanPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var clean = Regex.Replace(phone, @"[\s\.\-]", "");
        if (clean.StartsWith("+212")) clean = clean.Substring(4);
        if (clean.StartsWith("212")) clean = clean.Substring(3);
        if (clean.Length == 9 && (clean.StartsWith("6") || clean.StartsWith("7") || clean.StartsWith("5")))
        {
            clean = "0" + clean;
        }
        return clean;
    }

    private static bool HasWeirdChars(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return Regex.IsMatch(name, @"[^a-zA-Z\s\-\'\u00C0-\u017F]");
    }

    private static DateOnly? ParseSingleDate(string part, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(part))
            return null;

        var p = part.Trim();
        if (p.Equals("Non renseigné", StringComparison.OrdinalIgnoreCase) || 
            p.Equals("Non renseigne", StringComparison.OrdinalIgnoreCase) ||
            p.Equals("NonRenseigne", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Correct year typos: e.g. "/025" -> "/2025" or "/025 " -> "/2025"
        p = Regex.Replace(p, @"/0(\d{2})\b", "/20$1");

        // Correct year typos where the slash is missing, e.g. "7/72025" -> "7/7/2025"
        if (p.Count(c => c == '/') == 1)
        {
            p = Regex.Replace(p, @"^(\d{1,2})/(\d{1,2})(\d{4})$", "$1/$2/$3");
        }

        // Try double (OADate serial)
        if (double.TryParse(p, CultureInfo.InvariantCulture, out double serial))
        {
            try
            {
                var dt = DateTime.FromOADate(serial);
                return DateOnly.FromDateTime(dt);
            }
            catch
            {
                error = $"Série Excel invalide : {p}";
                return null;
            }
        }

        var dateFormats = new[] { 
            "d/M/yyyy", "d/M/yy", "dd/MM/yyyy", "dd/MM/yy",
            "M/d/yyyy", "M/d/yy", "yyyy-MM-dd", "yyyy/MM/dd",
            "d-M-yyyy", "dd-MM-yyyy", "yyyy-M-d"
        };

        if (DateTime.TryParseExact(p, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDt))
        {
            return DateOnly.FromDateTime(parsedDt);
        }
        
        if (DateTime.TryParse(p, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDt2))
        {
            return DateOnly.FromDateTime(parsedDt2);
        }

        error = $"Format de date non reconnu : {p}";
        return null;
    }

    private static (bool success, string year, DateOnly? start, DateOnly? end, string error) ParseDates(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return (false, "Inconnue", null, null, "Date manquante");

        var cleaned = Regex.Replace(dateStr, @"\s+", " ").Trim();

        // 1. Check for dummy date like "0/0/2025"
        if (Regex.IsMatch(cleaned, @"^0/0/\d{4}$"))
        {
            var y = cleaned.Split('/').Last();
            return (false, y, null, null, $"Date factice : {cleaned}");
        }

        // 2. Check for pure year of 4 digits
        if (Regex.IsMatch(cleaned, @"^\d{4}$"))
        {
            if (int.TryParse(cleaned, out int yVal) && yVal >= 2020 && yVal <= 2026)
            {
                return (true, cleaned, null, null, "");
            }
            return (false, cleaned, null, null, $"Année hors plage (2020-2026) : {cleaned}");
        }

        // 3. Normalize separators to |
        var rangeStr = cleaned;

        // Temporarily mask "Non renseigné" / "Non renseigne" to prevent splitting on its spaces
        rangeStr = Regex.Replace(rangeStr, @"Non renseigné", "NonRenseigne", RegexOptions.IgnoreCase);
        rangeStr = Regex.Replace(rangeStr, @"Non renseigne", "NonRenseigne", RegexOptions.IgnoreCase);

        // Separators with any amount of whitespace around: "au", "à", "to", "–", "—" (en/em dashes)
        rangeStr = Regex.Replace(rangeStr, @"\s*(?:au|à|to|–|—)\s*", "|", RegexOptions.IgnoreCase);

        // Separator "a" with spaces or between digits
        rangeStr = Regex.Replace(rangeStr, @"\s+a\s+", "|", RegexOptions.IgnoreCase);
        rangeStr = Regex.Replace(rangeStr, @"(?<=\d)a(?=\d)", "|", RegexOptions.IgnoreCase);

        // Dashes / hyphens "-"
        // 1. Dash with spaces around
        rangeStr = Regex.Replace(rangeStr, @"\s+-\s+", "|");
        // 2. Dash between a digit and "NonRenseigne" (with optional spaces)
        rangeStr = Regex.Replace(rangeStr, @"(?<=\d)\s*-\s*(?=NonRenseigne)", "|", RegexOptions.IgnoreCase);
        // 3. If it contains slashes, any remaining dash can be assumed a range separator
        if (rangeStr.Contains("/"))
        {
            rangeStr = Regex.Replace(rangeStr, @"-", "|");
        }
        // 4. Dash between serial numbers (4 to 5 digits)
        rangeStr = Regex.Replace(rangeStr, @"(?<=\d{4,5})-(?=\d{4,5})", "|");

        // If there are still spaces in the string, they are likely separating the start and end date (e.g., "12/6/2025 11/7/2025")
        // Replace spaces with '|'
        rangeStr = Regex.Replace(rangeStr, @"\s+", "|");

        var parts = rangeStr.Split('|', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return (false, "Inconnue", null, null, $"Format de date non supporté : {dateStr}");
        }

        DateOnly? start = null;
        DateOnly? end = null;
        string year = "Inconnue";

        // Parse start date (first part)
        var p1 = parts[0].Trim();
        if (p1.Equals("NonRenseigne", StringComparison.OrdinalIgnoreCase))
        {
            p1 = "Non renseigné";
        }
        var startParsed = ParseSingleDate(p1, out string err1);
        if (!startParsed.HasValue)
        {
            return (false, "Inconnue", null, null, $"Date de début invalide : {p1}. {err1}");
        }
        start = startParsed.Value;
        year = start.Value.Year.ToString();

        // Check if start year is within range
        if (start.Value.Year < 2020 || start.Value.Year > 2026)
        {
            return (false, year, null, null, $"Année de début {start.Value.Year} hors plage (2020-2026)");
        }

        // Parse end date (second part if exists)
        if (parts.Length >= 2)
        {
            var p2 = parts[1].Trim();
            if (p2.Equals("NonRenseigne", StringComparison.OrdinalIgnoreCase))
            {
                p2 = "Non renseigné";
            }

            if (p2.Equals("Non renseigné", StringComparison.OrdinalIgnoreCase) || 
                p2.Equals("Non renseigne", StringComparison.OrdinalIgnoreCase) || 
                string.IsNullOrWhiteSpace(p2))
            {
                end = null;
            }
            else
            {
                var endParsed = ParseSingleDate(p2, out string err2);
                if (endParsed.HasValue)
                {
                    end = endParsed.Value;
                    // Check if end year is within range
                    if (end.Value.Year < 2020 || end.Value.Year > 2026)
                    {
                        return (false, year, start, null, $"Année de fin {end.Value.Year} hors plage (2020-2026)");
                    }
                }
                else
                {
                    return (false, year, start, null, $"Date de fin invalide : {p2}. {err2}");
                }
            }
        }

        return (true, year, start, end, "");
    }

    private static string NormalizeForEmail(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        
        // Remove accents
        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        var asciiOnly = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // Keep only lowercase letters and numbers, remove spaces, dashes, apostrophes, etc.
        var clean = Regex.Replace(asciiOnly.ToLowerInvariant(), @"[^a-z0-9]", "");
        return clean;
    }
}
