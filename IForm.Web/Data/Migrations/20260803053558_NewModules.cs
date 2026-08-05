using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IForm.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationUnitId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    ManagerId = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationUnits_AspNetUsers_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrganizationUnits_OrganizationUnits_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IncidentNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    SiteId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReportedById = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedToId = table.Column<string>(type: "TEXT", nullable: true),
                    ImmediateActionTaken = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RootCause = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    InvestigationNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsWorkRelated = table.Column<bool>(type: "INTEGER", nullable: false),
                    PersonsInvolved = table.Column<int>(type: "INTEGER", nullable: true),
                    DaysLost = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_AspNetUsers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Incidents_AspNetUsers_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ActionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActionNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedToId = table.Column<string>(type: "TEXT", nullable: false),
                    IncidentId = table.Column<int>(type: "INTEGER", nullable: true),
                    TicketId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: false),
                    CompletionNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionItems_AspNetUsers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionItems_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionItems_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActionItems_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SiteId",
                table: "Tickets",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationUnitId",
                table: "AspNetUsers",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_ActionNumber",
                table: "ActionItems",
                column: "ActionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_AssignedToId",
                table: "ActionItems",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_CreatedById",
                table: "ActionItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_DueDate",
                table: "ActionItems",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_IncidentId",
                table: "ActionItems",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_Status",
                table: "ActionItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_TicketId",
                table: "ActionItems",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_AssignedToId",
                table: "Incidents",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IncidentNumber",
                table: "Incidents",
                column: "IncidentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_OccurredAt",
                table: "Incidents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ReportedById",
                table: "Incidents",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_SiteId",
                table: "Incidents",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Status",
                table: "Incidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_Code",
                table: "OrganizationUnits",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_ManagerId",
                table: "OrganizationUnits",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_ParentId",
                table: "OrganizationUnits",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_OrganizationUnits_OrganizationUnitId",
                table: "AspNetUsers",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Sites_SiteId",
                table: "Tickets",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_OrganizationUnits_OrganizationUnitId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Sites_SiteId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "ActionItems");

            migrationBuilder.DropTable(
                name: "OrganizationUnits");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SiteId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizationUnitId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "OrganizationUnitId",
                table: "AspNetUsers");
        }
    }
}
