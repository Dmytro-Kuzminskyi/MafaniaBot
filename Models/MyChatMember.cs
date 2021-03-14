using System.ComponentModel.DataAnnotations;

namespace MafaniaBot.Models
{
	public class MyChatMember
	{
		[Required]
		public int Id { get; set; }

		[Required]
		public int UserId { get; set; }
	}
}
