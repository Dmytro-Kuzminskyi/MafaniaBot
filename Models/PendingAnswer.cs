using System.ComponentModel.DataAnnotations;

namespace MafaniaBot.Models
{
	public class PendingAnswer
	{
		[Required]
		public int Id { get; set; }

		[Required]
		public long ChatId { get; set; }

		[Required]
		public int FromUserId { get; set; }

		[Required]
		public string FromUserName { get; set; }

		[Required]
		public int ToUserId { get; set; }

		[Required]
		public int MessageId { get; set; }
	}
}
