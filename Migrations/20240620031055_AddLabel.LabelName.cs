using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabelSync.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelLabelName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelName",
                table: "Labels",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabelName",
                table: "Labels");
        }
    }
}
