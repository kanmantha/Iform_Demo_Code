using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IForm.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEotModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EotRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EotNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Project = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Client = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FinancialYear = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RevisionNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Scenario = table.Column<int>(type: "INTEGER", nullable: true),
                    ChangeProposedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SpaDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DesignRevisionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstimatedTimeImpactDays = table.Column<int>(type: "INTEGER", nullable: true),
                    EstimatedCostImpact = table.Column<decimal>(type: "TEXT", nullable: true),
                    OriginalApprovedScope = table.Column<string>(type: "TEXT", nullable: true),
                    RevisedScope = table.Column<string>(type: "TEXT", nullable: true),
                    ScopeAddition = table.Column<decimal>(type: "TEXT", nullable: true),
                    ScopeReduction = table.Column<decimal>(type: "TEXT", nullable: true),
                    DelayDays = table.Column<int>(type: "INTEGER", nullable: true),
                    CostEscalation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SubmissionStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientApproval = table.Column<int>(type: "INTEGER", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HasApprovedDrawings = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasRevisedDrawings = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasClientInstructions = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasDelayAnalysis = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasScopeVariationStatement = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasProgressReport = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasConsultantCorrespondence = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSupportingDocuments = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EotRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EotRequests_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EotRequests_Category",
                table: "EotRequests",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_EotRequests_CreatedAt",
                table: "EotRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EotRequests_CreatedById",
                table: "EotRequests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_EotRequests_EotNumber",
                table: "EotRequests",
                column: "EotNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EotRequests_Project",
                table: "EotRequests",
                column: "Project");

            migrationBuilder.CreateIndex(
                name: "IX_EotRequests_SubmissionStatus",
                table: "EotRequests",
                column: "SubmissionStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EotRequests");
        }
    }
}
