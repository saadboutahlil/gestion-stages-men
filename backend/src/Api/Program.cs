using GestionStagesMEN.Core.Entities;
using GestionStagesMEN.Data;
using GestionStagesMEN.Data.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════
// 1. BASE DE DONNÉES
// ═══════════════════════════════════════════
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// ═══════════════════════════════════════════
// 2. IDENTITY (utilisateurs + rôles)
// ═══════════════════════════════════════════
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ═══════════════════════════════════════════
// 3. JWT AUTHENTICATION
// ═══════════════════════════════════════════
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5),
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"error\":\"Non autorisé — veuillez vous connecter.\"}");
        }
    };
});

// ═══════════════════════════════════════════
// 4. CORS (autoriser le frontend Angular)
// ═══════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ═══════════════════════════════════════════
// 5. SERVICES
// ═══════════════════════════════════════════
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<ExcelGeneratorService>();
builder.Services.AddScoped<GestionStagesMEN.Core.Interfaces.IEmailService, EmailService>();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ═══════════════════════════════════════════
// PIPELINE HTTP
// ═══════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ═══════════════════════════════════════════
// SEED — Initialisation de la base en dev
// ═══════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var ctx = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    // Désactivation de la suppression automatique pour conserver les données
    // await ctx.Database.EnsureDeletedAsync();
    
    // Appliquer les migrations automatiquement
    await ctx.Database.MigrateAsync();

    // Seed des données
    await DbInitializer.InitializeAsync(ctx, userManager, roleManager);

    // Temporary Excel check
    try {
        using var workbook = new ClosedXML.Excel.XLWorkbook(@"C:\Users\pc gz\Desktop\saadboutahlil\PROJETS\stage info 23-26.xlsx");
        var dateStrings = new HashSet<string>();
        foreach(var sheet in workbook.Worksheets) {
            var usedRange = sheet.RangeUsed();
            if (usedRange == null) continue;
            for (int r = 2; r <= usedRange.RowCount(); r++) {
                var row = sheet.Row(r);
                var dateVal = row.Cell(3).Value.ToString();
                if (!string.IsNullOrWhiteSpace(dateVal)) {
                    dateStrings.Add(dateVal.Trim());
                }
            }
        }
        Console.WriteLine("ALL DISTINCT DATE STRINGS IN EXCEL:");
        foreach(var ds in dateStrings) {
            Console.WriteLine($"DATE_RAW: '{ds}'");
        }
    } catch(Exception ex) { Console.WriteLine("EXCEL ERR: " + ex.Message); }
}

app.Run();
