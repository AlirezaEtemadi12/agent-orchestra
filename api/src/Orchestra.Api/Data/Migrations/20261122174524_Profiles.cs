using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchestra.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Profiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EffectiveConfigJson",
                table: "Run",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Profile",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CurrentVersion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileVersion_Profile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileVersion_ProfileId_Version",
                table: "ProfileVersion",
                columns: new[] { "ProfileId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileVersion");

            migrationBuilder.DropTable(
                name: "Profile");

            migrationBuilder.DropColumn(
                name: "EffectiveConfigJson",
                table: "Run");
        }
    }
}
