using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SponsorshipApproval.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialDomainSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "sponsorship_types",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sponsorship_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "sponsorship_requests",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                requestor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                requestor_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                department = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                sponsorship_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                event_date = table.Column<DateOnly>(type: "date", nullable: false),
                requested_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                purpose = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                expected_benefit = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                remarks = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sponsorship_requests", x => x.id);
                table.ForeignKey(
                    name: "fk_sponsorship_requests_sponsorship_types_sponsorship_type_id",
                    column: x => x.sponsorship_type_id,
                    principalTable: "sponsorship_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "attachments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                sponsorship_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                object_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                content_type = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                size_bytes = table.Column<long>(type: "bigint", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_attachments", x => x.id);
                table.ForeignKey(
                    name: "fk_attachments_sponsorship_requests_sponsorship_request_id",
                    column: x => x.sponsorship_request_id,
                    principalTable: "sponsorship_requests",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "workflow_history",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                sponsorship_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                from_status = table.Column<int>(type: "integer", nullable: false),
                to_status = table.Column<int>(type: "integer", nullable: false),
                remarks = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_workflow_history", x => x.id);
                table.ForeignKey(
                    name: "fk_workflow_history_sponsorship_requests_sponsorship_request_id",
                    column: x => x.sponsorship_request_id,
                    principalTable: "sponsorship_requests",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_attachments_object_key",
            table: "attachments",
            column: "object_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_attachments_sponsorship_request_id",
            table: "attachments",
            column: "sponsorship_request_id");

        migrationBuilder.CreateIndex(
            name: "ix_sponsorship_requests_requestor_id",
            table: "sponsorship_requests",
            column: "requestor_id");

        migrationBuilder.CreateIndex(
            name: "ix_sponsorship_requests_sponsorship_type_id",
            table: "sponsorship_requests",
            column: "sponsorship_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_sponsorship_requests_status",
            table: "sponsorship_requests",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_sponsorship_requests_status_created_at",
            table: "sponsorship_requests",
            columns: new[] { "status", "created_at" });

        migrationBuilder.CreateIndex(
            name: "ix_sponsorship_types_is_active",
            table: "sponsorship_types",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_sponsorship_types_name",
            table: "sponsorship_types",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_workflow_history_actor_id",
            table: "workflow_history",
            column: "actor_id");

        migrationBuilder.CreateIndex(
            name: "ix_workflow_history_sponsorship_request_id",
            table: "workflow_history",
            column: "sponsorship_request_id");

        migrationBuilder.CreateIndex(
            name: "ix_workflow_history_sponsorship_request_id_occurred_at",
            table: "workflow_history",
            columns: new[] { "sponsorship_request_id", "occurred_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "attachments");

        migrationBuilder.DropTable(
            name: "workflow_history");

        migrationBuilder.DropTable(
            name: "sponsorship_requests");

        migrationBuilder.DropTable(
            name: "sponsorship_types");
    }
}
