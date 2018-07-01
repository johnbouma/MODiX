namespace Modix.Data.Models.Infractions
{
    using System;
    public class Warning : Infraction
    {
        public Warning(long affectedUserId,
            long creatingUserId,
            DiscordGuild guild,
            string reason) 
            : base(InfractionType.Warning,
                affectedUserId,
                creatingUserId,
                guild,
                reason,
                DateTime.UtcNow)
        {
        }
    }
}
