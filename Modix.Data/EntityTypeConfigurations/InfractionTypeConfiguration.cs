using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modix.Data.Models.Infractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modix.Data.EntityTypeConfigurations
{
    internal sealed class InfractionTypeConfiguration : IEntityTypeConfiguration<Infraction>
    {
        public void Configure(EntityTypeBuilder<Infraction> builder)
        {
            builder.ToTable("Infractions")
                .HasDiscriminator<int>("Severity")
                .HasValue<Warning>((int)InfractionType.Warning)
                .HasValue<Mute>((int)InfractionType.Mute)
                .HasValue<Ban>((int)InfractionType.Ban);
        }
    }
}
