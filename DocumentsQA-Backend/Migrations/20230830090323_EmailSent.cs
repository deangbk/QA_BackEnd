using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class EmailSent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyEmailSent",
                table: "Questions");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateAnswered",
                table: "Questions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEmailSentDate",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateAnswered",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "LastEmailSentDate",
                table: "Projects");

            migrationBuilder.AddColumn<bool>(
                name: "DailyEmailSent",
                table: "Questions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
