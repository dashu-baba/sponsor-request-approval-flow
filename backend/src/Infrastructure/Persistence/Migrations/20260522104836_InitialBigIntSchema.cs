using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SponsorshipApproval.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialBigIntSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                id = table.Column<string>(type: "text", nullable: false),
                name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                concurrency_stamp = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_roles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                id = table.Column<string>(type: "text", nullable: false),
                display_name = table.Column<string>(type: "text", nullable: false),
                department = table.Column<string>(type: "text", nullable: true),
                user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                password_hash = table.Column<string>(type: "text", nullable: true),
                security_stamp = table.Column<string>(type: "text", nullable: true),
                concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                phone_number = table.Column<string>(type: "text", nullable: true),
                phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                access_failed_count = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "sponsorship_types",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
            name: "AspNetRoleClaims",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                role_id = table.Column<string>(type: "text", nullable: false),
                claim_type = table.Column<string>(type: "text", nullable: true),
                claim_value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                table.ForeignKey(
                    name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "AspNetRoles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                user_id = table.Column<string>(type: "text", nullable: false),
                claim_type = table.Column<string>(type: "text", nullable: true),
                claim_value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                table.ForeignKey(
                    name: "fk_asp_net_user_claims_asp_net_users_user_id",
                    column: x => x.user_id,
                    principalTable: "AspNetUsers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                login_provider = table.Column<string>(type: "text", nullable: false),
                provider_key = table.Column<string>(type: "text", nullable: false),
                provider_display_name = table.Column<string>(type: "text", nullable: true),
                user_id = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                table.ForeignKey(
                    name: "fk_asp_net_user_logins_asp_net_users_user_id",
                    column: x => x.user_id,
                    principalTable: "AspNetUsers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                user_id = table.Column<string>(type: "text", nullable: false),
                role_id = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                table.ForeignKey(
                    name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "AspNetRoles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_asp_net_user_roles_asp_net_users_user_id",
                    column: x => x.user_id,
                    principalTable: "AspNetUsers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                user_id = table.Column<string>(type: "text", nullable: false),
                login_provider = table.Column<string>(type: "text", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                table.ForeignKey(
                    name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                    column: x => x.user_id,
                    principalTable: "AspNetUsers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                replaced_by_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refresh_tokens", x => x.id);
                table.ForeignKey(
                    name: "fk_refresh_tokens_users_user_id",
                    column: x => x.user_id,
                    principalTable: "AspNetUsers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "sponsorship_requests",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                requestor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                requestor_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                department = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                sponsorship_type_id = table.Column<long>(type: "bigint", nullable: false),
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
                table.CheckConstraint("ck_sponsorship_requests_requested_amount_non_negative", "requested_amount >= 0");
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
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                sponsorship_request_id = table.Column<long>(type: "bigint", nullable: false),
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
                table.CheckConstraint("ck_attachments_size_bytes_non_negative", "size_bytes >= 0");
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
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                sponsorship_request_id = table.Column<long>(type: "bigint", nullable: false),
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
            name: "ix_asp_net_role_claims_role_id",
            table: "AspNetRoleClaims",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles",
            column: "normalized_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_asp_net_user_claims_user_id",
            table: "AspNetUserClaims",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_asp_net_user_logins_user_id",
            table: "AspNetUserLogins",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_asp_net_user_roles_role_id",
            table: "AspNetUserRoles",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "normalized_email");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "normalized_user_name",
            unique: true);

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
            name: "ix_refresh_tokens_token_hash",
            table: "refresh_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_user_id_revoked_at",
            table: "refresh_tokens",
            columns: new[] { "user_id", "revoked_at" });

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
            unique: true,
            filter: "is_active = true");

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
            name: "AspNetRoleClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserLogins");

        migrationBuilder.DropTable(
            name: "AspNetUserRoles");

        migrationBuilder.DropTable(
            name: "AspNetUserTokens");

        migrationBuilder.DropTable(
            name: "attachments");

        migrationBuilder.DropTable(
            name: "refresh_tokens");

        migrationBuilder.DropTable(
            name: "workflow_history");

        migrationBuilder.DropTable(
            name: "AspNetRoles");

        migrationBuilder.DropTable(
            name: "AspNetUsers");

        migrationBuilder.DropTable(
            name: "sponsorship_requests");

        migrationBuilder.DropTable(
            name: "sponsorship_types");
    }
}
