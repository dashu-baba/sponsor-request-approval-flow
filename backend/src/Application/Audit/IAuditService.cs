namespace SponsorshipApproval.Application.Audit;

public interface IAuditService
{
    void Record(AuditRecord record);
}
