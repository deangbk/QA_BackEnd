using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class UserProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ProjectId",
                table: "AspNetUsers",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Projects_ProjectId",
                table: "AspNetUsers",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

			// Dear god.
			// Separate ProjectId prefix on username into new column ProjectId
			migrationBuilder.Sql(
				@"
					update [AspNetUsers] 
					set
						ProjectId = dd.ProjectId,
						UserName = dd.NewUsername,
						Email = dd.NewUsername,
						NormalizedUserName = UPPER(dd.NewUsername),
						NormalizedEmail = UPPER(dd.NewUsername)
					from (
						select
							u.Id as UserId,
							p.Id as ProjectId, 
							SUBSTRING(u.UserName, Pos + 1, 1000) as NewUsername
						from (
							select 
								Id, UserName,
								CHARINDEX(':', UserName) as Pos
							from [AspNetUsers]
						) as u
						left join [Projects] as p
						on u.Pos - 1 = p.Id
						where p.Id is not null
					) as dd
					inner join [AspNetUsers] as su
					on su.Id = dd.UserId
				");
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			// Merge ProjectId column back into username-prefix format
			migrationBuilder.Sql(
				@"
					update [AspNetUsers] 
					set
						UserName = dd.NewUsername,
						Email = dd.NewUsername,
						NormalizedUserName = UPPER(dd.NewUsername),
						NormalizedEmail = UPPER(dd.NewUsername)
					from (
						select 
							Id,
							(cast(ProjectId as varchar) + ':' + UserName) as NewUsername
						from [AspNetUsers]
						where ProjectId is not null
					) as dd
					inner join [AspNetUsers] as su
					on su.Id = dd.Id
				");

			migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Projects_ProjectId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ProjectId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "AspNetUsers");
        }
    }
}
