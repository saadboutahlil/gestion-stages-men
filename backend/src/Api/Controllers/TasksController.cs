using GestionStagesMEN.Api.DTOs;
using GestionStagesMEN.Core.Entities;
using GestionStagesMEN.Core.Enums;
using GestionStagesMEN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public TasksController(AppDbContext ctx) => _ctx = ctx;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>GET /api/tasks/internship/{id} — Tâches d'un stage</summary>
    [HttpGet("internship/{internshipId}")]
    public async Task<IActionResult> GetByInternship(Guid internshipId)
    {
        var tasks = await _ctx.InternshipTasks
            .Where(t => t.InternshipId == internshipId)
            .OrderBy(t => t.DatePrevue)
            .ToListAsync();

        return Ok(tasks.Select(t => new
        {
            t.Id, t.Titre, t.Description, Statut = t.Statut.ToString(),
            t.DatePrevue, t.DateCompletion
        }));
    }

    /// <summary>POST /api/tasks — Ajouter une tâche (Encadrant)</summary>
    [HttpPost]
    [Authorize(Roles = "Encadrant")]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var userId = GetUserId();
        var supervisor = await _ctx.Supervisors.FirstOrDefaultAsync(s => s.UserId == userId);
        if (supervisor == null) return Forbid();

        var internship = await _ctx.Internships.FindAsync(dto.InternshipId);
        if (internship == null) return NotFound(new { error = "Stage introuvable." });

        // Vérification souple : l'encadrant est soit assigné via SupervisorId,
        // soit c'est le seul encadrant du système (fallback pour les stages sans affectation formelle)
        bool isAssigned = internship.SupervisorId == supervisor.Id;
        bool hasNoSupervisor = internship.SupervisorId == null;

        if (!isAssigned && !hasNoSupervisor)
            return BadRequest(new { error = "Vous n'êtes pas l'encadrant assigné à ce stage." });

        var task = new InternshipTask
        {
            InternshipId = dto.InternshipId,
            Titre = dto.Titre,
            Description = dto.Description,
            DatePrevue = dto.DatePrevue
        };

        _ctx.InternshipTasks.Add(task);
        await _ctx.SaveChangesAsync();
        return Ok(new { task.Id, message = "Tâche ajoutée." });
    }

    /// <summary>PUT /api/tasks/{id}/start — Démarrer une tâche</summary>
    [HttpPut("{id}/start")]
    public async Task<IActionResult> Start(Guid id)
    {
        var task = await _ctx.InternshipTasks.FindAsync(id);
        if (task == null) return NotFound();

        task.Demarrer();
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Tâche démarrée." });
    }

    /// <summary>PUT /api/tasks/{id}/complete — Terminer une tâche</summary>
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var task = await _ctx.InternshipTasks.FindAsync(id);
        if (task == null) return NotFound();

        task.Terminer();
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Tâche terminée." });
    }

    /// <summary>DELETE /api/tasks/{id}</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Encadrant")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _ctx.InternshipTasks.Include(t => t.Internship).ThenInclude(i => i.Agreement).ThenInclude(a => a.Application).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return NotFound();

        var userId = GetUserId();
        var supervisor = await _ctx.Supervisors.Include(s => s.Supervisions).FirstOrDefaultAsync(s => s.UserId == userId);
        if (supervisor == null) return Forbid();

        if (task.Internship.SupervisorId != supervisor.Id)
            return BadRequest(new { error = "Vous n'êtes pas l'encadrant assigné à ce stage." });

        _ctx.InternshipTasks.Remove(task);
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Tâche supprimée." });
    }
}
