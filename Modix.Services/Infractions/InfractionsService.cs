using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Modix.Data;
using Modix.Data.Models.Infractions;
using Modix.Data.Services;
using Modix.Data.Utilities;

namespace Modix.Services.Infractions
{
    public abstract class InfractionService<T> where T : Infraction
    {
        private readonly DiscordGuildService _guildService;
        protected DiscordGuildService GuildService => _guildService;

        private readonly ModixContext _context;
        protected ModixContext Context => _context;

        protected InfractionService(ModixContext context)
        {
            _context = context;
            _guildService = new DiscordGuildService(context);
        }

        protected IAsyncEnumerable<T> GetAllForUser(IGuildUser user)
        {
            return _context.Infractions.OfType<T>().Where(x => x.Active && x.Guild.Id == user.GuildId.ToLong()).ToAsyncEnumerable();
        }

        protected async Task AddAsync(T newItem, CancellationToken token = default(CancellationToken))
        {
            await _context.Infractions.AddAsync(newItem, token);
            await _context.SaveChangesAsync(token);
        }

        protected async Task DeactivateAsync(T item, long deactivatorId, CancellationToken token = default(CancellationToken))
        {
            item.Deactivate(deactivatorId);
            _context.Infractions.Update(item);
            await _context.SaveChangesAsync(token);
        }

    }
}
