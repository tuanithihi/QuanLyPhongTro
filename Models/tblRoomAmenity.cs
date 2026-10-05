using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    [Table("tblRoomAmenity")]
    public class tblRoomAmenity
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        public int AmenityId { get; set; }

        // Navigation
        [ForeignKey(nameof(RoomId))]
        public virtual tblRoom? Room { get; set; }

        [ForeignKey(nameof(AmenityId))]
        public virtual tblAmenity? Amenity { get; set; }
    }
}
