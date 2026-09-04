using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Lecture01.Sync.Odata.CatsService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Breed = table.Column<string>(type: "text", nullable: false),
                    AgeMonths = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false),
                    IsVaccinated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cats", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Cats",
                columns: new[] { "Id", "AgeMonths", "Breed", "IsVaccinated", "Name", "WeightKg" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111111"), 24, "Британская короткошёрстная", true, "Барсик", 5.2000000000000002 },
                    { new Guid("a2222222-2222-2222-2222-222222222222"), 36, "Сибирская", true, "Мурка", 4.7999999999999998 },
                    { new Guid("a3333333-3333-3333-3333-333333333333"), 12, "Дворовая", false, "Васька", 3.8999999999999999 },
                    { new Guid("a4444444-4444-4444-4444-444444444444"), 8, "Мейн-кун", true, "Рыжик", 3.1000000000000001 },
                    { new Guid("a5555555-5555-5555-5555-555555555555"), 18, "Шотландская вислоухая", false, "Кузя", 4.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cats");
        }
    }
}
