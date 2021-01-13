using Microsoft.EntityFrameworkCore;

namespace MafaniaBot.Models
{
	public class MafaniaBotContext : DbContext
	{
		public DbSet<MyGroup> MyGroups { get; set; }

		public MafaniaBotContext() { }
		public MafaniaBotContext(DbContextOptions<MafaniaBotContext> options) : base(options)
		{ }

		protected override void OnConfiguring(DbContextOptionsBuilder options)
		  => options.UseSqlServer(Startup.Conn);
	}

	public class MyGroup
	{
		public int Id { get; set; }
		public long ChatId { get; set; }
		public string Status { get; set; }
	}
}
