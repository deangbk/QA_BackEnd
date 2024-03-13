using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class DateSent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateSent",
                table: "Questions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

			// Initialize DateSent to DatePosted
			migrationBuilder.Sql(
				@"
					update [Questions]
					set DateSent = DatePosted
				");
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateSent",
                table: "Questions");
        }
    }
}
