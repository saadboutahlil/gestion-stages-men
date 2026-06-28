using GestionStagesMEN.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionStagesMEN.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets ──
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Direction> Directions => Set<Direction>();
    public DbSet<InternshipOffer> InternshipOffers => Set<InternshipOffer>();
    public DbSet<InternshipApplication> InternshipApplications => Set<InternshipApplication>();
    public DbSet<InternshipAgreement> InternshipAgreements => Set<InternshipAgreement>();
    public DbSet<Internship> Internships => Set<Internship>();
    public DbSet<InternshipTask> InternshipTasks => Set<InternshipTask>();
    public DbSet<InternshipReport> InternshipReports => Set<InternshipReport>();
    public DbSet<InternshipEvaluation> InternshipEvaluations => Set<InternshipEvaluation>();
    public DbSet<Supervisor> Supervisors => Set<Supervisor>();
    public DbSet<Supervision> Supervisions => Set<Supervision>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ═══════════════════════════════════════════════════════════
        // NOTE : SQL Server interdit les cascades multiples.
        // On met NoAction partout pour éviter les cycles.
        // Les suppressions sont gérées manuellement par l'API.
        // ═══════════════════════════════════════════════════════════

        // ── Student ──
        b.Entity<Student>(e =>
        {
            e.HasIndex(s => s.UserId).IsUnique();
            e.HasIndex(s => s.CNE).IsUnique();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Direction ──
        b.Entity<Direction>(e =>
        {
            e.HasIndex(d => d.Sigle).IsUnique();
            e.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── InternshipOffer ──
        b.Entity<InternshipOffer>(e =>
        {
            e.HasOne(o => o.Direction).WithMany(d => d.Offers).HasForeignKey(o => o.DirectionId).OnDelete(DeleteBehavior.NoAction);
            e.Property(o => o.GratificationMensuelle).HasColumnType("decimal(10,2)");
        });

        // ── InternshipApplication ──
        b.Entity<InternshipApplication>(e =>
        {
            e.HasOne(a => a.Student).WithMany(s => s.Applications).HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(a => a.Offer).WithMany(o => o.Applications).HasForeignKey(a => a.OfferId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(a => new { a.StudentId, a.OfferId }).IsUnique();
        });

        // ── InternshipAgreement ──
        b.Entity<InternshipAgreement>(e =>
        {
            e.HasIndex(a => a.ApplicationId).IsUnique();
            e.HasOne(a => a.Application).WithOne(a => a.Agreement).HasForeignKey<InternshipAgreement>(a => a.ApplicationId).OnDelete(DeleteBehavior.NoAction);
            e.Property(a => a.GratificationMensuelle).HasColumnType("decimal(10,2)");
        });

        // ── Internship ──
        b.Entity<Internship>(e =>
        {
            e.HasIndex(i => i.AgreementId).IsUnique();
            e.HasOne(i => i.Agreement).WithOne(a => a.Internship).HasForeignKey<Internship>(i => i.AgreementId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── InternshipTask ──
        b.Entity<InternshipTask>(e =>
        {
            e.HasOne(t => t.Internship).WithMany(i => i.Taches).HasForeignKey(t => t.InternshipId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── InternshipReport ──
        b.Entity<InternshipReport>(e =>
        {
            e.HasOne(r => r.Internship).WithMany(i => i.Rapports).HasForeignKey(r => r.InternshipId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── InternshipEvaluation ──
        b.Entity<InternshipEvaluation>(e =>
        {
            e.HasOne(ev => ev.Internship).WithMany(i => i.Evaluations).HasForeignKey(ev => ev.InternshipId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(ev => ev.Evaluateur).WithMany(s => s.Evaluations).HasForeignKey(ev => ev.EvaluateurId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Supervisor ──
        b.Entity<Supervisor>(e =>
        {
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(s => s.Direction).WithMany(d => d.Encadrants).HasForeignKey(s => s.DirectionId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Supervision ──
        b.Entity<Supervision>(e =>
        {
            e.HasOne(s => s.Supervisor).WithMany(sup => sup.Supervisions).HasForeignKey(s => s.SupervisorId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(s => s.Student).WithMany().HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── AppSetting ──
        b.Entity<AppSetting>(e =>
        {
            e.HasIndex(s => s.Cle).IsUnique();
        });

        // ── AuditLog ──
        b.Entity<AuditLog>(e =>
        {
            e.HasIndex(l => l.Timestamp);
            e.HasIndex(l => l.UserId);
        });

        // ── School ──
        b.Entity<School>(e =>
        {
            e.HasIndex(s => s.UserId).IsUnique();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
