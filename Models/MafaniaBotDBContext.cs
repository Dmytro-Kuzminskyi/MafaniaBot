using Microsoft.EntityFrameworkCore;

namespace MafaniaBot.Models
{
	public class MafaniaBotDBContext : DbContext
	{
		public DbSet<Participant> AskAnonymousParticipants { get; set; }

		public DbSet<PendingQuestion> PendingAnonymousQuestions { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);
			optionsBuilder.UseMySQL(Startup.DB_CS);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<Participant>();
			modelBuilder.Entity<PendingQuestion>();
		}		
	}
}
