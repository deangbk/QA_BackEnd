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

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventLog_View",
                table: "EventLog_View");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventLog_Login",
                table: "EventLog_Login");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "EventLog_View",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "EventLog_View",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "EventLog_Login",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "EventLog_Login",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventLog_View",
                table: "EventLog_View",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventLog_Login",
                table: "EventLog_Login",
                column: "Id");

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

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventLog_View",
                table: "EventLog_View");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventLog_Login",
                table: "EventLog_Login");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "EventLog_View");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "EventLog_Login");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "EventLog_View",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "EventLog_Login",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventLog_View",
                table: "EventLog_View",
                columns: new[] { "Timestamp", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventLog_Login",
                table: "EventLog_Login",
                columns: new[] { "Timestamp", "UserId" });

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
