using Microsoft.EntityFrameworkCore;
using PaymentsMatching.Models;

namespace PaymentsMatching.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MatchSession> MatchSessions => Set<MatchSession>();
        public DbSet<MatchResult> MatchResults => Set<MatchResult>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MatchSession>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<MatchResult>(e =>
            {
                e.HasKey(r => r.Id);
                e.HasIndex(r => new { r.SessionId, r.OrderId, r.Currency }).IsUnique();
                e.HasIndex(r => new { r.SessionId, r.IsResolved });
                e.Property(r => r.SystemAmount).HasColumnType("decimal(18,4)");
                e.Property(r => r.ProviderAmount).HasColumnType("decimal(18,4)");

                e.HasOne<MatchSession>()
                 .WithMany(s => s.Results)
                 .HasForeignKey(r => r.SessionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
