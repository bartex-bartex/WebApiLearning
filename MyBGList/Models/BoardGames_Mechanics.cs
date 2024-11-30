using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyBGList.Models
{
    [Table("BoardGames_Mechanics")]
    public class BoardGames_Mechanics
    {
        [Key]
        [Required]
        public int BoardGameId { get; set; }

        public BoardGame? BoardGame { get; set; }

        [Key]
        [Required]
        public int MechanicId { get; set; }

        public Mechanic? Mechanic { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }
    }
}
