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
    }
}
