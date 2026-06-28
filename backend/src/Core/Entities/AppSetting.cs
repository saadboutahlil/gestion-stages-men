namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Paramètre global configurable par l'admin.
/// </summary>
public class AppSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Cle { get; set; } = string.Empty;
    public string Valeur { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
