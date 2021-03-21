using System.ComponentModel.DataAnnotations;

namespace MafaniaBot.Models
{
    public class Question
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public long ChatId { get; set; }

        [Required]
        public int FromUserId { get; set; }

        [Required]
        public int ToUserId { get; set; }

        [Required]
        public string Text { get; set; }
    }
}