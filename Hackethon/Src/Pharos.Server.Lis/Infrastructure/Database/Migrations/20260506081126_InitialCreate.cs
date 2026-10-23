using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Lis.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Lis");

            migrationBuilder.CreateTable(
                name: "LisStatistics",
                schema: "Lis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    LisStatisticsCategory = table.Column<int>(type: "int", nullable: false),
                    LatestSampleId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SamplesCount = table.Column<int>(type: "int", nullable: false),
                    TestsCount = table.Column<int>(type: "int", nullable: true),
                    LatestTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LisStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueuedLisMessages",
                schema: "Lis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    DeliveryTimeout = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MessageMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageData = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueuedLisMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LisStatistics_ChannelId_LisStatisticsCategory",
                schema: "Lis",
                table: "LisStatistics",
                columns: new[] { "ChannelId", "LisStatisticsCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueuedLisMessages_Priority_Created",
                schema: "Lis",
                table: "QueuedLisMessages",
                columns: new[] { "Priority", "Created" },
                descending: new[] { true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LisStatistics",
                schema: "Lis");

            migrationBuilder.DropTable(
                name: "QueuedLisMessages",
                schema: "Lis");
        }
    }
}
