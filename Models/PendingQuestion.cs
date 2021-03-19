using System.ComponentModel.DataAnnotations;

namespace MafaniaBot.Models
{
	public class PendingQuestion
	{
		[Required]
		public int Id { get; set; }

		[Required]
		public long ChatId { get; set; }

		[Required]
		public int FromUserId { get; set; }

		public int ToUserId { get; set; }

		public string ToUserName { get; set; }
	}
}
