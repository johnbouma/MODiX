namespace Modix.Data.Models.Infractions
{
    using System;
    public class Ban : Infraction
    {
        public Ban(long affectedUserId,
            long creatingUserId,
            DiscordGuild guild,
            string reason) 
            : base(InfractionType.Ban,
                affectedUserId,
                creatingUserId,
                guild,
                reason,
                DateTime.MaxValue)
        { }
    }
}
