using SponsorshipApproval.Application.Audit;

namespace SponsorshipApproval.Application.Tests.TestDoubles;

internal sealed class NoOpAuditService : IAuditService
{
    public void Record(AuditRecord record)
    {
    }
}
