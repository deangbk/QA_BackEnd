using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class UserProjectRevert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NormalizedEmail_ProjectId",
                table: "AspNetUsers",
                columns: new[] { "NormalizedEmail", "ProjectId" },
                unique: true);

			// Merge ProjectId column with Username
			migrationBuilder.Sql(
				@"
					update [AspNetUsers] 
					set
						UserName = dd.NewUsername,
						NormalizedUserName = UPPER(dd.NewUsername)
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
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NormalizedEmail_ProjectId",
                table: "AspNetUsers");

			// Remove ProjectId prefix in username
			migrationBuilder.Sql(
				@"
					update [AspNetUsers] 
					set
						UserName = dd.NewUsername,
						NormalizedUserName = UPPER(dd.NewUsername)
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
    }
}
