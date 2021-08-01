using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordOfTheDay
{
	public static class HelperMethods
	{
		const int PB_MAX_SIZE = 18;

		/// <summary>
		/// Genera un DiscordEmbed basico
		/// </summary>
		/// <param name="titulo">Titulo del embed</param>
		/// <param name="descripcion">Descripcion del embed</param>
		/// <param name="color">Cadena Hexadecimal para el color del embed</param>
		/// <param name="footerspam">Habilita el footerSpam "A Yoshi's bot"</param>
		/// <returns></returns>
		public static DiscordEmbed QuickEmbed(String titulo = "", string descripcion = "", bool footerspam = true, string color = "#970045")
		{


			DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
			builder.WithTitle(titulo)
				.WithDescription(descripcion)
				.WithColor(new DiscordColor(color));
			if (footerspam)
			{
				builder.WithFooter(
						   "A Yoshi's Bot",
						   "https://i.imgur.com/rT9YocG.jpg"
						   );
			}
			return builder.Build();
		}

		public static DiscordEmbed DiscordSpamEmbed
		{
			get
			{
				DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
				builder.WithFooter(
								   "A Yoshi's Bot",
								   "https://i.imgur.com/rT9YocG.jpg"
								   );
				return builder.Build();
			}
		}
		public static DiscordColor GetCurrentBotColor(CommandContext ctx)
		{
			//From the context we get the guild. From then we get the BotMember. Then we got their roles and from then we got the color of the first role
			var roles = ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).Result.Roles.OrderByDescending(o => o.Position).ToList();
			return roles.First().Color;
		}
		public static MemoryStream StringToMemoryStream(String input)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(input));
		}
		/// <summary>
		/// Generates a Text Based Progress bar
		/// </summary>
		/// <param name="percentage">Percentage of progress</param>
		/// <param name="max_size">Max Width in characters of the progress bar</param>
		/// <returns>A progress bar EX: ▰▱▱▱▱▱▱▱▱▱▱▱▱▱▱▱▱▱ - [10,00 %]</returns>
		public static string GenerateProgressBar(double percentage, int max_size = PB_MAX_SIZE)
		{
			double completed_PB = percentage * max_size;
			return $"*{new string('▰', (int)completed_PB) + new string('▱', max_size - (int)completed_PB)}*{$" - **[{percentage:P}]**"}";
		}
		/// <summary>
		/// It sleeps the program for a certain amount of time
		/// <para>Helpful to evade ratelimits</para>
		/// </summary>
		/// <param name="ms">Time in miliseconds of which program will be paused</param>
		public static void Delay(int ms = 500) => System.Threading.Thread.Sleep(ms);
		public static void Delay(TimeSpan timeSpan) => System.Threading.Thread.Sleep(timeSpan.Milliseconds);

		public static async Task<bool> VerificarAsync(DiscordChannel channel, DiscordUser autor, DiscordClient client, int time = 30)
		{
			DiscordMessage verificacion = await channel.SendMessageAsync(EasyDualLanguageFormatting("¿Estas seguro?", "Are you sure?"));
			await WoteAsync(verificacion, client, false);
			HelperMethods.Delay(1000);
			for (int i = 0; i < time; i++)
			{
				IReadOnlyList<DiscordUser> reaccionesOK = await verificacion.GetReactionsAsync(DiscordEmoji.FromName(client, ":white_check_mark:"));
				IReadOnlyList<DiscordUser> reaccionesNOPE = await verificacion.GetReactionsAsync(DiscordEmoji.FromName(client, ":x:"));
				if (reaccionesOK.Contains(autor))
				{
					await verificacion.DeleteAsync();
					return true;
				}
				if (reaccionesNOPE.Contains(autor))
				{
					await verificacion.DeleteAsync();
					return false;
				}
				HelperMethods.Delay(1000);
			}

			await verificacion.DeleteAsync();
			return false;
		}

		public static Task WoteAsync(DiscordMessage message, DiscordClient client, bool dunno = false)
		{
			message.CreateReactionAsync(DiscordEmoji.FromName(client, ":white_check_mark:"));
			HelperMethods.Delay();
			message.CreateReactionAsync(DiscordEmoji.FromName(client, ":x:"));
			if (dunno)
			{
				HelperMethods.Delay();
				message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(client, 614346797141458974));
			}
			return Task.CompletedTask;
		}

		public static string EasyDualLanguageFormatting(string EScontent, string ENcontent)
		{
			return $":flag_es: {EScontent}\n:flag_gb: {ENcontent}";
		}

		public static DiscordEmbed BuildWOTDEmbed(WordOfTheDay WOTD){

			DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(HelperMethods.DiscordSpamEmbed);

			embedBuilder.WithTitle("Word of the Day");
			embedBuilder.WithUrl(WOTD.link);
			embedBuilder.WithThumbnail("https://cdn.discordapp.com/attachments/477632242190123027/603763546836303899/dummy.png");
			embedBuilder.WithFooter("A Yoshi's bot", "https://i.imgur.com/rT9YocG.jpg");
			embedBuilder.AddField(":flag_es: - " + WOTD.es_word, $"{WOTD.es_sentence}", true);
			embedBuilder.AddField(":flag_gb: - " + WOTD.en_word, $"{WOTD.en_sentence}", true);
			embedBuilder.WithColor(new DiscordColor("#970045"));

			return embedBuilder.Build();
		}
	}
}
