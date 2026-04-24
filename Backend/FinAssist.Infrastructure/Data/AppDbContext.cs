using FinAssist.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UtilisateurPermission> UtilisateurPermissions => Set<UtilisateurPermission>();
    public DbSet<LogUtilisateur> LogsUtilisateurs => Set<LogUtilisateur>();
    public DbSet<Categorie> Categories => Set<Categorie>();
    public DbSet<Besoin> Besoins => Set<Besoin>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Historique> Historiques => Set<Historique>();
    public DbSet<WorkflowCircuit> WorkflowCircuits => Set<WorkflowCircuit>();
    public DbSet<EtapeCircuit> EtapesCircuit => Set<EtapeCircuit>();
    public DbSet<Validation> Validations => Set<Validation>();
    public DbSet<SignatureElectronique> Signatures => Set<SignatureElectronique>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UtilisateurNotification> UtilisateurNotifications => Set<UtilisateurNotification>();
    public DbSet<LogActivite> LogsActivites => Set<LogActivite>();
    public DbSet<SignatureUtilisateur> SignaturesUtilisateurs => Set<SignatureUtilisateur>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<QrSignatureSession> QrSignatureSessions => Set<QrSignatureSession>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Code).IsUnique();
            e.Property(r => r.Code).HasMaxLength(50).IsRequired();
            e.Property(r => r.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<Utilisateur>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.Nom).HasMaxLength(100).IsRequired();
            e.Property(u => u.Prenom).HasMaxLength(100).IsRequired();
            e.Property(u => u.MotDePasse).IsRequired();
            e.HasOne(u => u.Role)
             .WithMany(r => r.Utilisateurs)
             .HasForeignKey(u => u.RoleId);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Code).IsUnique();
            e.Property(p => p.Code).HasMaxLength(100).IsRequired();
            e.Property(p => p.Description).HasMaxLength(255);
        });

        // Table de jonction ROLE_PERMISSION
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.ToTable("ROLE_PERMISSION");
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            e.HasOne(rp => rp.Role)
             .WithMany(r => r.RolePermissions)
             .HasForeignKey(rp => rp.RoleId);
            e.HasOne(rp => rp.Permission)
             .WithMany(p => p.RolePermissions)
             .HasForeignKey(rp => rp.PermissionId);
        });

        // Table de jonction UTILISATEUR_PERMISSION
        modelBuilder.Entity<UtilisateurPermission>(e =>
        {
            e.ToTable("UTILISATEUR_PERMISSION");
            e.HasKey(up => new { up.UtilisateurId, up.PermissionId });
            e.HasOne(up => up.Utilisateur)
             .WithMany(u => u.UtilisateurPermissions)
             .HasForeignKey(up => up.UtilisateurId);
            e.HasOne(up => up.Permission)
             .WithMany(p => p.UtilisateurPermissions)
             .HasForeignKey(up => up.PermissionId);
        });

        // Logs utilisateur
        modelBuilder.Entity<LogUtilisateur>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Action).HasMaxLength(200).IsRequired();
            e.Property(l => l.Details).HasMaxLength(1000);
            e.HasOne(l => l.Utilisateur)
             .WithMany(u => u.Logs)
             .HasForeignKey(l => l.UtilisateurId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Catégorie
        modelBuilder.Entity<Categorie>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Nom).IsUnique();
            e.Property(c => c.Nom).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(255);
            e.HasOne(c => c.WorkflowCircuit)
             .WithMany()
             .HasForeignKey(c => c.WorkflowCircuitId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Besoin
        modelBuilder.Entity<Besoin>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Titre).HasMaxLength(200).IsRequired();
            e.Property(b => b.NiveauImportance).HasMaxLength(50).IsRequired();
            e.Property(b => b.Statut).HasMaxLength(50).IsRequired();
            e.HasOne(b => b.Utilisateur)
             .WithMany()
             .HasForeignKey(b => b.UtilisateurId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Categorie)
             .WithMany(c => c.Besoins)
             .HasForeignKey(b => b.CategorieId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Document
        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Nom).HasMaxLength(255).IsRequired();
            e.Property(d => d.Type).HasMaxLength(100).IsRequired();
            e.Property(d => d.Checksum).HasMaxLength(64).IsRequired();
            e.HasOne(d => d.Besoin)
             .WithMany(b => b.Documents)
             .HasForeignKey(d => d.BesoinId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Historique
        modelBuilder.Entity<Historique>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Action).HasMaxLength(100).IsRequired();
            e.Property(h => h.Description).HasMaxLength(500);
            e.HasOne(h => h.Besoin)
             .WithMany(b => b.Historiques)
             .HasForeignKey(h => h.BesoinId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // WorkflowCircuit
        modelBuilder.Entity<WorkflowCircuit>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Nom).IsUnique();
            e.Property(c => c.Nom).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(255);
            e.Property(c => c.NomCreateur).HasMaxLength(200);
        });

        // EtapeCircuit
        modelBuilder.Entity<EtapeCircuit>(e =>
        {
            e.HasKey(ec => ec.Id);
            e.Property(ec => ec.RoleRequis).HasMaxLength(50).IsRequired();
            e.HasOne<WorkflowCircuit>()
             .WithMany(c => c.Etapes)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Validation
        modelBuilder.Entity<Validation>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Decision).HasConversion<string>().HasMaxLength(50);
            e.Property(v => v.Motif).HasMaxLength(500);
            e.Property(v => v.Commentaire).HasMaxLength(1000);
            e.Property(v => v.StatutApres).HasMaxLength(50);
            e.HasOne(v => v.Validateur)
             .WithMany()
             .HasForeignKey(v => v.ValidateurId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Besoin)
             .WithMany()
             .HasForeignKey(v => v.BesoinId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Signature électronique — pas d'index unique sur DocumentId (plusieurs signataires possibles)
        modelBuilder.Entity<SignatureElectronique>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Valeur).IsRequired();
            e.Property(s => s.Empreinte).HasMaxLength(64).IsRequired();
            e.HasIndex(s => new { s.DocumentId, s.UtilisateurId }).IsUnique(); // un utilisateur signe une seule fois par document
            e.HasOne(s => s.Utilisateur)
             .WithMany()
             .HasForeignKey(s => s.UtilisateurId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Document)
             .WithMany()
             .HasForeignKey(s => s.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Message).HasMaxLength(500).IsRequired();
            e.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
        });

        // Table de jonction UTILISATEUR_NOTIFICATION
        modelBuilder.Entity<UtilisateurNotification>(e =>
        {
            e.ToTable("UTILISATEUR_NOTIFICATION");
            e.HasKey(un => new { un.UtilisateurId, un.NotificationId });
            e.HasOne(un => un.Utilisateur)
             .WithMany()
             .HasForeignKey(un => un.UtilisateurId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(un => un.Notification)
             .WithMany(n => n.UtilisateurNotifications)
             .HasForeignKey(un => un.NotificationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Log d'activité (audit trail)
        modelBuilder.Entity<LogActivite>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Action).HasMaxLength(200).IsRequired();
            e.Property(l => l.EntiteType).HasMaxLength(100).IsRequired();
            e.HasOne(l => l.Utilisateur)
             .WithMany()
             .HasForeignKey(l => l.UtilisateurId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Signature personnelle utilisateur
        modelBuilder.Entity<SignatureUtilisateur>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.UtilisateurId).IsUnique(); // une seule signature par utilisateur
            e.Property(s => s.Type).HasMaxLength(50).IsRequired();
            e.Property(s => s.Police).HasMaxLength(100);
            e.HasOne(s => s.Utilisateur)
             .WithMany()
             .HasForeignKey(s => s.UtilisateurId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Préférences utilisateur
        modelBuilder.Entity<UserPreferences>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.UtilisateurId).IsUnique();
            e.Property(p => p.Langue).HasMaxLength(10);
            e.Property(p => p.FormatDate).HasMaxLength(20);
            e.Property(p => p.PageAccueil).HasMaxLength(100);
            e.Property(p => p.TriDefaut).HasMaxLength(50);
            e.HasOne(p => p.Utilisateur)
             .WithMany()
             .HasForeignKey(p => p.UtilisateurId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Sessions QR Code
        modelBuilder.Entity<QrSignatureSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Token).IsUnique();
            e.Property(s => s.Token).HasMaxLength(100).IsRequired();
            e.HasOne(s => s.Utilisateur)
             .WithMany()
             .HasForeignKey(s => s.UtilisateurId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Push subscriptions
        modelBuilder.Entity<PushSubscription>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Endpoint).IsUnique();
            e.HasOne(s => s.Utilisateur)
             .WithMany()
             .HasForeignKey(s => s.UtilisateurId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
