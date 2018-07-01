namespace Modix.Data.Models.Infractions
{
    using System;
    public class Infraction
    {
        public int Id { get; private set; }
        public long UserId { get; private set; }
        public long CreatorId { get; private set; }
        public DiscordGuild Guild { get; private set; }
        public string Reason { get; private set; }
        public bool Active { get; private set; }
        internal InfractionType Severity { get; private set; }
        public long? DeactivatedBy { get; private set; }
        public DateTime Begins { get; private set; }
        public DateTime Ends { get; private set; }
        private Infraction() { }
        internal Infraction(InfractionType severity,
            long affectedUserId,
            long creatingUserId,
            DiscordGuild guild,
            string reason,
            DateTime ends)
        {
            Severity = severity;
            UserId = UserId;
            CreatorId = CreatorId;
            Guild = guild;
            Reason = reason;
            Active = true;
            Begins = DateTime.UtcNow;
            Ends = ends;
        }

        internal Infraction(InfractionType severity,
            long affectedUserId,
            long creatingUserId,
            DiscordGuild guild,
            string reason,
            TimeSpan length)
        {
            Severity = severity;
            UserId = UserId;
            CreatorId = CreatorId;
            Guild = guild;
            Reason = reason;
            Active = true;
            Begins = DateTime.UtcNow;
            Ends = Begins + length;
        }

        public void Deactivate(long userId)
        {
            this.Active = false;
            this.Ends = DateTime.UtcNow;
            DeactivatedBy = userId;
        }

        public void Extend(TimeSpan toExtend)
        {
            if (Ends == DateTime.MaxValue) return;
            if (Active == false) return;
            if (Ends < DateTime.UtcNow) return;
            var currentEndingTime = Ends;
            Ends = currentEndingTime + toExtend;
        }

        public void Shorten(TimeSpan length)
        {
            if ((Ends - length < Begins) || (Ends - length < DateTime.UtcNow))
            {
                Ends = DateTime.UtcNow;
            }
            var currentEndTime = Ends;
            Ends = Ends - length;
        }
    }
}
