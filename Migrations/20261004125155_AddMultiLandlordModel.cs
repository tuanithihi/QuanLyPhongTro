using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyPhongTro.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiLandlordModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LandlordId",
                table: "tblTenant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LandlordId",
                table: "tblService",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "tblRoom",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "tblRoom",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "tblRoom",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "tblRoom",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "tblRoom",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "tblRoom",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "tblRoom",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "tblRoom",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContractId",
                table: "tblReview",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LandlordId",
                table: "tblInvoice",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LandlordId",
                table: "tblContract",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LandlordId",
                table: "tblChatSession",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LandlordId",
                table: "tblBookingRequest",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ViewingTime",
                table: "tblBookingRequest",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tblAmenity",
                columns: table => new
                {
                    AmenityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblAmenity", x => x.AmenityId);
                });

            migrationBuilder.CreateTable(
                name: "tblFavorite",
                columns: table => new
                {
                    FavoriteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblFavorite", x => x.FavoriteId);
                    table.ForeignKey(
                        name: "FK_tblFavorite_tblRoom_RoomId",
                        column: x => x.RoomId,
                        principalTable: "tblRoom",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblFavorite_tblTenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tblTenant",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tblFavorite_tblUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tblLandlord",
                columns: table => new
                {
                    LandlordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IdentityNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblLandlord", x => x.LandlordId);
                    table.ForeignKey(
                        name: "FK_tblLandlord_tblUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tblProvince",
                columns: table => new
                {
                    ProvinceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblProvince", x => x.ProvinceId);
                });

            migrationBuilder.CreateTable(
                name: "tblRoomImage",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblRoomImage", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_tblRoomImage_tblRoom_RoomId",
                        column: x => x.RoomId,
                        principalTable: "tblRoom",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblRoomAmenity",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    AmenityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblRoomAmenity", x => new { x.RoomId, x.AmenityId });
                    table.ForeignKey(
                        name: "FK_tblRoomAmenity_tblAmenity_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "tblAmenity",
                        principalColumn: "AmenityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblRoomAmenity_tblRoom_RoomId",
                        column: x => x.RoomId,
                        principalTable: "tblRoom",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblDistrict",
                columns: table => new
                {
                    DistrictId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProvinceId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblDistrict", x => x.DistrictId);
                    table.ForeignKey(
                        name: "FK_tblDistrict_tblProvince_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "tblProvince",
                        principalColumn: "ProvinceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblWard",
                columns: table => new
                {
                    WardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblWard", x => x.WardId);
                    table.ForeignKey(
                        name: "FK_tblWard_tblDistrict_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "tblDistrict",
                        principalColumn: "DistrictId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblProperty",
                columns: table => new
                {
                    PropertyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LandlordId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProvinceId = table.Column<int>(type: "int", nullable: true),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    WardId = table.Column<int>(type: "int", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblProperty", x => x.PropertyId);
                    table.ForeignKey(
                        name: "FK_tblProperty_tblDistrict_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "tblDistrict",
                        principalColumn: "DistrictId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblProperty_tblLandlord_LandlordId",
                        column: x => x.LandlordId,
                        principalTable: "tblLandlord",
                        principalColumn: "LandlordId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblProperty_tblProvince_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "tblProvince",
                        principalColumn: "ProvinceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblProperty_tblWard_WardId",
                        column: x => x.WardId,
                        principalTable: "tblWard",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblTenant_LandlordId",
                table: "tblTenant",
                column: "LandlordId");

            migrationBuilder.CreateIndex(
                name: "IX_tblService_LandlordId",
                table: "tblService",
                column: "LandlordId");

            migrationBuilder.CreateIndex(
                name: "IX_tblRoom_ApprovalStatus",
                table: "tblRoom",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_tblRoom_PropertyId",
                table: "tblRoom",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_tblRoom_RoomPrice",
                table: "tblRoom",
                column: "RoomPrice");

            migrationBuilder.Sql("UPDATE [tblRoom] SET [Slug] = CONCAT('phong-', [RoomId]) WHERE [Slug] = '' OR [Slug] IS NULL;");
            migrationBuilder.Sql("UPDATE [tblRoom] SET [Title] = [RoomName] WHERE [Title] IS NULL OR [Title] = '';");

            migrationBuilder.CreateIndex(
                name: "IX_tblRoom_Slug",
                table: "tblRoom",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblReview_ContractId",
                table: "tblReview",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_tblInvoice_LandlordId",
                table: "tblInvoice",
                column: "LandlordId");

            migrationBuilder.CreateIndex(
                name: "IX_tblContract_LandlordId",
                table: "tblContract",
                column: "LandlordId");

            migrationBuilder.CreateIndex(
                name: "IX_tblChatSession_LandlordId",
                table: "tblChatSession",
                column: "LandlordId");

            migrationBuilder.CreateIndex(
                name: "IX_tblBookingRequest_LandlordId",
                table: "tblBookingRequest",
                column: "LandlordId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAmenity_Name",
                table: "tblAmenity",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblDistrict_Code",
                table: "tblDistrict",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_tblDistrict_ProvinceId",
                table: "tblDistrict",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_tblFavorite_RoomId",
                table: "tblFavorite",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_tblFavorite_TenantId",
                table: "tblFavorite",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tblFavorite_UserId",
                table: "tblFavorite",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblLandlord_UserId",
                table: "tblLandlord",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblProperty_DistrictId",
                table: "tblProperty",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_tblProperty_LandlordId",
                table: "tblProperty",
                column: "LandlordId");

            migrationBuilder.CreateIndex(
                name: "IX_tblProperty_ProvinceId",
                table: "tblProperty",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_tblProperty_Slug",
                table: "tblProperty",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblProperty_WardId",
                table: "tblProperty",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_tblProvince_Code",
                table: "tblProvince",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblRoomAmenity_AmenityId",
                table: "tblRoomAmenity",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_tblRoomImage_RoomId",
                table: "tblRoomImage",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_tblWard_Code",
                table: "tblWard",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_tblWard_DistrictId",
                table: "tblWard",
                column: "DistrictId");

            migrationBuilder.AddForeignKey(
                name: "FK_tblBookingRequest_tblLandlord_LandlordId",
                table: "tblBookingRequest",
                column: "LandlordId",
                principalTable: "tblLandlord",
                principalColumn: "LandlordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblChatSession_tblLandlord_LandlordId",
                table: "tblChatSession",
                column: "LandlordId",
                principalTable: "tblLandlord",
                principalColumn: "LandlordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblContract_tblLandlord_LandlordId",
                table: "tblContract",
                column: "LandlordId",
                principalTable: "tblLandlord",
                principalColumn: "LandlordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblInvoice_tblLandlord_LandlordId",
                table: "tblInvoice",
                column: "LandlordId",
                principalTable: "tblLandlord",
                principalColumn: "LandlordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblReview_tblContract_ContractId",
                table: "tblReview",
                column: "ContractId",
                principalTable: "tblContract",
                principalColumn: "ContractId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_tblRoom_tblProperty_PropertyId",
                table: "tblRoom",
                column: "PropertyId",
                principalTable: "tblProperty",
                principalColumn: "PropertyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblService_tblLandlord_LandlordId",
                table: "tblService",
                column: "LandlordId",
                principalTable: "tblLandlord",
                principalColumn: "LandlordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblTenant_tblLandlord_LandlordId",
                table: "tblTenant",
                column: "LandlordId",
                principalTable: "tblLandlord",
                principalColumn: "LandlordId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblBookingRequest_tblLandlord_LandlordId",
                table: "tblBookingRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_tblChatSession_tblLandlord_LandlordId",
                table: "tblChatSession");

            migrationBuilder.DropForeignKey(
                name: "FK_tblContract_tblLandlord_LandlordId",
                table: "tblContract");

            migrationBuilder.DropForeignKey(
                name: "FK_tblInvoice_tblLandlord_LandlordId",
                table: "tblInvoice");

            migrationBuilder.DropForeignKey(
                name: "FK_tblReview_tblContract_ContractId",
                table: "tblReview");

            migrationBuilder.DropForeignKey(
                name: "FK_tblRoom_tblProperty_PropertyId",
                table: "tblRoom");

            migrationBuilder.DropForeignKey(
                name: "FK_tblService_tblLandlord_LandlordId",
                table: "tblService");

            migrationBuilder.DropForeignKey(
                name: "FK_tblTenant_tblLandlord_LandlordId",
                table: "tblTenant");

            migrationBuilder.DropTable(
                name: "tblFavorite");

            migrationBuilder.DropTable(
                name: "tblProperty");

            migrationBuilder.DropTable(
                name: "tblRoomAmenity");

            migrationBuilder.DropTable(
                name: "tblRoomImage");

            migrationBuilder.DropTable(
                name: "tblLandlord");

            migrationBuilder.DropTable(
                name: "tblWard");

            migrationBuilder.DropTable(
                name: "tblAmenity");

            migrationBuilder.DropTable(
                name: "tblDistrict");

            migrationBuilder.DropTable(
                name: "tblProvince");

            migrationBuilder.DropIndex(
                name: "IX_tblTenant_LandlordId",
                table: "tblTenant");

            migrationBuilder.DropIndex(
                name: "IX_tblService_LandlordId",
                table: "tblService");

            migrationBuilder.DropIndex(
                name: "IX_tblRoom_ApprovalStatus",
                table: "tblRoom");

            migrationBuilder.DropIndex(
                name: "IX_tblRoom_PropertyId",
                table: "tblRoom");

            migrationBuilder.DropIndex(
                name: "IX_tblRoom_RoomPrice",
                table: "tblRoom");

            migrationBuilder.DropIndex(
                name: "IX_tblRoom_Slug",
                table: "tblRoom");

            migrationBuilder.DropIndex(
                name: "IX_tblReview_ContractId",
                table: "tblReview");

            migrationBuilder.DropIndex(
                name: "IX_tblInvoice_LandlordId",
                table: "tblInvoice");

            migrationBuilder.DropIndex(
                name: "IX_tblContract_LandlordId",
                table: "tblContract");

            migrationBuilder.DropIndex(
                name: "IX_tblChatSession_LandlordId",
                table: "tblChatSession");

            migrationBuilder.DropIndex(
                name: "IX_tblBookingRequest_LandlordId",
                table: "tblBookingRequest");

            migrationBuilder.DropColumn(
                name: "LandlordId",
                table: "tblTenant");

            migrationBuilder.DropColumn(
                name: "LandlordId",
                table: "tblService");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "tblRoom");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "tblRoom");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "tblRoom");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "tblRoom");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "tblRoom");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "tblRoom");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "tblRoom");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "tblRoom");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "tblReview");

            migrationBuilder.DropColumn(
                name: "LandlordId",
                table: "tblInvoice");

            migrationBuilder.DropColumn(
                name: "LandlordId",
                table: "tblContract");

            migrationBuilder.DropColumn(
                name: "LandlordId",
                table: "tblChatSession");

            migrationBuilder.DropColumn(
                name: "LandlordId",
                table: "tblBookingRequest");

            migrationBuilder.DropColumn(
                name: "ViewingTime",
                table: "tblBookingRequest");
        }
    }
}
