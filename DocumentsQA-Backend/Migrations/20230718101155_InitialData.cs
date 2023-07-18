using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FavouriteProjectId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BannerUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProjectEndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionNum = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Tranche = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatePosted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuestionApproved = table.Column<bool>(type: "bit", nullable: false),
                    AnswerApproved = table.Column<bool>(type: "bit", nullable: false),
                    DateQuestionApproved = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateAnswerApproved = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AnsweredById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    QuestionApprovedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AnswerApprovedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastEditorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DateLastEdited = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DailyEmailSent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_AspNetUsers_AnswerApprovedById",
                        column: x => x.AnswerApprovedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_AspNetUsers_AnsweredById",
                        column: x => x.AnsweredById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_AspNetUsers_LastEditorId",
                        column: x => x.LastEditorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_AspNetUsers_PostedById",
                        column: x => x.PostedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_AspNetUsers_QuestionApprovedById",
                        column: x => x.QuestionApprovedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectUserAccesses",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAccessesId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Tranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tranches_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DateUploaded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AssocQuestionId = table.Column<int>(type: "int", nullable: true),
                    AssocUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Hidden = table.Column<bool>(type: "bit", nullable: false),
                    AllowPrint = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_AssocUserId",
                        column: x => x.AssocUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Questions_AssocQuestionId",
                        column: x => x.AssocQuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FavouriteProjectId",
                table: "AspNetUsers",
                column: "FavouriteProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_AssocQuestionId",
                table: "Documents",
                column: "AssocQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_AssocUserId",
                table: "Documents",
                column: "AssocUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedById",
                table: "Documents",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccesses_ProjectId",
                table: "ProjectUserAccesses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccesses_UserAccessesId",
                table: "ProjectUserAccesses",
                column: "UserAccessesId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AnswerApprovedById",
                table: "Questions",
                column: "AnswerApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AnsweredById",
                table: "Questions",
                column: "AnsweredById");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_LastEditorId",
                table: "Questions",
                column: "LastEditorId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_PostedById",
                table: "Questions",
                column: "PostedById");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionApprovedById",
                table: "Questions",
                column: "QuestionApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_Tranches_ProjectId",
                table: "Tranches",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Projects_FavouriteProjectId",
                table: "AspNetUsers",
                column: "FavouriteProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Projects_FavouriteProjectId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "ProjectUserAccesses");

            migrationBuilder.DropTable(
                name: "Tranches");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_FavouriteProjectId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FavouriteProjectId",
                table: "AspNetUsers");
        }
    }
}
