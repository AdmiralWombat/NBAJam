using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NBAJam.Models;

namespace NBAJam.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamTournament> TeamTournaments { get; set; }
        public DbSet<PlayerTournament> PlayerTournaments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TeamTournament>()
                .HasKey(tt => new { tt.TeamId, tt.TournamentId });

            modelBuilder.Entity<TeamTournament>()
                .HasOne(tt => tt.Team)
                .WithMany(t => t.TeamTournaments)
                .HasForeignKey(tt => tt.TeamId);

            modelBuilder.Entity<TeamTournament>()
                .HasOne(tt => tt.Tournament)
                .WithMany(t => t.TeamTournaments)
                .HasForeignKey(tt => tt.TournamentId);



            modelBuilder.Entity<PlayerTournament>()
                .HasKey(tt => new { tt.PlayerID, tt.TournamentId });

            modelBuilder.Entity<PlayerTournament>()
                .HasOne(tt => tt.Player)
                .WithMany(t => t.PlayerTournaments)
                .HasForeignKey(tt => tt.PlayerID);

            modelBuilder.Entity<PlayerTournament>()
                .HasOne(tt => tt.Tournament)
                .WithMany(t => t.PlayerTournaments)
                .HasForeignKey(tt => tt.TournamentId);
        }
    }
}
