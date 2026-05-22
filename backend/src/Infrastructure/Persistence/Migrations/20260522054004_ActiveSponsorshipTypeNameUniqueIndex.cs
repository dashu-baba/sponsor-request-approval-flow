using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SponsorshipApproval.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ActiveSponsorshipTypeNameUniqueIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_sponsorship_types_name",
            table: "sponsorship_types");

        migrationBuilder.CreateIndex(
            name: "ix_sponsorship_types_name",
            table: "sponsorship_types",
            column: "name",
            unique: true,
            filter: "is_active = true");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_sponsorship_types_name",
            table: "sponsorship_types");

        migrationBuilder.CreateIndex(
            name: "ix_sponsorship_types_name",
            table: "sponsorship_types",
            column: "name",
            unique: true);
    }
}
