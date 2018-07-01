using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Modix.Services.Infractions;


namespace Modix.Modules
{
    [Name("Bans"), Summary("Manage user bans")]
    public class BanModule: ModuleBase
    {
        private BanService _service;
        public BanModule(BanService service)
        {
            _service = service;
        }

       [Command("ban"), Summary("Bans a user")]
       public async Task Ban([Summary("The user to ban.")] SocketGuildUser user,
           [Remainder, Summary("The reason for this ban.")] string reason)
        {
            if(Context.User is SocketGuildUser socketGuildUser)
            {
                if (socketGuildUser.GuildPermissions.BanMembers)
                {
                        await _service.BanAsync(socketGuildUser, user, Context.Guild, reason, CancellationToken.None);
                        await ReplyAsync($"{user.Nickname} was banned.");
                }
                else
                {
                    await ReplyAsync("You do not have the required permissions to perform this information.");
                }
            }
        }
    }
}
