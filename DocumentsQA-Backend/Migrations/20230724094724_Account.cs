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
                name: "FK_ProjectUserAccesses_AspNetUsers_UserAccessesId",
                table: "ProjectUserAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectUserAccesses_Projects_ProjectId",
                table: "ProjectUserAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Tranches_TrancheId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectUserAccesses_UserAccessesId",
                table: "ProjectUserAccesses");

            migrationBuilder.DropColumn(
                name: "UserAccessesId",
                table: "ProjectUserAccesses");

            migrationBuilder.RenameTable(
                name: "ProjectUserAccesses",
                newName: "UserProjectAccesses");

            migrationBuilder.RenameColumn(
                name: "TrancheId",
                table: "Questions",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Questions_TrancheId",
                table: "Questions",
                newName: "IX_Questions_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectUserAccesses_ProjectId",
                table: "UserProjectAccesses",
                newName: "IX_UserProjectAccesses_ProjectId");

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
                name: "UserProjectManages",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserProjectManages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProjectManages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTrancheAccesses",
                columns: table => new
                {
                    TrancheId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserTrancheAccesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTrancheAccesses_Tranches_TrancheId",
                        column: x => x.TrancheId,
                        principalTable: "Tranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProjectAccesses_UserId",
                table: "UserProjectAccesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TrancheId",
                table: "Accounts",
                column: "TrancheId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProjectManages_ProjectId",
                table: "UserProjectManages",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProjectManages_UserId",
                table: "UserProjectManages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrancheAccesses_TrancheId",
                table: "UserTrancheAccesses",
                column: "TrancheId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrancheAccesses_UserId",
                table: "UserTrancheAccesses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Accounts_AccountId",
                table: "Questions",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProjectAccesses_AspNetUsers_UserId",
                table: "UserProjectAccesses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProjectAccesses_Projects_ProjectId",
                table: "UserProjectAccesses",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Accounts_AccountId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProjectAccesses_AspNetUsers_UserId",
                table: "UserProjectAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProjectAccesses_Projects_ProjectId",
                table: "UserProjectAccesses");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "UserProjectManages");

            migrationBuilder.DropTable(
                name: "UserTrancheAccesses");

            migrationBuilder.DropIndex(
                name: "IX_UserProjectAccesses_UserId",
                table: "UserProjectAccesses");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "UserProjectAccesses",
                newName: "ProjectUserAccesses");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Questions",
                newName: "TrancheId");

            migrationBuilder.RenameIndex(
                name: "IX_Questions_AccountId",
                table: "Questions",
                newName: "IX_Questions_TrancheId");

            migrationBuilder.RenameIndex(
                name: "IX_UserProjectAccesses_ProjectId",
                table: "ProjectUserAccesses",
                newName: "IX_ProjectUserAccesses_ProjectId");

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

            migrationBuilder.AddColumn<int>(
                name: "UserAccessesId",
                table: "ProjectUserAccesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccesses_UserAccessesId",
                table: "ProjectUserAccesses",
                column: "UserAccessesId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUserAccesses_AspNetUsers_UserAccessesId",
                table: "ProjectUserAccesses",
                column: "UserAccessesId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUserAccesses_Projects_ProjectId",
                table: "ProjectUserAccesses",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
