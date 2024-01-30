using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AccountAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Projects_FavouriteProjectId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_FavouriteProjectId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FavouriteProjectId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Accounts",
                type: "int",
                nullable: true,		// Make nullable first so alter table succeeds
                defaultValue: 0);

			// Initialize value of Accounts.ProjectId
			migrationBuilder.Sql(
				@"
					UPDATE [Accounts]
					SET
						Accounts.ProjectId = t.ProjectId
					FROM [Accounts] as a
					INNER JOIN [Tranches] as t ON t.Id = a.TrancheId
				");

			// Make Accounts.ProjectId non-nullable
			migrationBuilder.AlterColumn<int>(
				name: "ProjectId",
				table: "Accounts",
				nullable: false);

			// Then we can initialize the FK relationship now

			migrationBuilder.CreateIndex(
				name: "IX_Accounts_ProjectId",
				table: "Accounts",
				column: "ProjectId");

			migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Projects_ProjectId",
                table: "Accounts",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Projects_ProjectId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_ProjectId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Accounts");

            migrationBuilder.AddColumn<int>(
                name: "FavouriteProjectId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FavouriteProjectId",
                table: "AspNetUsers",
                column: "FavouriteProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Projects_FavouriteProjectId",
                table: "AspNetUsers",
                column: "FavouriteProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
