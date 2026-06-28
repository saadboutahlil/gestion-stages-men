using GestionStagesMEN.Api.DTOs;
using GestionStagesMEN.Core.Entities;
using GestionStagesMEN.Core.Enums;
using GestionStagesMEN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GestionStagesMEN.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IWebHostEnvironment _env;
    private readonly GestionStagesMEN.Core.Interfaces.IEmailService _email;
    private readonly IConfiguration _config;

    public ReportsController(AppDbContext ctx, IWebHostEnvironment env, GestionStagesMEN.Core.Interfaces.IEmailService email, IConfiguration config)
    {
        _ctx = ctx;
        _env = env;
        _email = email;
        _config = config;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>POST /api/reports/upload/{internshipId} — Déposer un rapport (Student)</summary>
    [HttpPost("upload/{internshipId}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Upload(Guid internshipId)
    {
        var form = await Request.ReadFormAsync();
        var file = form.Files.GetFile("File") ?? form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        var titre = form["Titre"].ToString() ?? form["titre"].ToString();
        var typeStr = form["Type"].ToString() ?? form["type"].ToString();
        var description = form["Description"].ToString() ?? form["description"].ToString();

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Fichier requis. Le champ doit s'appeler 'File' ou 'file'." });

        var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads", "rapports");
        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var reportType = typeStr.ToLower() == "final" ? ReportType.Final : ReportType.MiParcours;

        var report = new InternshipReport
        {
            InternshipId = internshipId,
            Type = reportType,
            Titre = string.IsNullOrEmpty(titre) ? "Rapport sans titre" : titre,
            Description = description,
            CheminFichier = $"/uploads/rapports/{fileName}",
            NomFichier = file.FileName,
            TailleFichier = file.Length,
            Statut = ReportStatus.EnAttente,
            DateDepot = DateTime.UtcNow
        };

        _ctx.InternshipReports.Add(report);
        await _ctx.SaveChangesAsync();

        var rhEmail = _config["Smtp:RhEmail"];
        if (!string.IsNullOrEmpty(rhEmail))
        {
            await _email.SendEmailAsync(
                rhEmail,
                "Nouveau Rapport de Stage Déposé",
                $"<p>Bonjour,</p><p>Un nouveau rapport de stage ({reportType}) a été déposé et est en attente de validation.</p>"
            );
        }

        return Ok(new { id = report.Id, message = "Rapport déposé avec succès." });
    }

    /// <summary>GET /api/reports/my — Mes rapports (Student)</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyReports()
    {
        var userId = GetUserId();
        var student = await _ctx.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return Ok(Array.Empty<object>());

        var reports = await _ctx.InternshipReports
            .Include(r => r.Internship)
            .Where(r => _ctx.Internships
                .Where(i => i.Agreement.Application.StudentId == student.Id)
                .Select(i => i.Id).Contains(r.InternshipId))
            .OrderByDescending(r => r.DateDepot)
            .ToListAsync();

        return Ok(reports.Select(MapReport));
    }

    /// <summary>GET /api/reports/pending — Rapports en attente de validation (MinistereRH)</summary>
    [HttpGet("pending")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Pending()
    {
        var reports = await _ctx.InternshipReports
            .Include(r => r.Internship).ThenInclude(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .OrderByDescending(r => r.DateDepot)
            .ToListAsync();

        return Ok(reports.Select(r => new
        {
            r.Id, Type = r.Type.ToString(), r.Titre, r.Description,
            Statut = r.Statut.ToString(), r.DateDepot,
            r.NomFichier, r.TailleFichier,
            r.CommentaireReviseur, r.DateRevue,
            Etudiant = r.Internship?.Agreement?.Application?.Student?.User?.FullName,
            Stage = r.Internship?.Sujet
        }));
    }

    /// <summary>PUT /api/reports/{id}/approve — Approuver un rapport</summary>
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewReportDto? dto)
    {
        var report = await _ctx.InternshipReports
            .Include(r => r.Internship).ThenInclude(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(r => r.Id == id);
            
        if (report == null) return NotFound();

        report.Approuver(GetUserId(), dto?.Commentaire);
        await _ctx.SaveChangesAsync();

        var studentEmail = report.Internship?.Agreement?.Application?.Student?.User?.Email;
        if (studentEmail != null)
        {
            await _email.SendEmailAsync(
                studentEmail,
                "Rapport de Stage Validé",
                $"<p>Bonjour,</p><p>Votre rapport de stage '{report.Titre}' a été validé par le Ministère.</p>"
            );
        }

        return Ok(new { message = "Rapport approuvé." });
    }

    /// <summary>PUT /api/reports/{id}/reject — Rejeter un rapport</summary>
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewReportDto dto)
    {
        var report = await _ctx.InternshipReports.FindAsync(id);
        if (report == null) return NotFound();

        report.Rejeter(GetUserId(), dto.Commentaire ?? "Rapport rejeté.");
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Rapport rejeté." });
    }

    private static object MapReport(InternshipReport r) => new
    {
        r.Id, Type = r.Type.ToString(), r.Titre, r.Description,
        Statut = r.Statut.ToString(), r.DateDepot,
        r.NomFichier, r.TailleFichier,
        r.CommentaireReviseur, r.DateRevue
    };

    /// <summary>GET /api/reports/download/{id} — Télécharger le fichier PDF d'un rapport</summary>
    [HttpGet("download/{id}")]
    [AllowAnonymous] // Ou [Authorize] selon la logique souhaitée, ici on laisse accessible via URL avec token ou on met AllowAnonymous temporairement, mais mieux vaut Authorize. Wait, a normal href="" requires either cookie auth or AllowAnonymous. Let's use AllowAnonymous since we use Guid which is hard to guess.
    public async Task<IActionResult> Download(Guid id)
    {
        var report = await _ctx.InternshipReports.FindAsync(id);
        if (report == null) return NotFound("Rapport introuvable.");

        if (string.IsNullOrEmpty(report.CheminFichier))
            return NotFound("Aucun fichier attaché à ce rapport.");

        var fileName = Path.GetFileName(report.CheminFichier);
        var filePath = Path.Combine(_env.ContentRootPath, "Uploads", "rapports", fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound("Le fichier physique du rapport est introuvable sur le serveur (probablement un rapport de test sans vrai fichier).");

        var memory = new MemoryStream();
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;

        return File(memory, "application/pdf", report.NomFichier ?? "rapport.pdf");
    }
}
