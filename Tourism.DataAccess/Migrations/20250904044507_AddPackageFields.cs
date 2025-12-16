using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tourism.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Packages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Packages");
        }
    }
}
