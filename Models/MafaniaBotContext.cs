using Microsoft.EntityFrameworkCore;

namespace MafaniaBot.Models
{
	public class MafaniaBotContext : DbContext
	{
		public DbSet<AskAnonymousParticipant> AskAnonymousParticipants { get; set; }

		public MafaniaBotContext() { }
		public MafaniaBotContext(DbContextOptions<MafaniaBotContext> options) : base(options)
		{ }

		protected override void OnConfiguring(DbContextOptionsBuilder options)
		  => options.UseSqlServer(Startup.Conn);
	}

	public class AskAnonymousParticipant
	{
		public int Id { get; set; }
		public long ChatId { get; set; }
		public long UserId { get; set; }
	}
}
