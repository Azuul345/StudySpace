using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudySpace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSimpleMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mnemonic",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SimpleExplanation",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SimplePrompt",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Mnemonic",
                table: "Lessons",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SimpleSummary",
                table: "Lessons",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mnemonic",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SimpleExplanation",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SimplePrompt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Mnemonic",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SimpleSummary",
                table: "Lessons");
        }
    }
}
