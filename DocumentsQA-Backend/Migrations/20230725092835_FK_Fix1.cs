using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class FK_Fix1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProjectAccesses");

            migrationBuilder.DropTable(
                name: "UserProjectManages");

            migrationBuilder.DropTable(
                name: "UserTrancheAccesses");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectUserAccess");

            migrationBuilder.DropTable(
                name: "ProjectUserManage");

            migrationBuilder.DropTable(
                name: "TrancheUserAccess");

            migrationBuilder.CreateTable(
                name: "UserProjectAccesses",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserProjectAccesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProjectAccesses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_UserProjectAccesses_ProjectId",
                table: "UserProjectAccesses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProjectAccesses_UserId",
                table: "UserProjectAccesses",
                column: "UserId");

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
        }
    }
}
