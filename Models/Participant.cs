using System.ComponentModel.DataAnnotations;

namespace MafaniaBot.Models
{
	public class Participant
	{
		[Required]
		public int Id { get; set; }
		[Required]
		public long ChatId { get; set; }
		[Required]
		public int UserId { get; set; }
	}
}
