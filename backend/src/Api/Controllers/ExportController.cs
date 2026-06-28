using ClosedXML.Excel;
using GestionStagesMEN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly AppDbContext _ctx;

    public ExportController(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    private void FormatHeader(IXLWorksheet worksheet, int cols)
    {
        var headerRange = worksheet.Range(1, 1, 1, cols);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportUsers()
    {
        var users = await _ctx.Users.ToListAsync();
        var userRoles = await _ctx.UserRoles.ToListAsync();
        var roles = await _ctx.Roles.ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Utilisateurs");

        // Headers
        ws.Cell(1, 1).Value = "ID";
        ws.Cell(1, 2).Value = "Nom Complet";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(1, 4).Value = "Rôle";
        ws.Cell(1, 5).Value = "Statut";
        ws.Cell(1, 6).Value = "Date de création";
        
        FormatHeader(ws, 6);

        // Data
        int row = 2;
        foreach (var u in users)
        {
            var roleId = userRoles.FirstOrDefault(ur => ur.UserId == u.Id)?.RoleId;
            var roleName = roles.FirstOrDefault(r => r.Id == roleId)?.Name ?? "Sans rôle";

            ws.Cell(row, 1).Value = u.Id.ToString();
            ws.Cell(row, 2).Value = u.FullName;
            ws.Cell(row, 3).Value = u.Email;
            ws.Cell(row, 4).Value = roleName;
            ws.Cell(row, 5).Value = u.IsActive ? "Actif" : "Inactif";
            ws.Cell(row, 6).Value = u.CreatedAt.ToString("dd/MM/yyyy");
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Utilisateurs_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("agreements")]
    [Authorize(Roles = "Admin,MinistereRH")]
    public async Task<IActionResult> ExportAgreements()
    {
        var agreements = await _ctx.InternshipAgreements
            .Include(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Conventions");

        // Headers
        ws.Cell(1, 1).Value = "ID Convention";
        ws.Cell(1, 2).Value = "N° Étudiant";
        ws.Cell(1, 3).Value = "Nom Étudiant";
        ws.Cell(1, 4).Value = "École";
        ws.Cell(1, 5).Value = "Date Début";
        ws.Cell(1, 6).Value = "Date Fin";
        ws.Cell(1, 7).Value = "Statut";
        ws.Cell(1, 8).Value = "Tuteur";
        
        FormatHeader(ws, 8);

        // Data
        int row = 2;
        foreach (var a in agreements)
        {
            ws.Cell(row, 1).Value = a.Id.ToString();
            ws.Cell(row, 2).Value = a.NumeroEtudiant ?? "-";
            ws.Cell(row, 3).Value = a.Application?.Student?.User?.FullName ?? "-";
            ws.Cell(row, 4).Value = a.Application?.Student?.Etablissement ?? "-";
            ws.Cell(row, 5).Value = a.DateDebut.ToString("dd/MM/yyyy");
            ws.Cell(row, 6).Value = a.DateFin.ToString("dd/MM/yyyy");
            ws.Cell(row, 7).Value = a.Statut.ToString();
            ws.Cell(row, 8).Value = a.NomTuteur ?? "-";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Conventions_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("internships")]
    [Authorize(Roles = "Admin,MinistereRH")]
    public async Task<IActionResult> ExportInternships()
    {
        var internships = await _ctx.Internships
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Include(i => i.Supervisor)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Stages");

        // Headers
        ws.Cell(1, 1).Value = "ID Stage";
        ws.Cell(1, 2).Value = "Sujet";
        ws.Cell(1, 3).Value = "Étudiant";
        ws.Cell(1, 4).Value = "Encadrant (Ministère)";
        ws.Cell(1, 5).Value = "Début Effectif";
        ws.Cell(1, 6).Value = "Fin Effective";
        ws.Cell(1, 7).Value = "Statut";
        
        FormatHeader(ws, 7);

        // Data
        int row = 2;
        foreach (var i in internships)
        {
            ws.Cell(row, 1).Value = i.Id.ToString();
            ws.Cell(row, 2).Value = i.Sujet;
            ws.Cell(row, 3).Value = i.Agreement?.Application?.Student?.User?.FullName ?? "-";
            ws.Cell(row, 4).Value = i.Supervisor?.NomComplet ?? "-";
            ws.Cell(row, 5).Value = i.DateDebutEffective.ToString("dd/MM/yyyy");
            ws.Cell(row, 6).Value = i.DateFinEffective?.ToString("dd/MM/yyyy") ?? "-";
            ws.Cell(row, 7).Value = i.Statut.ToString();
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Stages_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
