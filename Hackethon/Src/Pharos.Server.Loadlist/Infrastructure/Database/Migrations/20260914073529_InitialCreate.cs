using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.LoadList.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "LoadList");

            migrationBuilder.CreateTable(
                name: "LastLoadedReagents",
                schema: "LoadList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReagentKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArticleNumber = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastLoadedReagents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoadListConfigurations",
                schema: "LoadList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentSerial = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Identity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ArticleNumber = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    FriendlyName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(55)", maxLength: 55, nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    ArticleType = table.Column<int>(type: "int", nullable: false),
                    Shape = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    RefillLevel = table.Column<int>(type: "int", nullable: false),
                    TriggerLevel = table.Column<int>(type: "int", nullable: false),
                    Text1 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Text2 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Text3 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Text4 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Color1 = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Color2 = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Color3 = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Color4 = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadListConfigurations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LastLoadedReagents",
                schema: "LoadList");

            migrationBuilder.DropTable(
                name: "LoadListConfigurations",
                schema: "LoadList");
        }
    }
}
