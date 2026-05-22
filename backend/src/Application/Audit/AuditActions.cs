namespace SponsorshipApproval.Application.Audit;

public static class AuditActions
{
    public const string RequestCreated = "request.created";
    public const string RequestUpdated = "request.updated";
    public const string AttachmentUploaded = "attachment.uploaded";
    public const string SponsorshipTypeCreated = "sponsorship_type.created";
    public const string SponsorshipTypeUpdated = "sponsorship_type.updated";
    public const string SponsorshipTypeDeactivated = "sponsorship_type.deactivated";
    public const string UserCreated = "user.created";
    public const string AuthLogin = "auth.login";
    public const string AuthLogout = "auth.logout";
    public const string AuthProfileUpdated = "auth.profile_updated";
    public const string AuthPasswordChanged = "auth.password_changed";
}
