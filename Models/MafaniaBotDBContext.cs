using Microsoft.EntityFrameworkCore;

namespace MafaniaBot.Models
{
	public class MafaniaBotDBContext : DbContext
	{
		public DbSet<MyChatMember> MyChatMembers { get; set; }

		public DbSet<Participant> AskAnonymousParticipants { get; set; }

		public DbSet<PendingQuestion> PendingAnonymousQuestions { get; set; }

		public DbSet<PendingAnswer> PendingAnonymousAnswers { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
		{
			base.OnConfiguring(optionsBuilder);
			optionsBuilder.UseMySQL(Startup.DATABASE_URL);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<MyChatMember>();
			modelBuilder.Entity<Participant>();
			modelBuilder.Entity<PendingQuestion>();
			modelBuilder.Entity<PendingAnswer>();
		}		
	}
}
