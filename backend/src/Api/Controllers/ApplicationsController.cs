using GestionStagesMEN.Api.DTOs;
using GestionStagesMEN.Core.Entities;
using GestionStagesMEN.Core.Enums;
using GestionStagesMEN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.IO;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IWebHostEnvironment _env;
    private readonly GestionStagesMEN.Core.Interfaces.IEmailService _email;
    public ApplicationsController(AppDbContext ctx, IWebHostEnvironment env, GestionStagesMEN.Core.Interfaces.IEmailService email)
    {
        _ctx = ctx;
        _env = env;
        _email = email;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>POST /api/applications — Postuler à une offre (Student)</summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Apply([FromForm] ApplyDto dto, IFormFile cv, IFormFile lettre)
    {
        var userId = GetUserId();
        var student = await _ctx.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return BadRequest(new { error = "Profil étudiant introuvable." });

        if (cv == null || lettre == null)
            return BadRequest(new { error = "Le CV et la lettre de motivation sont obligatoires." });

        var exists = await _ctx.InternshipApplications.AnyAsync(a => a.StudentId == student.Id && a.OfferId == dto.OfferId);
        if (exists) return BadRequest(new { error = "Vous avez déjà postulé à cette offre." });

        var cvDir = Path.Combine(_env.ContentRootPath, "Uploads", "Cv");
        var lettreDir = Path.Combine(_env.ContentRootPath, "Uploads", "Lettres");
        
        if (!Directory.Exists(cvDir)) Directory.CreateDirectory(cvDir);
        if (!Directory.Exists(lettreDir)) Directory.CreateDirectory(lettreDir);

        var cvFileName = $"{Guid.NewGuid()}{Path.GetExtension(cv.FileName)}";
        var lettreFileName = $"{Guid.NewGuid()}{Path.GetExtension(lettre.FileName)}";

        var cvPath = Path.Combine(cvDir, cvFileName);
        using (var stream = new FileStream(cvPath, FileMode.Create)) { await cv.CopyToAsync(stream); }

        var lettrePath = Path.Combine(lettreDir, lettreFileName);
        using (var stream = new FileStream(lettrePath, FileMode.Create)) { await lettre.CopyToAsync(stream); }

        var app = new InternshipApplication
        {
            StudentId = student.Id,
            OfferId = dto.OfferId,
            Message = dto.Message,
            Statut = ApplicationStatus.Soumise, // Statut initial (En attente)
            CvPath = $"/uploads/cv/{cvFileName}",
            LettreMotivationPath = $"/uploads/lettres/{lettreFileName}",
            DatePostulation = DateTime.UtcNow
        };

        _ctx.InternshipApplications.Add(app);
        await _ctx.SaveChangesAsync();
        return Ok(new { app.Id, message = "Candidature envoyée avec succès !" });
    }

    /// <summary>GET /api/applications/my — Mes candidatures (Student)</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyApplications()
    {
        var userId = GetUserId();
        var student = await _ctx.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return Ok(Array.Empty<object>());

        var apps = await _ctx.InternshipApplications
            .Include(a => a.Offer).ThenInclude(o => o.Direction)
            .Include(a => a.Agreement)
            .Where(a => a.StudentId == student.Id)
            .OrderByDescending(a => a.DatePostulation)
            .ToListAsync();

        return Ok(apps.Select(a => new
        {
            a.Id, Statut = a.Statut.ToString(),
            Offre = a.Offer.Titre,
            Direction = a.Offer.Direction.Nom,
            a.DatePostulation, a.Message, a.MotifRefus,
            HasConvention = a.Agreement != null,
            ConventionId = a.Agreement?.Id
        }));
    }

    [HttpGet("received")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Received()
    {
        var userId = GetUserId();
        var direction = await _ctx.Directions.FirstOrDefaultAsync(d => d.UserId == userId);
        
        if (direction == null && !User.IsInRole("Admin"))
            return Ok(Array.Empty<object>());

        var query = _ctx.InternshipApplications
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Offer).ThenInclude(o => o.Direction)
            .Where(a => a.CvPath != null && a.Offer.Statut != OfferStatus.Archivee)
            .AsQueryable();

        if (!User.IsInRole("Admin"))
        {
            query = query.Where(a => a.Offer.DirectionId == direction!.Id);
        }

        var apps = await query
            .OrderByDescending(a => a.DatePostulation)
            .ToListAsync();

        return Ok(apps.Select(a => new
        {
            a.Id,
            Offre = a.Offer.Titre,
            Etudiant = a.Student.User.FullName,
            Filiere = a.Student.Filiere,
            Etablissement = a.Student.Etablissement,
            Statut = a.Statut.ToString(),
            a.DatePostulation,
            a.Message,
            a.MotifRefus,
            CvPath = a.CvPath,
            LettrePath = a.LettreMotivationPath,
            IsArchived = a.Offer.Statut == OfferStatus.Archivee
        }));
    }

    [HttpGet("accepted-for-school")]
    [Authorize]
    public async Task<IActionResult> AcceptedForSchool()
    {
        var apps = await _ctx.InternshipApplications
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Offer)
            .Where(a => a.Statut == ApplicationStatus.Acceptee && a.Offer.Statut != OfferStatus.Archivee)
            .ToListAsync();

        return Ok(apps.Select(a => new
        {
            Id = a.Id,
            Offre = a.Offer?.Titre ?? "Non renseigné",
            Etudiant = a.Student?.User?.FullName ?? "Inconnu",
            Filiere = a.Student?.Filiere ?? "Non renseignée",
            Etablissement = a.Student?.Etablissement ?? "Non renseigné",
            Statut = a.Statut.ToString(),
            DatePostulation = a.DatePostulation,
            Message = a.Message,
            IsArchived = a.Offer?.Statut == OfferStatus.Archivee
        }));
    }

    /// <summary>PUT /api/applications/{id}/accept</summary>
    [HttpPut("{id}/accept")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var app = await _ctx.InternshipApplications
            .Include(a => a.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app == null) return NotFound();

        app.Statut = ApplicationStatus.Acceptee;
        await _ctx.SaveChangesAsync();

        if (app.Student?.User?.Email != null)
        {
            await _email.SendEmailAsync(
                app.Student.User.Email,
                "Candidature Acceptée",
                "<p>Bonjour,</p><p>Félicitations, votre candidature a été acceptée. La convention de stage sera générée prochainement par votre établissement.</p>"
            );
        }

        return Ok(new { message = "Candidature acceptée." });
    }

    /// <summary>PUT /api/applications/{id}/reject</summary>
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewReportDto? dto)
    {
        var app = await _ctx.InternshipApplications.FindAsync(id);
        if (app == null) return NotFound();

        app.Statut = ApplicationStatus.Refusee;
        app.MotifRefus = dto?.Commentaire;
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Candidature refusée." });
    }
}
