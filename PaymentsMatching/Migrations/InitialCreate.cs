using Microsoft.EntityFrameworkCore.Migrations;

namespace PaymentsMatching.Migrations
{

    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false,
                        defaultValueSql: "GETUTCDATE()"),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    MatchedCount = table.Column<int>(type: "int", nullable: false),
                    OnlySystemCount = table.Column<int>(type: "int", nullable: false),
                    OnlyProviderCount = table.Column<int>(type: "int", nullable: false),
                    AmountMismatchCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_MatchSessions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "MatchResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SystemAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ProviderAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ResolutionSide = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.Id);
                    table.ForeignKey("FK_MatchResults_MatchSessions_SessionId",
                        x => x.SessionId, "MatchSessions", "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_MatchResults_SessionId_IsResolved",
                "MatchResults", new[] { "SessionId", "IsResolved" });

            migrationBuilder.CreateIndex("IX_MatchResults_Status",
                "MatchResults", "Status");

            migrationBuilder.CreateIndex("IX_MatchResults_SessionId_OrderId_Currency",
                "MatchResults", new[] { "SessionId", "OrderId", "Currency" }, unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MatchResults");
            migrationBuilder.DropTable(name: "MatchSessions");
        }
    }
}
