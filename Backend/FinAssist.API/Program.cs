using System.Text;
using FinAssist.Application.Services;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FinAssist.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Base de données ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Injection de dépendances — Module Auth ───────────────────────────────────
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordService, Argon2PasswordService>();

// ── Injection de dépendances — Module Utilisateurs ───────────────────────────
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IRolesRepository, RolesRepository>();
builder.Services.AddScoped<IPermissionsRepository, PermissionsRepository>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// ── Injection de dépendances — Module Besoins ─────────────────────────────────
builder.Services.AddScoped<IBesoinsRepository, BesoinsRepository>();
builder.Services.AddScoped<IBesoinsService, BesoinsService>();

// ── Injection de dépendances — Module Workflow ────────────────────────────────
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();

// ── Injection de dépendances — Module Signature ───────────────────────────────
builder.Services.AddScoped<ISignatureRepository, SignatureRepository>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<ISignatureConfig, SignatureConfig>();
builder.Services.AddScoped<IPdfSignatureService, PdfSignatureService>();
builder.Services.AddScoped<ISignatureService, SignatureService>();

// ── Injection de dépendances — Module Notifications ───────────────────────────
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();

// ── Injection de dépendances — Module Reporting ───────────────────────────────
builder.Services.AddScoped<IReportingRepository, ReportingRepository>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

// ── Injection de dépendances — Module Signature Utilisateur ──────────────────
builder.Services.AddScoped<ISignatureUtilisateurRepository, SignatureUtilisateurRepository>();
builder.Services.AddScoped<ISignatureUtilisateurService, SignatureUtilisateurService>();
builder.Services.AddScoped<QrSignatureService>();
builder.Services.AddScoped<IQrSessionRepository, QrSessionRepository>();

// ── Injection de dépendances — Module Settings ────────────────────────────────
builder.Services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();

// ── Push Notifications ────────────────────────────────────────────────────────
builder.Services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
builder.Services.AddScoped<IWebPushService, WebPushService>();

// ── Validation Deadline Worker ────────────────────────────────────────────────
builder.Services.AddScoped<ValidationDeadlineService>();
builder.Services.AddHostedService<FinAssist.Application.Workers.ValidationDeadlineWorker>();

// ── Injection de dépendances — Module Logs & Audit ────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IUserAgentParser, UserAgentParserService>();
builder.Services.AddHttpClient<IGeoIpService, GeoIpService>();
builder.Services.AddScoped<ILogService, LogService>();

// ── Email ─────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// ── Authentification JWT ─────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── CORS ─────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",")
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FinAssistPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Controllers + Swagger (net8) ─────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new FinAssist.API.Converters.UtcDateTimeConverter());
        opts.JsonSerializerOptions.Converters.Add(new FinAssist.API.Converters.UtcNullableDateTimeConverter());
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FinAssist API",
        Version = "v1",
        Description = "API du système FINASSIST — Gestion des réclamations et besoins"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

var app = builder.Build();

// ── Pipeline HTTP ─────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinAssist API v1"));

// CORS doit être avant HttpsRedirection pour que les preflight OPTIONS reçoivent les headers CORS
app.UseCors("FinAssistPolicy");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// ── Audit Middleware (après auth pour avoir l'identité) ───────────────────────
app.UseMiddleware<FinAssist.API.Middleware.AuditMiddleware>();

app.MapControllers();

// ── Seed des données initiales (dev uniquement) ───────────────────────────────
if (app.Environment.IsDevelopment())
    await DataSeeder.SeedAsync(app.Services);

app.Run();
