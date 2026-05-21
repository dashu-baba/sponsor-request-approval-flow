using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SponsorshipRequest> SponsorshipRequests => Set<SponsorshipRequest>();

    public DbSet<SponsorshipType> SponsorshipTypes => Set<SponsorshipType>();

    public DbSet<WorkflowHistory> WorkflowHistoryEntries => Set<WorkflowHistory>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
