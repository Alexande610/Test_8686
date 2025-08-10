using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LittleFishBeauty.Migrations
{
    /// <inheritdoc />
    public partial class AddDanhGiaValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_DanhGia_SoSao",
                table: "DanhGia",
                sql: "SoSao BETWEEN 1 AND 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DanhGia_SoSao",
                table: "DanhGia");
        }
    }
}
