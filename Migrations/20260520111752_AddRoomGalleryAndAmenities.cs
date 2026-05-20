using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyPhongTro.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomGalleryAndAmenities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.tblRoomReview', 'TenantId') IS NULL
    ALTER TABLE dbo.tblRoomReview ADD TenantId int NULL;

IF COL_LENGTH('dbo.tblRoomReview', 'UserId') IS NULL
    ALTER TABLE dbo.tblRoomReview ADD UserId int NULL;

IF COL_LENGTH('dbo.tblRoom', 'GalleryImages') IS NULL
    ALTER TABLE dbo.tblRoom ADD GalleryImages nvarchar(max) NULL;

IF COL_LENGTH('dbo.tblRoom', 'IncludedAmenities') IS NULL
    ALTER TABLE dbo.tblRoom ADD IncludedAmenities nvarchar(max) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.tblRoom', 'IncludedAmenities') IS NOT NULL
    ALTER TABLE dbo.tblRoom DROP COLUMN IncludedAmenities;

IF COL_LENGTH('dbo.tblRoom', 'GalleryImages') IS NOT NULL
    ALTER TABLE dbo.tblRoom DROP COLUMN GalleryImages;

IF COL_LENGTH('dbo.tblRoomReview', 'UserId') IS NOT NULL
    ALTER TABLE dbo.tblRoomReview DROP COLUMN UserId;

IF COL_LENGTH('dbo.tblRoomReview', 'TenantId') IS NOT NULL
    ALTER TABLE dbo.tblRoomReview DROP COLUMN TenantId;
");
        }
    }
}
