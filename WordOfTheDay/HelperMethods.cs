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
	}
}
