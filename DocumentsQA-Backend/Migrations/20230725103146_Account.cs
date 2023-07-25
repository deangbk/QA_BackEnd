using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class Account : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Tranches_TrancheId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "ProjectUserAccesses");

            migrationBuilder.DropIndex(
                name: "IX_Questions_TrancheId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "TrancheId",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "Projects",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BannerUrl",
                table: "Projects",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "Documents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrancheId = table.Column<int>(type: "int", nullable: false),
                    AccountNo = table.Column<int>(type: "int", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Tranches_TrancheId",
                        column: x => x.TrancheId,
                        principalTable: "Tranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectUserAccess",
                columns: table => new
                {
                    Id1 = table.Column<int>(type: "int", nullable: false),
                    Id2 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUserAccess", x => new { x.Id1, x.Id2 });
                    table.ForeignKey(
                        name: "FK_ProjectUserAccess_AspNetUsers_Id1",
                        column: x => x.Id1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectUserAccess_Projects_Id2",
                        column: x => x.Id2,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectUserManage",
                columns: table => new
                {
                    Id1 = table.Column<int>(type: "int", nullable: false),
                    Id2 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUserManage", x => new { x.Id1, x.Id2 });
                    table.ForeignKey(
                        name: "FK_ProjectUserManage_AspNetUsers_Id1",
                        column: x => x.Id1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectUserManage_Projects_Id2",
                        column: x => x.Id2,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrancheUserAccess",
                columns: table => new
                {
                    Id1 = table.Column<int>(type: "int", nullable: false),
                    Id2 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrancheUserAccess", x => new { x.Id1, x.Id2 });
                    table.ForeignKey(
                        name: "FK_TrancheUserAccess_AspNetUsers_Id2",
                        column: x => x.Id2,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrancheUserAccess_Tranches_Id1",
                        column: x => x.Id1,
                        principalTable: "Tranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AccountId",
                table: "Questions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TrancheId",
                table: "Accounts",
                column: "TrancheId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccess_Id2",
                table: "ProjectUserAccess",
                column: "Id2");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserManage_Id2",
                table: "ProjectUserManage",
                column: "Id2");

            migrationBuilder.CreateIndex(
                name: "IX_TrancheUserAccess_Id2",
                table: "TrancheUserAccess",
                column: "Id2");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Accounts_AccountId",
                table: "Questions",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Accounts_AccountId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "ProjectUserAccess");

            migrationBuilder.DropTable(
                name: "ProjectUserManage");

            migrationBuilder.DropTable(
                name: "TrancheUserAccess");

            migrationBuilder.DropIndex(
                name: "IX_Questions_AccountId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "TrancheId",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BannerUrl",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateTable(
                name: "ProjectUserAccesses",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserAccessesId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_ProjectUserAccesses_AspNetUsers_UserAccessesId",
                        column: x => x.UserAccessesId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectUserAccesses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TrancheId",
                table: "Questions",
                column: "TrancheId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccesses_ProjectId",
                table: "ProjectUserAccesses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccesses_UserAccessesId",
                table: "ProjectUserAccesses",
                column: "UserAccessesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Tranches_TrancheId",
                table: "Questions",
                column: "TrancheId",
                principalTable: "Tranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
