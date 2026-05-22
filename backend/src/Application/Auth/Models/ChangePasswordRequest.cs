namespace SponsorshipApproval.Application.Auth.Models;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
