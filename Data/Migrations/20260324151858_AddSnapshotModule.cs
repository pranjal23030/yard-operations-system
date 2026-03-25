using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YardOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SnapshotRuns",
                columns: table => new
                {
                    SnapshotRunId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CapturedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TotalInYard = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotRuns", x => x.SnapshotRunId);
                    table.ForeignKey(
                        name: "FK_SnapshotRuns_AspNetUsers_CapturedBy",
                        column: x => x.CapturedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotItems",
                columns: table => new
                {
                    SnapshotItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SnapshotRunId = table.Column<int>(type: "int", nullable: false),
                    TrailerId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrailerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CarrierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GoodsType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotItems", x => x.SnapshotItemId);
                    table.ForeignKey(
                        name: "FK_SnapshotItems_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SnapshotItems_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_SnapshotItems_SnapshotRuns_SnapshotRunId",
                        column: x => x.SnapshotRunId,
                        principalTable: "SnapshotRuns",
                        principalColumn: "SnapshotRunId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SnapshotItems_Trailers_TrailerId",
                        column: x => x.TrailerId,
                        principalTable: "Trailers",
                        principalColumn: "TrailerId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotItems_CapturedAt",
                table: "SnapshotItems",
                column: "CapturedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotItems_CreatedBy",
                table: "SnapshotItems",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotItems_LocationId",
                table: "SnapshotItems",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotItems_SnapshotRunId",
                table: "SnapshotItems",
                column: "SnapshotRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotItems_SnapshotRunId_TrailerId",
                table: "SnapshotItems",
                columns: new[] { "SnapshotRunId", "TrailerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotItems_Status",
                table: "SnapshotItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotItems_TrailerId",
                table: "SnapshotItems",
                column: "TrailerId");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotRuns_CapturedAt",
                table: "SnapshotRuns",
                column: "CapturedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotRuns_CapturedBy",
                table: "SnapshotRuns",
                column: "CapturedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SnapshotItems");

            migrationBuilder.DropTable(
                name: "SnapshotRuns");
        }
    }
}
