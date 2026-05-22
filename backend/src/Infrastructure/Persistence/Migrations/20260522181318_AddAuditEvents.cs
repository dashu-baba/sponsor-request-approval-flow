using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SponsorshipApproval.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAuditEvents : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "audit_events",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                actor_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                resource_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                metadata = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_audit_events", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_audit_events_action_occurred_at",
            table: "audit_events",
            columns: new[] { "action", "occurred_at" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_events_actor_id_occurred_at",
            table: "audit_events",
            columns: new[] { "actor_id", "occurred_at" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_events_occurred_at",
            table: "audit_events",
            column: "occurred_at");

        migrationBuilder.CreateIndex(
            name: "ix_audit_events_resource_type_resource_id_occurred_at",
            table: "audit_events",
            columns: new[] { "resource_type", "resource_id", "occurred_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "audit_events");
    }
}
