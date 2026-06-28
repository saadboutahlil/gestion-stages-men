using GestionStagesMEN.Data;
using GestionStagesMEN.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseSchemaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ExcelGeneratorService _excelService;

    public DatabaseSchemaController(AppDbContext context, ExcelGeneratorService excelService)
    {
        _context = context;
        _excelService = excelService;
    }

    /// <summary>GET /api/admin/database-schema/excel — Téléchargement du fichier Excel</summary>
    [AllowAnonymous]
    [HttpGet("excel")]
    public IActionResult DownloadExcel()
    {
        try
        {
            var fileBytes = _excelService.GenerateTestDataExcel();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Structure_Base_Donnees_MEN.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Erreur lors de la génération Excel", message = ex.Message });
        }
    }

    /// <summary>GET /api/admin/database-schema/info — Métadonnées pour le diagramme Mermaid</summary>
    [HttpGet("info")]
    public IActionResult GetSchemaInfo()
    {
        try
        {
            // On construit manuellement le JSON pour être sûr d'éviter les cycles ou les types complexes
            var tables = _context.Model.GetEntityTypes().Select(t => new {
                name = t.GetTableName() ?? t.Name,
                columns = t.GetProperties().Select(p => {
                    string desc;
                    try {
                        desc = _excelService.GetFriendlyDescription(t.GetTableName() ?? t.Name, p.Name);
                    } catch {
                        desc = "Description indisponible";
                    }
                    return new {
                        name = p.Name,
                        type = p.ClrType.Name,
                        isPk = p.IsPrimaryKey(),
                        isFk = p.IsForeignKey(),
                        description = desc
                    };
                }).ToList()
            }).ToList();

            var rels = _context.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Select(fk => new {
                    from = fk.PrincipalEntityType.GetTableName() ?? fk.PrincipalEntityType.Name,
                    to = fk.DeclaringEntityType.GetTableName() ?? fk.DeclaringEntityType.Name,
                    name = fk.DependentToPrincipal?.Name ?? fk.PrincipalToDependent?.Name ?? "FK"
                }).ToList();

            return Ok(new { tables, relationships = rels });
        }
        catch (Exception ex)
        {
            return Ok(new { error = true, message = ex.Message });
        }
    }
}
