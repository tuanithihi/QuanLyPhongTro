using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyPhongTro.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectReasonAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "tblRoom",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "tblLandlord",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblRoom_CreatedAt",
                table: "tblRoom",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tblRoom_IsFeatured",
                table: "tblRoom",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_tblRoom_IsPublished_ApprovalStatus_Status",
                table: "tblRoom",
                columns: new[] { "IsPublished", "ApprovalStatus", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tblLandlord_Status",
                table: "tblLandlord",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tblRoom_CreatedAt",
                table: "tblRoom");

            migrationBuilder.DropIndex(
                name: "IX_tblRoom_IsFeatured",
                table: "tblRoom");

            migrationBuilder.DropIndex(
                name: "IX_tblRoom_IsPublished_ApprovalStatus_Status",
                table: "tblRoom");

            migrationBuilder.DropIndex(
                name: "IX_tblLandlord_Status",
                table: "tblLandlord");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "tblRoom");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "tblLandlord");
        }
    }
}
