using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lughat.Engine.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDictionaryLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Dictionaries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "Dictionaries");
        }
    }
}
