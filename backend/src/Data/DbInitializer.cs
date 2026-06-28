using GestionStagesMEN.Core.Entities;
using GestionStagesMEN.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionStagesMEN.Data;

/// <summary>
/// Initialise la base avec les rôles, les comptes de test et un scénario complet.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext ctx, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        // ── CONTOURNEMENT MIGRATIONS ──
        // Créer la table Schools manuellement si elle n'existe pas
        await ctx.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Schools')
            BEGIN
                CREATE TABLE [Schools] (
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [UserId] uniqueidentifier NOT NULL,
                    [NomEtablissement] nvarchar(max) NOT NULL,
                    [Adresse] nvarchar(max) NULL,
                    [Telephone] nvarchar(max) NULL,
                    [EmailContact] nvarchar(max) NOT NULL,
                    CONSTRAINT [FK_Schools_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id])
                );
                CREATE UNIQUE INDEX [IX_Schools_UserId] ON [Schools] ([UserId]);
            END
        ");

        // VUES POWER BI
        await ctx.Database.ExecuteSqlRawAsync(@"
            CREATE OR ALTER VIEW vw_PowerBI_Stages AS
            SELECT 
                i.Id AS StageId,
                i.Sujet,
                CASE i.Statut
                    WHEN 10 THEN 'En Attente'
                    WHEN 20 THEN 'En Cours'
                    WHEN 30 THEN 'Suspendu'
                    WHEN 40 THEN 'Terminé'
                    WHEN 50 THEN 'Annulé'
                    ELSE 'Inconnu'
                END AS StatutStage,
                i.DateDebutEffective,
                i.DateFinEffective,
                YEAR(i.DateDebutEffective) AS AnneeStage,
                a.GratificationMensuelle,
                s.CNE,
                s.Filiere,
                s.Etablissement,
                u.FullName AS EtudiantNom,
                d.Nom AS DirectionNom,
                d.Sigle AS DirectionSigle,
                o.Titre AS OffreTitre
            FROM Internships i
            INNER JOIN InternshipAgreements a ON i.AgreementId = a.Id
            INNER JOIN InternshipApplications app ON a.ApplicationId = app.Id
            INNER JOIN Students s ON app.StudentId = s.Id
            INNER JOIN AspNetUsers u ON s.UserId = u.Id
            INNER JOIN InternshipOffers o ON app.OfferId = o.Id
            INNER JOIN Directions d ON o.DirectionId = d.Id;
        ");

        await ctx.Database.ExecuteSqlRawAsync(@"
            CREATE OR ALTER VIEW vw_PowerBI_Evaluations AS
            SELECT 
                e.Id AS EvalId,
                e.Type AS TypeEval,
                e.NoteTechnique,
                e.NoteComportement,
                e.NoteAutonomie,
                e.NoteGlobale,
                e.DateEvaluation,
                i.Id AS StageId,
                u.FullName AS EtudiantNom
            FROM InternshipEvaluations e
            INNER JOIN Internships i ON e.InternshipId = i.Id
            INNER JOIN InternshipAgreements a ON i.AgreementId = a.Id
            INNER JOIN InternshipApplications app ON a.ApplicationId = app.Id
            INNER JOIN Students s ON app.StudentId = s.Id
            INNER JOIN AspNetUsers u ON s.UserId = u.Id;
        ");

        // ═══════════════════════════════════════════
        // 1. RÔLES
        // ═══════════════════════════════════════════
        string[] roles = { "Admin", "Student", "MinistereRH", "Encadrant", "School" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
        }

        // ═══════════════════════════════════════════
        // 2. COMPTES UTILISATEURS (Garantie de présence)
        // ═══════════════════════════════════════════
        var adminUser = await EnsureUser(userManager, "admin@men.gov.ma", "Admin@2026!", "Administrateur Système", "Admin");
        var rhUser = await EnsureUser(userManager, "rh@men.gov.ma", "Rh@2026!", "Fatima Zahra Benali", "MinistereRH");
        var studentUser = await EnsureUser(userManager, "etudiant@ensias.ma", "Etudiant@2026!", "Ziad El Amrani", "Student");
        var encadrantUser = await EnsureUser(userManager, "encadrant@men.gov.ma", "Encadrant@2026!", "Mohamed Tazi", "Encadrant");
        var schoolUser = await EnsureUser(userManager, "ecole@ensias.ma", "Ecole@2026!", "ENSIAS - Responsable Stages", "School");
        var student2User = await EnsureUser(userManager, "sara@emi.ac.ma", "Etudiant@2026!", "Sara Alaoui", "Student");

        // Éviter de recréer les données métier (Directions, Offres...) si elles existent déjà
        if (await ctx.Directions.AnyAsync()) return;

        // ═══════════════════════════════════════════
        // 3. DIRECTION (DSI du Ministère)
        // ═══════════════════════════════════════════
        var dsi = new Direction
        {
            Nom = "Direction des Systèmes d'Information",
            Sigle = "DSI",
            Adresse = "Bab Rouah, Rabat",
            Telephone = "05 37 XX XX XX",
            Email = "dsi@men.gov.ma",
            UserId = rhUser.Id
        };
        ctx.Directions.Add(dsi);

        var drh = new Direction
        {
            Nom = "Direction des Ressources Humaines",
            Sigle = "DRH",
            Adresse = "Bab Rouah, Rabat",
            Email = "drh@men.gov.ma"
        };
        ctx.Directions.Add(drh);
        await ctx.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 4. ÉTUDIANTS
        // ═══════════════════════════════════════════
        var student1 = new Student
        {
            UserId = studentUser.Id,
            CNE = "G123456789",
            Filiere = "Génie Informatique",
            Promotion = "3ème année",
            Etablissement = "ENSIAS Rabat"
        };
        ctx.Students.Add(student1);

        var student2 = new Student
        {
            UserId = student2User.Id,
            CNE = "E987654321",
            Filiere = "Génie Logiciel",
            Promotion = "2ème année Master",
            Etablissement = "EMI Rabat"
        };
        ctx.Students.Add(student2);
        await ctx.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 5. ENCADRANT
        // ═══════════════════════════════════════════
        var encadrant = new Supervisor
        {
            UserId = encadrantUser.Id,
            DirectionId = dsi.Id,
            NomComplet = "Mohamed Tazi",
            Email = "encadrant@men.gov.ma",
            Telephone = "06 61 XX XX XX",
            Fonction = "Ingénieur Développement Senior",
            Service = "Service Développement"
        };
        ctx.Supervisors.Add(encadrant);
        await ctx.SaveChangesAsync();

        // Supervision : Mohamed encadre Ziad
        ctx.Supervisions.Add(new Supervision
        {
            SupervisorId = encadrant.Id,
            StudentId = student1.Id
        });
        await ctx.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 6. OFFRES DE STAGE
        // ═══════════════════════════════════════════
        var offre1 = new InternshipOffer
        {
            DirectionId = dsi.Id,
            Titre = "Stage PFE — Développement d'une application de gestion des stages",
            Description = "Développement d'une application web complète pour digitaliser le cycle de vie des stages au sein du Ministère. Technologies : ASP.NET Core, Angular, SQL Server.",
            Competences = "C#, Angular, SQL Server, Git",
            DateDebut = DateOnly.FromDateTime(DateTime.Now),
            DateFin = DateOnly.FromDateTime(DateTime.Now.AddMonths(4)),
            GratificationMensuelle = 2000,
            NombrePostes = 2,
            Lieu = "Rabat — Bab Rouah",
            Statut = OfferStatus.Ouverte
        };
        ctx.InternshipOffers.Add(offre1);

        var offre2 = new InternshipOffer
        {
            DirectionId = dsi.Id,
            Titre = "Stage — Administration réseau et sécurité",
            Description = "Renforcement de la sécurité réseau et mise en place de solutions de monitoring pour l'infrastructure IT du Ministère.",
            Competences = "Réseaux, Cybersécurité, Linux, Monitoring",
            DateDebut = DateOnly.FromDateTime(DateTime.Now.AddMonths(1)),
            DateFin = DateOnly.FromDateTime(DateTime.Now.AddMonths(3)),
            GratificationMensuelle = 1500,
            NombrePostes = 1,
            Lieu = "Rabat",
            Statut = OfferStatus.Ouverte
        };
        ctx.InternshipOffers.Add(offre2);

        var offre3 = new InternshipOffer
        {
            DirectionId = drh.Id,
            Titre = "Stage — Digitalisation des processus RH",
            Description = "Participation à la digitalisation des processus de gestion des ressources humaines du Ministère.",
            Competences = "Gestion de projet, Power BI, Excel avancé",
            DateDebut = DateOnly.FromDateTime(DateTime.Now.AddMonths(2)),
            DateFin = DateOnly.FromDateTime(DateTime.Now.AddMonths(5)),
            NombrePostes = 1,
            Lieu = "Rabat",
            Statut = OfferStatus.Ouverte
        };
        ctx.InternshipOffers.Add(offre3);
        await ctx.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 7. CANDIDATURE ACCEPTÉE + SCÉNARIO COMPLET
        // ═══════════════════════════════════════════

        // Candidature de Ziad → Offre 1 (acceptée)
        var candidature1 = new InternshipApplication
        {
            StudentId = student1.Id,
            OfferId = offre1.Id,
            Statut = ApplicationStatus.Acceptee,
            Message = "Très motivé pour ce stage ! J'ai de l'expérience en .NET et Angular."
        };
        ctx.InternshipApplications.Add(candidature1);

        // Candidature de Sara → Offre 2 (en attente)
        var candidature2 = new InternshipApplication
        {
            StudentId = student2.Id,
            OfferId = offre2.Id,
            Statut = ApplicationStatus.Soumise,
            Message = "Je souhaite approfondir mes compétences en cybersécurité."
        };
        ctx.InternshipApplications.Add(candidature2);

        // Candidature de Sara → Offre 1 (en revue)
        var candidature3 = new InternshipApplication
        {
            StudentId = student2.Id,
            OfferId = offre1.Id,
            Statut = ApplicationStatus.EnRevue,
            Message = "Stage PFE en développement web."
        };
        ctx.InternshipApplications.Add(candidature3);
        await ctx.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 8. CONVENTION SIGNÉE (scénario complet)
        // ═══════════════════════════════════════════
        var convention = new InternshipAgreement
        {
            ApplicationId = candidature1.Id,
            DateDebut = DateOnly.FromDateTime(DateTime.Now),
            DateFin = DateOnly.FromDateTime(DateTime.Now.AddMonths(4)),
            GratificationMensuelle = 2000,
            Missions = "Développement de l'application de gestion des stages",
            Objectifs = "Livrer une application fonctionnelle et documentée",

            // Partie École
            NumeroEtudiant = "G123456789",
            AnneeEtude = "3ème année Génie Informatique",
            Parcours = "Ingénierie des Systèmes d'Information",
            ObjectifsPedagogiques = "Maîtriser le développement Full-Stack, appliquer les bonnes pratiques",
            CadreApprentissage = "Stage PFE — 4 mois",
            NombreVisites = 2,
            LivrablesAttendus = "Rapport de mi-parcours, Rapport final, Soutenance",
            CriteresEvaluation = "Qualité du code, Respect des délais, Autonomie",

            // Partie RH
            MissionsConcretes = "Développement backend .NET, Frontend Angular, Base de données SQL Server",
            NomTuteur = "Mohamed Tazi",
            FonctionTuteur = "Ingénieur Développement Senior",
            EmailTuteur = "encadrant@men.gov.ma",
            HorairesTravail = "9h00 - 17h00",
            TeletravailPossible = true,
            MoyensFournis = "PC portable, accès VPN, badge d'accès",

            Statut = AgreementStatus.Signee,
            SignatureEtudiantAt = DateTime.UtcNow.AddDays(-10),
            SignatureRHAt = DateTime.UtcNow.AddDays(-8),
            SignatureEcoleAt = DateTime.UtcNow.AddDays(-5)
        };
        ctx.InternshipAgreements.Add(convention);
        await ctx.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 9. STAGE EN COURS
        // ═══════════════════════════════════════════
        var stage = new Internship
        {
            AgreementId = convention.Id,
            Sujet = "Développement de l'application de gestion des stages — MEN",
            DescriptionDetaillee = "Application web Full-Stack pour la gestion complète du cycle de vie des stages.",
            DateDebutEffective = DateOnly.FromDateTime(DateTime.Now.AddDays(-15)),
            Statut = InternshipStatus.EnCours,
            DemarreAt = DateTime.UtcNow.AddDays(-15)
        };
        ctx.Internships.Add(stage);
        await ctx.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 10. TÂCHES DU STAGE
        // ═══════════════════════════════════════════
        ctx.InternshipTasks.AddRange(new[]
        {
            new InternshipTask
            {
                InternshipId = stage.Id,
                Titre = "Analyse des besoins et cahier des charges",
                Description = "Rédiger le cahier des charges fonctionnel",
                Statut = TaskItemStatus.Terminee,
                DateCompletion = DateTime.UtcNow.AddDays(-10)
            },
            new InternshipTask
            {
                InternshipId = stage.Id,
                Titre = "Conception de la base de données",
                Description = "Modèle conceptuel, logique et physique",
                Statut = TaskItemStatus.Terminee,
                DateCompletion = DateTime.UtcNow.AddDays(-7)
            },
            new InternshipTask
            {
                InternshipId = stage.Id,
                Titre = "Développement du backend API",
                Description = "Créer les contrôleurs, services et entités",
                Statut = TaskItemStatus.EnCours,
                DatePrevue = DateTime.UtcNow.AddDays(5)
            },
            new InternshipTask
            {
                InternshipId = stage.Id,
                Titre = "Développement du frontend Angular",
                Description = "Pages, composants, design system",
                Statut = TaskItemStatus.AFaire,
                DatePrevue = DateTime.UtcNow.AddDays(15)
            },
            new InternshipTask
            {
                InternshipId = stage.Id,
                Titre = "Tests et documentation",
                Description = "Tests unitaires, documentation technique et utilisateur",
                Statut = TaskItemStatus.AFaire,
                DatePrevue = DateTime.UtcNow.AddDays(25)
            }
        });

        // ═══════════════════════════════════════════
        // 11. RAPPORT MI-PARCOURS (déposé)
        // ═══════════════════════════════════════════
        ctx.InternshipReports.Add(new InternshipReport
        {
            InternshipId = stage.Id,
            Type = ReportType.MiParcours,
            Titre = "Rapport de mi-parcours — Gestion des Stages",
            Description = "Rapport couvrant l'analyse et la conception",
            CheminFichier = "/uploads/rapports/rapport-mi-parcours-ziad.pdf",
            NomFichier = "rapport-mi-parcours-ziad.pdf",
            TailleFichier = 1024 * 512,
            Statut = ReportStatus.EnAttente
        });

        // ═══════════════════════════════════════════
        // 12. PARAMÈTRES PAR DÉFAUT
        // ═══════════════════════════════════════════
        ctx.AppSettings.AddRange(new[]
        {
            new AppSetting { Cle = "NomMinistere", Valeur = "Ministère de l'Éducation Nationale", Description = "Nom complet du ministère" },
            new AppSetting { Cle = "DureeMaxStageMois", Valeur = "6", Description = "Durée maximale d'un stage en mois" },
            new AppSetting { Cle = "GratificationMax", Valeur = "5000", Description = "Gratification mensuelle maximale en MAD" },
            new AppSetting { Cle = "EmailContact", Valeur = "stages@men.gov.ma", Description = "Email de contact pour les stages" }
        });

        await ctx.SaveChangesAsync();

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("✅ Base de données initialisée avec succès !");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Comptes de test :");
        Console.WriteLine("  Admin      → admin@men.gov.ma       / Admin@2026!");
        Console.WriteLine("  RH         → rh@men.gov.ma          / Rh@2026!");
        Console.WriteLine("  Étudiant   → etudiant@ensias.ma     / Etudiant@2026!");
        Console.WriteLine("  Encadrant  → encadrant@men.gov.ma   / Encadrant@2026!");
        Console.WriteLine("  École      → ecole@ensias.ma        / Ecole@2026!");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
    }

    private static async Task<ApplicationUser> EnsureUser(
        UserManager<ApplicationUser> userManager, string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception($"Échec création {email} : {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        else
        {
            // Forcer l'activation et le mot de passe s'il existe déjà
            user.IsActive = true;
            user.FullName = fullName;
            await userManager.UpdateAsync(user);

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, password);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
