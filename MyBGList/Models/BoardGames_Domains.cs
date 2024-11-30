using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyBGList.Models
{
    [Table("BoardGames_Domains")]
    public class BoardGames_Domains
    {
        [Key]
        [Required]
        public int BoardGameId { get; set; }

        public BoardGame? BoardGame { get; set; }

        [Key]
        [Required]
        public int DomainId { get; set; }

        public Domain? Domain { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }
    }
}
