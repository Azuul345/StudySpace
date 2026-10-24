using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudySpace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LessonDiagrams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiagramsJson",
                table: "Lessons",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiagramsJson",
                table: "Lessons");
        }
    }
}
