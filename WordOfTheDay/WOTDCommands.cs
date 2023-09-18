using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace WordOfTheDay
{
	public class WOTDCommands : BaseCommandModule
	{
		/*
		[Command("base"), RequirePermissions(Permissions.Administrator), RequireGuild(), Description("base for other commands"), Aliases("b"), RequireOwner()]
		public async Task base(CommandContext ctx)
		{ 
		}
		*/

		[Command("SpamNoRoles"), RequireOwner(), RequirePermissions(Permissions.Administrator),RequireGuild(), Description("It sends a message to all the people who does not have a native role")]
		public async Task SpamNoRoles(CommandContext ctx)
		{
			if (HelperMethods.VerificarAsync(ctx.Channel, ctx.User, ctx.Client).Result == false) return;
			_ = Task.Run(async () =>
			{
				string updatestring = $"Spam no roles requested by {ctx.Member.Mention}";
				DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
					.WithTitle("*Getting all users from the server*")
					.WithDescription(updatestring)
					.WithColor(DiscordColor.Yellow);

				var updatemessage = await ctx.RespondAsync(embedBuilder.Build());
				var members = ctx.Guild.GetAllMembersAsync().Result.ToList();
				embedBuilder.WithTitle("*Sorting users. Please wait*");
				await updatemessage.ModifyAsync(embedBuilder.Build());
				List<DiscordMember> SpamableMembers = new List<DiscordMember>();
				DateTime lastEdit = DateTime.Now;
				int max = members.Count;
				for (int i = 0; i < max; i++)
				{
					if (members[i].Roles.Count() == 0)
					{
						SpamableMembers.Add(members[i]);
					}
					if (DateTime.Now - lastEdit > TimeSpan.FromSeconds(8))
					{
						lastEdit = DateTime.Now;
						updatemessage.ModifyAsync(
						embedBuilder
						.WithDescription(HelperMethods.GenerateProgressBar(((double)i / max)) + $"\n{i}/{max}").Build());
					}
					if (i % 10 == 0) HelperMethods.Delay(100);
				}
				embedBuilder
					.WithTitle($"*Found {SpamableMembers.Count} users*")
					.WithDescription("Do you want to DM all the users?")
					.WithColor(DiscordColor.Blue);
				updatemessage.ModifyAsync(embedBuilder.Build());
				if (HelperMethods.VerificarAsync(ctx.Channel, ctx.User, ctx.Client).Result == false) return;

				int UsersWithClosedDM = 0;
				lastEdit = DateTime.Now;
				embedBuilder.WithTitle("*Sending DM's to all the users in the list* **[MOCKUP]**").WithColor(DiscordColor.Yellow); ;
				for (int i = 0; i < SpamableMembers.Count; i++)
				{
					/*try
					{
						var dmchannel = await SpamableMembers[i].CreateDmChannelAsync();
						dmchannel.SendMessageAsync("test")
					}
					catch (UnauthorizedException e)
					{
						UsersWithClosedDM++;
					}*/
					if (i % 5 == 0) HelperMethods.Delay(50);
					if (DateTime.Now - lastEdit > TimeSpan.FromSeconds(8))
					{
						lastEdit = DateTime.Now;
						updatemessage.ModifyAsync(
						embedBuilder
						.WithDescription(HelperMethods.GenerateProgressBar(((double)i / max)) + $"\n{i}/{max}").Build());
					}
				}
				embedBuilder
					.WithTitle("*All the users have recieved a message*")
					.WithDescription($"Except {UsersWithClosedDM} that have closed DM's\n" + updatestring)
					.WithColor(DiscordColor.Green);
				updatemessage.ModifyAsync(embedBuilder.Build());
			});
		}
		[Command("speak"), RequireOwner(), RequireGuild(), Description("Speaks"), Aliases("s")]
		public async Task speak(CommandContext ctx, ulong id, [RemainingText] string msg)
		{
			_ = Task.Run(async () =>
			{
				var Channel = ctx.Guild.GetChannel(id);
				Channel.TriggerTypingAsync();
				HelperMethods.Delay(msg.Length * 250 > 3500 ? 3500 : msg.Length * 250);
				Channel.SendMessageAsync(msg);
			});
		}

		[Command("UpdateWOTD"), RequirePermissions(Permissions.ManageChannels), RequireGuild(), Description("Updates current WOTD")]
		public async Task UpdateWOTD(CommandContext ctx)
		{ 
		}

}
}

