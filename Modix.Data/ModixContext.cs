namespace Modix.Data
{
    using Microsoft.EntityFrameworkCore;
    using Modix.Data.EntityTypeConfigurations;
    using Modix.Data.Models;
    using Modix.Data.Models.Infractions;
    public class ModixContext : DbContext
    {
        public ModixContext(DbContextOptions<ModixContext> options) : base(options)
        {

        }

        private ModixContext() { }

        public DbSet<Infraction> Infractions { get; set; }
        public DbSet<DiscordGuild> Guilds { get; set; }
        public DbSet<DiscordMessage> Messages { get; set; }
        public DbSet<DiscordUser> Users { get; set; }
        public DbSet<ChannelLimit> ChannelLimits { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration(new InfractionTypeConfiguration());
        }
    }
}
