using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YardOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrailersModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trailers",
                columns: table => new
                {
                    TrailerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrailerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CarrierId = table.Column<int>(type: "int", nullable: false),
                    DriverUserId = table.Column<int>(type: "int", nullable: true),
                    CurrentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GoodsType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentLocationId = table.Column<int>(type: "int", nullable: true),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trailers", x => x.TrailerId);
                    table.ForeignKey(
                        name: "FK_Trailers_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Trailers_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalTable: "Carriers",
                        principalColumn: "CarrierId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trailers_Locations_CurrentLocationId",
                        column: x => x.CurrentLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Goods",
                columns: table => new
                {
                    GoodsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrailerId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    HandlingNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goods", x => x.GoodsId);
                    table.ForeignKey(
                        name: "FK_Goods_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Goods_Trailers_TrailerId",
                        column: x => x.TrailerId,
                        principalTable: "Trailers",
                        principalColumn: "TrailerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ingates",
                columns: table => new
                {
                    IngateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrailerId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingates", x => x.IngateId);
                    table.ForeignKey(
                        name: "FK_Ingates_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ingates_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ingates_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_Ingates_Trailers_TrailerId",
                        column: x => x.TrailerId,
                        principalTable: "Trailers",
                        principalColumn: "TrailerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Outgates",
                columns: table => new
                {
                    OutgateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrailerId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outgates", x => x.OutgateId);
                    table.ForeignKey(
                        name: "FK_Outgates_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Outgates_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outgates_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_Outgates_Trailers_TrailerId",
                        column: x => x.TrailerId,
                        principalTable: "Trailers",
                        principalColumn: "TrailerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrailerHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrailerId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrailerHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_TrailerHistories_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrailerHistories_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_TrailerHistories_Trailers_TrailerId",
                        column: x => x.TrailerId,
                        principalTable: "Trailers",
                        principalColumn: "TrailerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Goods_CreatedBy",
                table: "Goods",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Goods_TrailerId",
                table: "Goods",
                column: "TrailerId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingates_CreatedBy",
                table: "Ingates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Ingates_LocationId",
                table: "Ingates",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingates_PerformedByUserId",
                table: "Ingates",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingates_TrailerId_Timestamp",
                table: "Ingates",
                columns: new[] { "TrailerId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Outgates_CreatedBy",
                table: "Outgates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Outgates_LocationId",
                table: "Outgates",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Outgates_PerformedByUserId",
                table: "Outgates",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Outgates_TrailerId_Timestamp",
                table: "Outgates",
                columns: new[] { "TrailerId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TrailerHistories_CreatedBy",
                table: "TrailerHistories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrailerHistories_LocationId",
                table: "TrailerHistories",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrailerHistories_TrailerId_StartTime",
                table: "TrailerHistories",
                columns: new[] { "TrailerId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_CarrierId",
                table: "Trailers",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_CreatedBy",
                table: "Trailers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_CurrentLocationId",
                table: "Trailers",
                column: "CurrentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_TrailerCode",
                table: "Trailers",
                column: "TrailerCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Goods");

            migrationBuilder.DropTable(
                name: "Ingates");

            migrationBuilder.DropTable(
                name: "Outgates");

            migrationBuilder.DropTable(
                name: "TrailerHistories");

            migrationBuilder.DropTable(
                name: "Trailers");
        }
    }
}
