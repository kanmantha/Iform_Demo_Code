using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IForm.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchAndCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DispatchOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DispatchNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IpoNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Project = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SlabTargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SlabCompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SlabDelayDays = table.Column<int>(type: "INTEGER", nullable: true),
                    QuantityNos = table.Column<int>(type: "INTEGER", nullable: true),
                    QuantitySqm = table.Column<decimal>(type: "TEXT", nullable: true),
                    CastingDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DispatchStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    DispatchDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DelayDays = table.Column<int>(type: "INTEGER", nullable: true),
                    Reason = table.Column<int>(type: "INTEGER", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchOrders_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CertificateNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Supplier = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SupplierReportNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    WorkOrderNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Alloy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TestMethod = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReportDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SampleReceivedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UploadedById = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialCertificates_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrders_CreatedAt",
                table: "DispatchOrders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrders_CreatedById",
                table: "DispatchOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrders_DispatchNumber",
                table: "DispatchOrders",
                column: "DispatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrders_DispatchStatus",
                table: "DispatchOrders",
                column: "DispatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrders_IpoNumber",
                table: "DispatchOrders",
                column: "IpoNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrders_Project",
                table: "DispatchOrders",
                column: "Project");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCertificates_CertificateNumber",
                table: "MaterialCertificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCertificates_CreatedAt",
                table: "MaterialCertificates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCertificates_Supplier",
                table: "MaterialCertificates",
                column: "Supplier");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCertificates_UploadedById",
                table: "MaterialCertificates",
                column: "UploadedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatchOrders");

            migrationBuilder.DropTable(
                name: "MaterialCertificates");
        }
    }
}
