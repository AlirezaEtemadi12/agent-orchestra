using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchestra.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Events : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RunEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    TaskId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Seq = table.Column<long>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DataJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunEvent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunEvent_Id",
                table: "RunEvent",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunEvent_Kind_At",
                table: "RunEvent",
                columns: new[] { "Kind", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_RunEvent_ProjectId_TaskId_At",
                table: "RunEvent",
                columns: new[] { "ProjectId", "TaskId", "At" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunEvent");
        }
    }
}
