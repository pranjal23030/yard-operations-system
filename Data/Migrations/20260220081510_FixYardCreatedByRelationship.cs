using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YardOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixYardCreatedByRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Yards_AspNetUsers_CreatedByUserId",
                table: "Yards");

            migrationBuilder.DropIndex(
                name: "IX_Yards_CreatedByUserId",
                table: "Yards");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Yards");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Yards",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Yards_CreatedBy",
                table: "Yards",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Yards_AspNetUsers_CreatedBy",
                table: "Yards",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Yards_AspNetUsers_CreatedBy",
                table: "Yards");

            migrationBuilder.DropIndex(
                name: "IX_Yards_CreatedBy",
                table: "Yards");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Yards",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Yards",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Yards_CreatedByUserId",
                table: "Yards",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Yards_AspNetUsers_CreatedByUserId",
                table: "Yards",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
