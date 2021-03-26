using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Emzi0767.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace WordOfTheDay
{
	public class StudyCommands : BaseCommandModule
	{
		[Command("study"), RequireGuild()]
		public async Task Study(CommandContext ctx, [RemainingText] string subject)
		{
			//Get ID and check if exists on the DB
			String UserID = ctx.User.Id.ToString();
			TryAddMember(ctx.Member);

			//Variables
			DateTime StartTime = DateTime.Now;
			InteractivityExtension interactivity = ctx.Client.GetInteractivity();
			DiscordEmoji EmojiStopStudying = DiscordEmoji.FromName(ctx.Client, ":octagonal_sign:");
			TimeSpan StudyTime;
			DateTime EndTime;

			//Create and send embed
			DiscordMessage mensaje = await ctx.Member.SendMessageAsync(null, false,
				HelperMethods.QuickEmbed($"Studying {subject}", $"Please click on the reaction {EmojiStopStudying} when you stop studying", false
				)); //sendembed

			//Create emoji and wait for answer
			await mensaje.CreateReactionAsync(EmojiStopStudying);
			var em = await interactivity.WaitForReactionAsync(xe => xe.Emoji.Equals(EmojiStopStudying), ctx.User, TimeSpan.FromHours(4));
			if (!em.TimedOut)
			{
				//User clicks on the reaction
				EndTime = DateTime.Now;
				StudyTime = EndTime - StartTime;
				await mensaje.ModifyAsync("",
				HelperMethods.QuickEmbed($"Study Ended {subject}", $"You've studied `{subject}` for {StudyTime.Hours}:{StudyTime.Minutes}:{StudyTime.Seconds}", false, DiscordColor.Red.ToString())
				);
				DBInterface.Instance.AddTime(UserID, subject, StartTime, EndTime);
			} else
			{
				//timeout
				await mensaje.ModifyAsync("",
					HelperMethods.QuickEmbed("4 Hours Reached, React to confirm changes", "If not they will be lost.\n*Also this would be a good time to take care for youself and take a break*", false, DiscordColor.Yellow.ToString())
					);
				DiscordMessage mencion = await ctx.Member.SendMessageAsync(ctx.Member.Mention);

				//second confirm
				var confirm = await interactivity.WaitForReactionAsync(xe => xe.Emoji.Equals(EmojiStopStudying), ctx.User, TimeSpan.FromMinutes(5));
				if (!confirm.TimedOut)
				{
					EndTime = DateTime.Now;
					StudyTime = EndTime - StartTime;
					await mensaje.ModifyAsync("",
							HelperMethods.QuickEmbed("Finished Studying", $"You've studied `{subject}` for {StudyTime.Hours:00}:{StudyTime.Minutes:00}", false, DiscordColor.Green.ToString())
						);
					DBInterface.Instance.AddTime(UserID, subject, StartTime, EndTime);
					await mencion.DeleteAsync();
				} else
				{
					//timeout and canel
					await mensaje.ModifyAsync("",
						HelperMethods.QuickEmbed("Exceeded time limit", "No Study time has been added", false, DiscordColor.Red.ToString())
						);
					await mencion.DeleteAsync();
				}
			}
		}

		//Help command when not enough params are added
		[Command("study")]
		public async Task Study(CommandContext ctx)
		{
			DiscordMessage mensaje = await ctx.RespondAsync("You need to put a subject!");
			System.Threading.Thread.Sleep(5000);
			await mensaje.DeleteAsync();
		}

		[Command("addhours"), RequireGuild()]
		public async Task Addhours(CommandContext ctx, string hours, [RemainingText] string subject)
		{
			String UserID = ctx.User.Id.ToString();
			TryAddMember(ctx.Member);

			DateTime StartTime = DateTime.Now.Date;
			// 30 => 30h | 3:00 => 3h
			TimeSpan tiempo = hours.Contains(":") ? TimeSpan.Parse(hours) : TimeSpan.FromHours(int.Parse(hours));
			int daystosubstract = tiempo.Days;
			StartTime = StartTime.Subtract(TimeSpan.FromDays(tiempo.Days));
			DateTime EndTime = StartTime.AddHours(tiempo.TotalHours);
			DBInterface.Instance.AddTime(UserID, subject, StartTime, EndTime);
			await ctx.RespondAsync(null, false,
				HelperMethods.QuickEmbed($"Added {tiempo} hours to the Study Tracker", $"Subject: {subject}", false)
				);
		}


		[Command("addhours"), RequireGuild()]
		public async Task Addhours(CommandContext ctx, string hours)
		{
			String UserID = ctx.User.Id.ToString();
			TryAddMember(ctx.Member);

			DateTime StartTime = DateTime.Now.Date;
			TimeSpan tiempo = hours.Contains(":") ? TimeSpan.Parse(hours) : TimeSpan.FromHours(int.Parse(hours));
			int daystosubstract = tiempo.Days;
			StartTime = StartTime.Subtract(TimeSpan.FromDays(tiempo.Days));
			DateTime EndTime = StartTime.AddHours(tiempo.TotalHours);
			DBInterface.Instance.AddTime(UserID, "Unspecified", StartTime, EndTime);
			await ctx.RespondAsync(null, false,
				HelperMethods.QuickEmbed($"Added {tiempo} hours to the Study Tracker", $"No Subject Provided", false)
				);
		}
		//Help command when not enough params are added
		[Command("addhours")]
		public async Task Addhours(CommandContext ctx)
		{
			DiscordMessage mensaje = await ctx.RespondAsync("You need to put a the number of hours and the subject! *in that order*");
			System.Threading.Thread.Sleep(5000);
			await mensaje.DeleteAsync();
		}

		[Command("gethours")]
		public async Task GetHours(CommandContext ctx)
		{
			String userid = ctx.User.Id.ToString();
			TimeSpan horas = DBInterface.Instance.GetTotalHoursByID(userid);

			Study_WorkSheet[] hours = DBInterface.Instance.GetRegsbyID(userid);
			int maxregs = hours.Length > 5 ? 5 : hours.Length;
			string des = $"Last {maxregs}\n";
			for (int i = 0; i < maxregs; i++)
			{
				des += $"{i + 1} - {hours[i].Subject} - {hours[i].TotalTime}\n";
			}
			await ctx.RespondAsync(null, false,
				HelperMethods.QuickEmbed(ctx.Member.DisplayName + " Has " + horas.ToString() + " Hours", des, false)
				);
		}

		[Command("ranking")]
		public async Task Ranking(CommandContext ctx)
		{
			var toppu = DBInterface.Instance.GetRanking().Take(5);
			int rank = 0;
			DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
			foreach (var item in toppu)
			{
				rank++;
				builder.AddField($"{rank} - {item.Key}", "" + item.Value.ToString());
			}

			builder
				.WithTitle($"Top {rank}").
				WithColor(new DiscordColor("#970045"));

			await ctx.RespondAsync(null, false,
			   builder.Build()
				);
		}

		[Command("exec"), RequireGuild(), RequireOwner(), Hidden()]
		public async Task Exec(CommandContext ctx, [RemainingText] string SQL)
		{
			ctx.RespondAsync("```" + DBInterface.Instance.Exec(SQL) + "```");
			System.Threading.Thread.Sleep(5000);
			ctx.Message.DeleteAsync();
		}
		[Command("execq"), RequireOwner(), Hidden()]
		public async Task ExecQ(CommandContext ctx, [RemainingText] string SQL)
		{
			string salida = "```" + DBInterface.Instance.ExecQuery(SQL) + "```";
			if (salida.Length >= 2000)
			{
				ctx.RespondWithFileAsync($"{DateTime.Now:s}Query.txt", HelperMethods.StringToMemoryStream(salida));
			} else
			{
				ctx.RespondAsync(salida);
			}
			System.Threading.Thread.Sleep(5000);
			ctx.Message.DeleteAsync();
		}


		//TODO: Think if these methods should be here or in DBInterface

		/// <summary>
		/// It adds a member to the DB if that member does not exists
		/// </summary>
		/// <param name="member">Member to add</param>
		public void TryAddMember(DiscordMember member)
		{
			String id = member.Id.ToString();
			if (String.IsNullOrWhiteSpace(DBInterface.Instance.GetUserByID(id)))
			{
				DBInterface.Instance.AddUser(id, member.DisplayName);
			}
		}
		/// <summary>
		/// Checks if a member is on the Database
		/// </summary>
		/// <param name="member">Member to check</param>
		public bool HasMember(DiscordMember member)
		{
			return String.IsNullOrWhiteSpace(DBInterface.Instance.GetUserByID(member.Id.ToString()));
		}

		[Command("talk"), RequireGuild()]
		public async Task talk(CommandContext ctx, ulong channelid, [RemainingText] string msg)
		{
			ctx.Message.DeleteAsync();
			DiscordChannel channel = await ctx.Client.GetChannelAsync(channelid);
			channel.SendMessageAsync(msg);
		}
	}
}
