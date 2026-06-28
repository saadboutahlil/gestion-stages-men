IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AppSettings] (
    [Id] uniqueidentifier NOT NULL,
    [Cle] nvarchar(450) NOT NULL,
    [Valeur] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AppSettings] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [AuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NULL,
    [UserName] nvarchar(max) NULL,
    [Action] nvarchar(max) NOT NULL,
    [EntityType] nvarchar(max) NULL,
    [EntityId] nvarchar(max) NULL,
    [Details] nvarchar(max) NULL,
    [IpAddress] nvarchar(max) NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Directions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NULL,
    [Nom] nvarchar(max) NOT NULL,
    [Sigle] nvarchar(450) NOT NULL,
    [Adresse] nvarchar(max) NULL,
    [Telephone] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    CONSTRAINT [PK_Directions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Directions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id])
);

CREATE TABLE [Students] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [CNE] nvarchar(450) NOT NULL,
    [Filiere] nvarchar(max) NOT NULL,
    [Promotion] nvarchar(max) NOT NULL,
    [Etablissement] nvarchar(max) NULL,
    [CvFilePath] nvarchar(max) NULL,
    [LettreMotivationPath] nvarchar(max) NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Students_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id])
);

CREATE TABLE [InternshipOffers] (
    [Id] uniqueidentifier NOT NULL,
    [DirectionId] uniqueidentifier NOT NULL,
    [Titre] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Competences] nvarchar(max) NULL,
    [DateDebut] date NOT NULL,
    [DateFin] date NOT NULL,
    [GratificationMensuelle] decimal(10,2) NULL,
    [NombrePostes] int NOT NULL,
    [Lieu] nvarchar(max) NOT NULL,
    [Statut] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InternshipOffers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InternshipOffers_Directions_DirectionId] FOREIGN KEY ([DirectionId]) REFERENCES [Directions] ([Id])
);

CREATE TABLE [Supervisors] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NULL,
    [DirectionId] uniqueidentifier NOT NULL,
    [NomComplet] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Telephone] nvarchar(max) NULL,
    [Fonction] nvarchar(max) NULL,
    [Service] nvarchar(max) NULL,
    CONSTRAINT [PK_Supervisors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Supervisors_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Supervisors_Directions_DirectionId] FOREIGN KEY ([DirectionId]) REFERENCES [Directions] ([Id])
);

CREATE TABLE [InternshipApplications] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [OfferId] uniqueidentifier NOT NULL,
    [Statut] int NOT NULL,
    [DatePostulation] datetime2 NOT NULL,
    [CvPath] nvarchar(max) NULL,
    [LettreMotivationPath] nvarchar(max) NULL,
    [Message] nvarchar(max) NULL,
    [MotifRefus] nvarchar(max) NULL,
    CONSTRAINT [PK_InternshipApplications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InternshipApplications_InternshipOffers_OfferId] FOREIGN KEY ([OfferId]) REFERENCES [InternshipOffers] ([Id]),
    CONSTRAINT [FK_InternshipApplications_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id])
);

CREATE TABLE [Supervisions] (
    [Id] uniqueidentifier NOT NULL,
    [SupervisorId] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [AssigneAt] datetime2 NOT NULL,
    [FinAt] datetime2 NULL,
    CONSTRAINT [PK_Supervisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Supervisions_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]),
    CONSTRAINT [FK_Supervisions_Supervisors_SupervisorId] FOREIGN KEY ([SupervisorId]) REFERENCES [Supervisors] ([Id])
);

CREATE TABLE [InternshipAgreements] (
    [Id] uniqueidentifier NOT NULL,
    [ApplicationId] uniqueidentifier NOT NULL,
    [DateDebut] date NOT NULL,
    [DateFin] date NOT NULL,
    [GratificationMensuelle] decimal(10,2) NULL,
    [Missions] nvarchar(max) NULL,
    [Objectifs] nvarchar(max) NULL,
    [NumeroEtudiant] nvarchar(max) NULL,
    [AnneeEtude] nvarchar(max) NULL,
    [Parcours] nvarchar(max) NULL,
    [ObjectifsPedagogiques] nvarchar(max) NULL,
    [CadreApprentissage] nvarchar(max) NULL,
    [NombreVisites] int NULL,
    [LivrablesAttendus] nvarchar(max) NULL,
    [CriteresEvaluation] nvarchar(max) NULL,
    [MissionsConcretes] nvarchar(max) NULL,
    [NomTuteur] nvarchar(max) NULL,
    [FonctionTuteur] nvarchar(max) NULL,
    [EmailTuteur] nvarchar(max) NULL,
    [TelephoneTuteur] nvarchar(max) NULL,
    [HorairesTravail] nvarchar(max) NULL,
    [TeletravailPossible] bit NULL,
    [MoyensFournis] nvarchar(max) NULL,
    [GrilleEvaluation] nvarchar(max) NULL,
    [Statut] int NOT NULL,
    [SignatureEtudiantAt] datetime2 NULL,
    [SignatureRHAt] datetime2 NULL,
    [SignatureEcoleAt] datetime2 NULL,
    [PdfPath] nvarchar(max) NULL,
    CONSTRAINT [PK_InternshipAgreements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InternshipAgreements_InternshipApplications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [InternshipApplications] ([Id])
);

CREATE TABLE [Internships] (
    [Id] uniqueidentifier NOT NULL,
    [AgreementId] uniqueidentifier NOT NULL,
    [Sujet] nvarchar(max) NOT NULL,
    [DescriptionDetaillee] nvarchar(max) NULL,
    [DateDebutEffective] date NOT NULL,
    [DateFinEffective] date NULL,
    [Statut] int NOT NULL,
    [DemarreAt] datetime2 NULL,
    [TermineAt] datetime2 NULL,
    CONSTRAINT [PK_Internships] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Internships_InternshipAgreements_AgreementId] FOREIGN KEY ([AgreementId]) REFERENCES [InternshipAgreements] ([Id])
);

CREATE TABLE [InternshipEvaluations] (
    [Id] uniqueidentifier NOT NULL,
    [InternshipId] uniqueidentifier NOT NULL,
    [EvaluateurId] uniqueidentifier NOT NULL,
    [Type] int NOT NULL,
    [DateEvaluation] datetime2 NOT NULL,
    [NoteTechnique] int NULL,
    [NoteComportement] int NULL,
    [NoteAutonomie] int NULL,
    [NoteGlobale] int NULL,
    [PointsForts] nvarchar(max) NULL,
    [PointsAmeliorer] nvarchar(max) NULL,
    [Recommandations] nvarchar(max) NULL,
    CONSTRAINT [PK_InternshipEvaluations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InternshipEvaluations_Internships_InternshipId] FOREIGN KEY ([InternshipId]) REFERENCES [Internships] ([Id]),
    CONSTRAINT [FK_InternshipEvaluations_Supervisors_EvaluateurId] FOREIGN KEY ([EvaluateurId]) REFERENCES [Supervisors] ([Id])
);

CREATE TABLE [InternshipReports] (
    [Id] uniqueidentifier NOT NULL,
    [InternshipId] uniqueidentifier NOT NULL,
    [Type] int NOT NULL,
    [Titre] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [CheminFichier] nvarchar(max) NOT NULL,
    [NomFichier] nvarchar(max) NOT NULL,
    [TailleFichier] bigint NOT NULL,
    [Statut] int NOT NULL,
    [DateDepot] datetime2 NOT NULL,
    [DateRevue] datetime2 NULL,
    [CommentaireReviseur] nvarchar(max) NULL,
    [ReviseurId] uniqueidentifier NULL,
    CONSTRAINT [PK_InternshipReports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InternshipReports_Internships_InternshipId] FOREIGN KEY ([InternshipId]) REFERENCES [Internships] ([Id])
);

CREATE TABLE [InternshipTasks] (
    [Id] uniqueidentifier NOT NULL,
    [InternshipId] uniqueidentifier NOT NULL,
    [Titre] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [DatePrevue] datetime2 NULL,
    [DateCompletion] datetime2 NULL,
    [Statut] int NOT NULL,
    CONSTRAINT [PK_InternshipTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InternshipTasks_Internships_InternshipId] FOREIGN KEY ([InternshipId]) REFERENCES [Internships] ([Id])
);

CREATE UNIQUE INDEX [IX_AppSettings_Cle] ON [AppSettings] ([Cle]);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);

CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);

CREATE UNIQUE INDEX [IX_Directions_Sigle] ON [Directions] ([Sigle]);

CREATE INDEX [IX_Directions_UserId] ON [Directions] ([UserId]);

CREATE UNIQUE INDEX [IX_InternshipAgreements_ApplicationId] ON [InternshipAgreements] ([ApplicationId]);

CREATE INDEX [IX_InternshipApplications_OfferId] ON [InternshipApplications] ([OfferId]);

CREATE UNIQUE INDEX [IX_InternshipApplications_StudentId_OfferId] ON [InternshipApplications] ([StudentId], [OfferId]);

CREATE INDEX [IX_InternshipEvaluations_EvaluateurId] ON [InternshipEvaluations] ([EvaluateurId]);

CREATE INDEX [IX_InternshipEvaluations_InternshipId] ON [InternshipEvaluations] ([InternshipId]);

CREATE INDEX [IX_InternshipOffers_DirectionId] ON [InternshipOffers] ([DirectionId]);

CREATE INDEX [IX_InternshipReports_InternshipId] ON [InternshipReports] ([InternshipId]);

CREATE UNIQUE INDEX [IX_Internships_AgreementId] ON [Internships] ([AgreementId]);

CREATE INDEX [IX_InternshipTasks_InternshipId] ON [InternshipTasks] ([InternshipId]);

CREATE UNIQUE INDEX [IX_Students_CNE] ON [Students] ([CNE]);

CREATE UNIQUE INDEX [IX_Students_UserId] ON [Students] ([UserId]);

CREATE INDEX [IX_Supervisions_StudentId] ON [Supervisions] ([StudentId]);

CREATE INDEX [IX_Supervisions_SupervisorId] ON [Supervisions] ([SupervisorId]);

CREATE INDEX [IX_Supervisors_DirectionId] ON [Supervisors] ([DirectionId]);

CREATE INDEX [IX_Supervisors_UserId] ON [Supervisors] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260505162713_Init', N'10.0.7');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Internships] ADD [SupervisorId] uniqueidentifier NULL;

CREATE INDEX [IX_Internships_SupervisorId] ON [Internships] ([SupervisorId]);

ALTER TABLE [Internships] ADD CONSTRAINT [FK_Internships_Supervisors_SupervisorId] FOREIGN KEY ([SupervisorId]) REFERENCES [Supervisors] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260505173611_AddTasksEvaluationsRapports', N'10.0.7');

COMMIT;
GO

