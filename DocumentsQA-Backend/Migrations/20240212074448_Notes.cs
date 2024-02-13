using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class Notes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AspNetUsers_PostedById",
                table: "Comments");

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Num = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatePosted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sticky = table.Column<bool>(type: "bit", nullable: false),
                    PostedById = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_AspNetUsers_PostedById",
                        column: x => x.PostedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_PostedById",
                table: "Notes",
                column: "PostedById");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_ProjectId",
                table: "Notes",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_AspNetUsers_PostedById",
                table: "Comments",
                column: "PostedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AspNetUsers_PostedById",
                table: "Comments");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_AspNetUsers_PostedById",
                table: "Comments",
                column: "PostedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
