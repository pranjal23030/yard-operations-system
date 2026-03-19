using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YardOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkTrailerDriverToIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DriverUserId",
                table: "Trailers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_DriverUserId",
                table: "Trailers",
                column: "DriverUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trailers_AspNetUsers_DriverUserId",
                table: "Trailers",
                column: "DriverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trailers_AspNetUsers_DriverUserId",
                table: "Trailers");

            migrationBuilder.DropIndex(
                name: "IX_Trailers_DriverUserId",
                table: "Trailers");

            migrationBuilder.AlterColumn<int>(
                name: "DriverUserId",
                table: "Trailers",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
