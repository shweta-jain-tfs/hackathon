using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.AuditTrail.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "AuditTrailRecord");

            migrationBuilder.CreateTable(
                name: "AuditTrailRecords",
                schema: "AuditTrailRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(55)", maxLength: 55, nullable: false),
                    EventData = table.Column<string>(type: "nvarchar(max)", maxLength: 64000, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EventDataString = table.Column<string>(type: "nvarchar(max)", maxLength: 64000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrailRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditTrailRecordNotes",
                schema: "AuditTrailRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditTrailRecordId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrailRecordNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditTrailRecordNotes_AuditTrailRecords_AuditTrailRecordId",
                        column: x => x.AuditTrailRecordId,
                        principalSchema: "AuditTrailRecord",
                        principalTable: "AuditTrailRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrailRecordNotes_AuditTrailRecordId",
                schema: "AuditTrailRecord",
                table: "AuditTrailRecordNotes",
                column: "AuditTrailRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrailRecords_Comment",
                schema: "AuditTrailRecord",
                table: "AuditTrailRecords",
                column: "Comment");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrailRecords_Created",
                schema: "AuditTrailRecord",
                table: "AuditTrailRecords",
                column: "Created",
                descending: Array.Empty<bool>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrailRecordNotes",
                schema: "AuditTrailRecord");

            migrationBuilder.DropTable(
                name: "AuditTrailRecords",
                schema: "AuditTrailRecord");
        }
    }
}
