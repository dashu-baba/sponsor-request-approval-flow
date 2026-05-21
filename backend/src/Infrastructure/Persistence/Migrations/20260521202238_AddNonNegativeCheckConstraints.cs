using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SponsorshipApproval.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddNonNegativeCheckConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "ck_sponsorship_requests_requested_amount_non_negative",
            table: "sponsorship_requests",
            sql: "requested_amount >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "ck_attachments_size_bytes_non_negative",
            table: "attachments",
            sql: "size_bytes >= 0");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_sponsorship_requests_requested_amount_non_negative",
            table: "sponsorship_requests");

        migrationBuilder.DropCheckConstraint(
            name: "ck_attachments_size_bytes_non_negative",
            table: "attachments");
    }
}
