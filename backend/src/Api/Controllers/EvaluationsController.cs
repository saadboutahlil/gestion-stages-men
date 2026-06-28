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
public class EvaluationsController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public EvaluationsController(AppDbContext ctx) => _ctx = ctx;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>POST /api/evaluations — Créer une évaluation (Encadrant)</summary>
    [HttpPost]
    [Authorize(Roles = "Encadrant")]
    public async Task<IActionResult> Create([FromBody] CreateEvaluationDto dto)
    {
        var userId = GetUserId();
        var supervisor = await _ctx.Supervisors.FirstOrDefaultAsync(s => s.UserId == userId);
        if (supervisor == null) return BadRequest(new { error = "Profil encadrant introuvable." });

        var evalType = dto.Type.ToLower() == "finale" ? EvaluationType.Finale : EvaluationType.MiParcours;

        if (evalType == EvaluationType.Finale)
        {
            var hasFinalReport = await _ctx.InternshipReports.AnyAsync(r => r.InternshipId == dto.InternshipId && r.Type == ReportType.Final);
            if (!hasFinalReport)
                return BadRequest(new { error = "Impossible de créer une évaluation finale : l'étudiant n'a pas encore déposé son rapport final." });
        }

        var evaluation = new InternshipEvaluation
        {
            InternshipId = dto.InternshipId,
            EvaluateurId = supervisor.Id,
            Type = evalType,
            NoteTechnique = dto.NoteTechnique,
            NoteComportement = dto.NoteComportement,
            NoteAutonomie = dto.NoteAutonomie,
            NoteGlobale = dto.NoteGlobale,
            PointsForts = dto.PointsForts,
            PointsAmeliorer = dto.PointsAmeliorer,
            Recommandations = dto.Recommandations
        };

        _ctx.InternshipEvaluations.Add(evaluation);
        await _ctx.SaveChangesAsync();
        return Ok(new { evaluation.Id, message = "Évaluation enregistrée." });
    }

    /// <summary>GET /api/evaluations/my — Mes évaluations (Encadrant)</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Encadrant")]
    public async Task<IActionResult> MyEvaluations()
    {
        var userId = GetUserId();
        var supervisor = await _ctx.Supervisors.FirstOrDefaultAsync(s => s.UserId == userId);
        if (supervisor == null) return Ok(Array.Empty<object>());

        var evals = await _ctx.InternshipEvaluations
            .Include(e => e.Internship).ThenInclude(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Where(e => e.EvaluateurId == supervisor.Id)
            .OrderByDescending(e => e.DateEvaluation)
            .ToListAsync();

        return Ok(evals.Select(e => new
        {
            e.Id, Type = e.Type.ToString(),
            e.NoteTechnique, e.NoteComportement, e.NoteAutonomie, e.NoteGlobale,
            e.PointsForts, e.PointsAmeliorer, e.Recommandations,
            e.DateEvaluation,
            Etudiant = e.Internship?.Agreement?.Application?.Student?.User?.FullName,
            Stage = e.Internship?.Sujet
        }));
    }

    /// <summary>GET /api/evaluations/internship/{id} — Évaluations d'un stage</summary>
    [HttpGet("internship/{internshipId}")]
    public async Task<IActionResult> ByInternship(Guid internshipId)
    {
        var evals = await _ctx.InternshipEvaluations
            .Include(e => e.Evaluateur)
            .Where(e => e.InternshipId == internshipId)
            .ToListAsync();

        return Ok(evals.Select(e => new
        {
            e.Id, Type = e.Type.ToString(),
            e.NoteTechnique, e.NoteComportement, e.NoteAutonomie, e.NoteGlobale,
            e.PointsForts, e.PointsAmeliorer, e.Recommandations,
            e.DateEvaluation,
            Evaluateur = e.Evaluateur?.NomComplet
        }));
    }
}
