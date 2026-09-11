using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Lecture03.Replication.App.Migrations
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Breed = table.Column<string>(type: "text", nullable: false),
                    PetsCount = table.Column<int>(type: "integer", nullable: false),
                    LastPetAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cats", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Cats",
                columns: new[] { "Id", "Breed", "LastPetAt", "Name", "PetsCount" },
                values: new object[,]
                {
                    { 1, "Maine Coon", null, "Whiskers", 0 },
                    { 2, "British Shorthair", null, "Mittens", 0 },
                    { 3, "Bombay", null, "Shadow", 0 },
                    { 4, "Siamese", null, "Luna", 0 },
                    { 5, "Scottish Fold", null, "Oliver", 0 },
                    { 6, "Abyssinian", null, "Simba", 0 },
                    { 7, "Ragdoll", null, "Nala", 0 },
                    { 8, "Sphynx", null, "Milo", 0 },
                    { 9, "Bengal", null, "Leo", 0 },
                    { 10, "Persian", null, "Bella", 0 },
                    { 11, "Norwegian Forest", null, "Tom", 0 },
                    { 12, "Devon Rex", null, "Cleo", 0 },
                    { 13, "Russian Blue", null, "Jasper", 0 },
                    { 14, "Turkish Angora", null, "Chloe", 0 },
                    { 15, "American Shorthair", null, "Felix", 0 },
                    { 16, "Birman", null, "Sasha", 0 },
                    { 17, "Manx", null, "Oscar", 0 },
                    { 18, "Somali", null, "Ginger", 0 },
                    { 19, "Chartreux", null, "Smokey", 0 },
                    { 20, "Egyptian Mau", null, "Pepper", 0 }
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
