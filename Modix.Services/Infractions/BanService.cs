using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Modix.Data;
using Modix.Data.Utilities;

namespace Modix.Services.Infractions
{
    public class BanService: InfractionService<Data.Models.Infractions.Ban>
    {
        public BanService(ModixContext context): base(context)
        {
        }

        public async Task BanAsync(IGuildUser author, IGuildUser user, IGuild guild, string reason, CancellationToken token = default(CancellationToken))
        {
            var dbGuild = await GuildService.ObtainAsync(guild);

            var ban = new Data.Models.Infractions.Ban(
                user.Id.ToLong(),
                author.Id.ToLong(),
                dbGuild,
                reason);
            await base.AddAsync(ban, token);

        }

        public async Task UnbanAsync(ulong guildId, ulong userId, long deactivatorId, CancellationToken token = default(CancellationToken))
        {
            var banResult = await Context.Infractions.OfType<Data.Models.Infractions.Ban>().AsQueryable()
                                .Where(ban => ban.UserId == userId.ToLong() && ban.Guild.Id == guildId.ToLong())
                                .FirstAsync();
            await base.DeactivateAsync(banResult, deactivatorId, token);
        }

        public async Task<string> GetAllBans(IGuildUser user)
        {
            var bans = base.GetAllForUser(user);
            var sb = new StringBuilder();
            await bans.ForEachAsync(ban => sb.AppendLine($"{ban.Reason} | Active: {ban.Active}"));
            return sb.ToString();
        }
    }
}
