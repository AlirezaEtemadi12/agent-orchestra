using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchestra.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Library : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LibraryItems",
                table: "ImportLog",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LibraryItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RelPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Repo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DoneOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PlannedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    PreviewRelPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LinkedPlanNumber = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    RunKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Group = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryItem_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_ProjectId_Category",
                table: "LibraryItem",
                columns: new[] { "ProjectId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_ProjectId_Category_RelPath",
                table: "LibraryItem",
                columns: new[] { "ProjectId", "Category", "RelPath" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryItem");

            migrationBuilder.DropColumn(
                name: "LibraryItems",
                table: "ImportLog");
        }
    }
}
