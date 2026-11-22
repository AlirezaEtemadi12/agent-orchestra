using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchestra.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FilesRead = table.Column<int>(type: "INTEGER", nullable: false),
                    BlocksRead = table.Column<int>(type: "INTEGER", nullable: false),
                    RunsBuilt = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    RunsPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportError",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportLogId = table.Column<Guid>(type: "TEXT", nullable: false),
                    File = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportError", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportError_ImportLog_ImportLogId",
                        column: x => x.ImportLogId,
                        principalTable: "ImportLog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Run",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RunKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TaskId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Repo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Task = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PlanVersion = table.Column<int>(type: "INTEGER", nullable: true),
                    PlanNumber = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    PlanTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Rounds = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CostMicros = table.Column<long>(type: "INTEGER", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SourceRef = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Run", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Run_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionUsage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    FirstRole = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Calls = table.Column<int>(type: "INTEGER", nullable: true),
                    Requests = table.Column<int>(type: "INTEGER", nullable: true),
                    Input = table.Column<long>(type: "INTEGER", nullable: true),
                    Output = table.Column<long>(type: "INTEGER", nullable: true),
                    Reasoning = table.Column<long>(type: "INTEGER", nullable: true),
                    CacheRead = table.Column<long>(type: "INTEGER", nullable: true),
                    CostMicros = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionUsage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionUsage_Run_RunId",
                        column: x => x.RunId,
                        principalTable: "Run",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StageResult",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Round = table.Column<int>(type: "INTEGER", nullable: true),
                    Phase = table.Column<int>(type: "INTEGER", nullable: true),
                    Adapter = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Verdict = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Blockers = table.Column<int>(type: "INTEGER", nullable: true),
                    Should = table.Column<int>(type: "INTEGER", nullable: true),
                    Nits = table.Column<int>(type: "INTEGER", nullable: true),
                    Changed = table.Column<int>(type: "INTEGER", nullable: true),
                    OutsidePlan = table.Column<int>(type: "INTEGER", nullable: true),
                    Violations = table.Column<int>(type: "INTEGER", nullable: true),
                    GatesGreen = table.Column<bool>(type: "INTEGER", nullable: true),
                    GatesNote = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ModelVerdict = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    IsFinal = table.Column<bool>(type: "INTEGER", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageResult_Run_RunId",
                        column: x => x.RunId,
                        principalTable: "Run",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StageText",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Round = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 200000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageText", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageText_Run_RunId",
                        column: x => x.RunId,
                        principalTable: "Run",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StageUsage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Sessions = table.Column<int>(type: "INTEGER", nullable: true),
                    Calls = table.Column<int>(type: "INTEGER", nullable: true),
                    Requests = table.Column<int>(type: "INTEGER", nullable: true),
                    Input = table.Column<long>(type: "INTEGER", nullable: true),
                    Output = table.Column<long>(type: "INTEGER", nullable: true),
                    Reasoning = table.Column<long>(type: "INTEGER", nullable: true),
                    CacheRead = table.Column<long>(type: "INTEGER", nullable: true),
                    Seconds = table.Column<int>(type: "INTEGER", nullable: true),
                    CostMicros = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageUsage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageUsage_Run_RunId",
                        column: x => x.RunId,
                        principalTable: "Run",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportError_ImportLogId",
                table: "ImportError",
                column: "ImportLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportLog_ProjectId_StartedAt",
                table: "ImportLog",
                columns: new[] { "ProjectId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Run_ProjectId_RunKey",
                table: "Run",
                columns: new[] { "ProjectId", "RunKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Run_ProjectId_Status",
                table: "Run",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionUsage_RunId",
                table: "SessionUsage",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_StageResult_RunId_Stage",
                table: "StageResult",
                columns: new[] { "RunId", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_StageText_RunId",
                table: "StageText",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_StageUsage_RunId",
                table: "StageUsage",
                column: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportError");

            migrationBuilder.DropTable(
                name: "SessionUsage");

            migrationBuilder.DropTable(
                name: "StageResult");

            migrationBuilder.DropTable(
                name: "StageText");

            migrationBuilder.DropTable(
                name: "StageUsage");

            migrationBuilder.DropTable(
                name: "ImportLog");

            migrationBuilder.DropTable(
                name: "Run");

            migrationBuilder.DropTable(
                name: "Project");
        }
    }
}
