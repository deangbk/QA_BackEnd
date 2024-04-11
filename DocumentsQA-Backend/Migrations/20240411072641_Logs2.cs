using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class Logs2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventLog_Login_AspNetUsers_UserId",
                table: "EventLog_Login");

            migrationBuilder.DropForeignKey(
                name: "FK_EventLog_View_AspNetUsers_UserId",
                table: "EventLog_View");

            migrationBuilder.AddForeignKey(
                name: "FK_EventLog_Login_AspNetUsers_UserId",
                table: "EventLog_Login",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EventLog_View_AspNetUsers_UserId",
                table: "EventLog_View",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventLog_Login_AspNetUsers_UserId",
                table: "EventLog_Login");

            migrationBuilder.DropForeignKey(
                name: "FK_EventLog_View_AspNetUsers_UserId",
                table: "EventLog_View");

            migrationBuilder.AddForeignKey(
                name: "FK_EventLog_Login_AspNetUsers_UserId",
                table: "EventLog_Login",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EventLog_View_AspNetUsers_UserId",
                table: "EventLog_View",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
