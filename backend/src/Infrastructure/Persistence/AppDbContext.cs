using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Domain.Audit;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Identity;

namespace SponsorshipApproval.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<SponsorshipRequest> SponsorshipRequests => Set<SponsorshipRequest>();

    public DbSet<SponsorshipType> SponsorshipTypes => Set<SponsorshipType>();

    public DbSet<WorkflowHistory> WorkflowHistoryEntries => Set<WorkflowHistory>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
