using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IForm.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SiteQueryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Specification = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Dimensions = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Project = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QueryNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IpoNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Project = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    QuantityNos = table.Column<decimal>(type: "TEXT", nullable: false),
                    QuantitySqm = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SlabTargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SlabCompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SlabDelayDays = table.Column<int>(type: "INTEGER", nullable: true),
                    RaisedById = table.Column<string>(type: "TEXT", nullable: false),
                    ResolvedById = table.Column<string>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteQueries_AspNetUsers_RaisedById",
                        column: x => x.RaisedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteQueries_AspNetUsers_ResolvedById",
                        column: x => x.ResolvedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SiteQueries_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QueryComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteQueryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QueryComments_SiteQueries_SiteQueryId",
                        column: x => x.SiteQueryId,
                        principalTable: "SiteQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteQueryPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteQueryId = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Caption = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteQueryPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteQueryPhotos_SiteQueries_SiteQueryId",
                        column: x => x.SiteQueryId,
                        principalTable: "SiteQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCode",
                table: "Products",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueryComments_SiteQueryId",
                table: "QueryComments",
                column: "SiteQueryId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryComments_UserId",
                table: "QueryComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteQueries_CreatedAt",
                table: "SiteQueries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SiteQueries_IpoNumber",
                table: "SiteQueries",
                column: "IpoNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SiteQueries_ProductId",
                table: "SiteQueries",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteQueries_Project",
                table: "SiteQueries",
                column: "Project");

            migrationBuilder.CreateIndex(
                name: "IX_SiteQueries_QueryNumber",
                table: "SiteQueries",
                column: "QueryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteQueries_RaisedById",
                table: "SiteQueries",
                column: "RaisedById");

            migrationBuilder.CreateIndex(
                name: "IX_SiteQueries_ResolvedById",
                table: "SiteQueries",
                column: "ResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_SiteQueries_Status",
                table: "SiteQueries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SiteQueryPhotos_SiteQueryId",
                table: "SiteQueryPhotos",
                column: "SiteQueryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueryComments");

            migrationBuilder.DropTable(
                name: "SiteQueryPhotos");

            migrationBuilder.DropTable(
                name: "SiteQueries");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
