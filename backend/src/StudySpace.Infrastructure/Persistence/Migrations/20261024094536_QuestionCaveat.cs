using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudySpace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuestionCaveat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationCaveat",
                table: "Questions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationCaveat",
                table: "Questions");
        }
    }
}
