namespace Modix.Data.Models.Infractions
{
    using System;
    public class Mute : Infraction
    {
        public Mute(long affectedUserId,
            long creatingUserId,
            DiscordGuild guild,
            string reason,
            TimeSpan length) 
            : base(InfractionType.Mute,
                affectedUserId,
                creatingUserId,
                guild,
                reason,
                length)
        { }
    }
}
