﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Modix.Bot.Extensions;
using Modix.Data.Models.Core;
using Modix.Data.Repositories;
using Modix.Services.Core;
using Modix.Services.Moderation;
using Modix.Services.Utilities;

namespace Modix.Modules
{
    public class UserInfoModule : ModuleBase
    {
        private const string Format = "{0}: {1} ago ({2:yyyy-MM-ddTHH:mm:ssK})\n";

        //optimization: UtcNow is slow and the module is created per-request
        private readonly DateTime _utcNow = DateTime.UtcNow;

        public UserInfoModule(
            ILogger<UserInfoModule> logger,
            IUserService userService,
            IModerationService moderationService,
            IAuthorizationService authorizationService,
            IMessageRepository messageRepository,
            IHttpClientFactory httpClientFactory)
        {
            Log = logger ?? new NullLogger<UserInfoModule>();
            UserService = userService;
            ModerationService = moderationService;
            AuthorizationService = authorizationService;
            MessageRepository = messageRepository;
            HttpClientFactory = httpClientFactory;
        }

        private ILogger<UserInfoModule> Log { get; }
        private IUserService UserService { get; }
        private IModerationService ModerationService { get; }
        private IAuthorizationService AuthorizationService { get; }
        private IMessageRepository MessageRepository { get; }
        private IHttpClientFactory HttpClientFactory { get; }

        [Command("info")]
        public async Task GetUserInfoAsync(DiscordUserEntity user = null)
        {
            user = user ?? new DiscordUserEntity(Context.User.Id);

            var timer = Stopwatch.StartNew();

            var userInfo = await UserService.GetUserInformationAsync(Context.Guild.Id, user.Id);

            if (userInfo == null)
            {
                await ReplyAsync("", embed: new EmbedBuilder()
                    .WithTitle("Retrieval Error")
                    .WithColor(Color.Red)
                    .WithDescription("Sorry, we don't have any data for that user - and we couldn't find any, either.")
                    .AddField("User Id", user.Id)
                    .Build());

                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("**\u276F User Information**");
            builder.AppendLine("ID: " + userInfo.Id);
            builder.AppendLine("Profile: " + MentionUtils.MentionUser(userInfo.Id));

            if (userInfo.IsBanned)
            {
                builder.AppendLine("Status: **Banned** \\🔨");

                if (await AuthorizationService.HasClaimsAsync(Context.User as IGuildUser, AuthorizationClaim.ModerationRead))
                {
                    builder.AppendLine($"Ban Reason: {userInfo.BanReason}");
                }
            }
            else
            {
                builder.AppendLine($"Status: {userInfo.Status.Humanize()}");
            }

            if (userInfo.FirstSeen is DateTimeOffset firstSeen)
                builder.Append(FormatTimeAgo("First Seen", firstSeen));

            if (userInfo.LastSeen is DateTimeOffset lastSeen)
                builder.Append(FormatTimeAgo("Last Seen", lastSeen));

            try
            {
                await AddParticipationToEmbedAsync(user.Id, builder);
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "An error occured while retrieving a user's message count.");
            }

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(userInfo.Username + "#" + userInfo.Discriminator)
                .WithTimestamp(_utcNow);

            var avatar = userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl();

            embedBuilder.ThumbnailUrl = avatar;
            embedBuilder.Author.IconUrl = avatar;

            await AddMemberInformationToEmbedAsync(userInfo, builder, embedBuilder);

            if (await AuthorizationService.HasClaimsAsync(Context.User as IGuildUser, AuthorizationClaim.ModerationRead))
            {
                await AddInfractionsToEmbedAsync(user.Id, builder);
            }

            embedBuilder.Description = builder.ToString();

            timer.Stop();
            embedBuilder.WithFooter(footer => footer.Text = $"Completed after {timer.ElapsedMilliseconds} ms");

            await ReplyAsync(string.Empty, embed: embedBuilder.Build());
        }

        private async Task AddMemberInformationToEmbedAsync(EphemeralUser member, StringBuilder builder, EmbedBuilder embedBuilder)
        {
            builder.AppendLine();
            builder.AppendLine("**\u276F Member Information**");

            if (!string.IsNullOrEmpty(member.Nickname))
            {
                builder.AppendLine("Nickname: " + member.Nickname);
            }

            builder.Append(FormatTimeAgo("Created", member.CreatedAt));

            if (member.JoinedAt is DateTimeOffset joinedAt)
            {
                builder.Append(FormatTimeAgo("Joined", joinedAt));
            }

            if (member.RoleIds?.Count > 0)
            {
                var roles = member.RoleIds.Select(x => member.Guild.Roles.Single(y => y.Id == x))
                    .Where(x => x.Id != x.Guild.Id) // @everyone role always has same ID than guild
                    .ToArray();

                if (roles.Length > 0)
                {
                    Array.Sort(roles); // Sort by position: lowest positioned role is first
                    Array.Reverse(roles); // Reverse the sort: highest positioned role is first

                    builder.Append(roles.Length > 1 ? "Roles: " : "Role: ");
                    builder.AppendLine(roles.Select(r => r.Mention).Humanize());
                }
            }

            if ((member.GetAvatarUrl(size: 16) ?? member.GetDefaultAvatarUrl()) is string avatarUrl)
            {
                using (var httpStream = await HttpClientFactory.CreateClient().GetStreamAsync(avatarUrl))
                {
                    using (var avatarStream = new MemoryStream())
                    {
                        await httpStream.CopyToAsync(avatarStream);

                        var avatar = new Image(avatarStream);

                        embedBuilder.WithColor(FormatUtilities.GetDominantColor(avatar));
                    }
                }
            }
        }

        private async Task AddInfractionsToEmbedAsync(ulong userId, StringBuilder builder)
        {
            builder.AppendLine();
            builder.AppendLine($"**\u276F Infractions [See here](https://mod.gg/infractions?subject={userId})**");

            var counts = await ModerationService.GetInfractionCountsForUserAsync(userId);

            builder.AppendLine(FormatUtilities.FormatInfractionCounts(counts));
        }

        private async Task AddParticipationToEmbedAsync(ulong userId, StringBuilder builder)
        {
            var userRank = await MessageRepository.GetGuildUserParticipationStatistics(Context.Guild.Id, userId);
            var messagesByDate = await MessageRepository.GetGuildUserMessageCountByDate(Context.Guild.Id, userId, TimeSpan.FromDays(30));

            var lastWeek = _utcNow - TimeSpan.FromDays(7);

            var weekTotal = 0;
            var monthTotal = 0;
            foreach (var kvp in messagesByDate)
            {
                if (kvp.Key >= lastWeek)
                {
                    weekTotal += kvp.Value;
                }

                monthTotal += kvp.Value;
            }

            builder.AppendLine();
            builder.AppendLine("**\u276F Guild Participation**");

            if (userRank?.Rank > 0)
            {
                builder.AppendFormat("Rank: {0} {1}\n", userRank.Rank.Ordinalize(), GetParticipationEmoji(userRank));
            }

            builder.AppendLine("Last 7 days: " + weekTotal + " messages");
            builder.AppendLine("Last 30 days: " + monthTotal + " messages");

            if (monthTotal > 0)
            {
                builder.AppendFormat(
                    "Avg. per day: {0} messages (top {1} percentile)\n",
                    decimal.Round(userRank.AveragePerDay, 3),
                    userRank.Percentile.Ordinalize());

                try
                {
                    var channels = await MessageRepository.GetGuildUserMessageCountByChannel(Context.Guild.Id, userId, TimeSpan.FromDays(30));

                    foreach (var kvp in channels.OrderByDescending(x => x.Value))
                    {
                        var channel = await Context.Guild.GetChannelAsync(kvp.Key);

                        if (channel.IsPublic())
                        {
                            builder.AppendLine($"Most active channel: {MentionUtils.MentionChannel(channel.Id)} ({kvp.Value} messages)");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogDebug(ex, "Unable to get the most active channel for {UserId}.", userId);
                }
            }
        }

        private string FormatTimeAgo(string prefix, DateTimeOffset ago)
        {
            var span = _utcNow - ago;

            var humanizedTimeAgo = span > TimeSpan.FromSeconds(60)
                ? span.Humanize(maxUnit: TimeUnit.Year, culture: CultureInfo.InvariantCulture)
                : "a few seconds";

            return string.Format(CultureInfo.InvariantCulture, Format, prefix, humanizedTimeAgo, ago.UtcDateTime);
        }

        private string GetParticipationEmoji(GuildUserParticipationStatistics stats)
        {
            if (stats.Percentile == 100 || stats.Rank == 1)
            {
                return "🥇";
            }
            else if (stats.Percentile == 99 || stats.Rank == 2)
            {
                return "🥈";
            }
            else if (stats.Percentile == 98 || stats.Rank == 3)
            {
                return "🥉";
            }
            else if (stats.Percentile >= 95 && stats.Percentile < 98)
            {
                return "🏆";
            }

            return string.Empty;
        }
    }
}
