using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;

namespace WordOfTheDay
{

    public class Program
    {
        public readonly string version = "1.0.0";
        public readonly string internalname = "Holy shit. it's done?";
        public DiscordClient Client { get; set; }
        private static Program prog;

        DiscordGuild languageServer;

        private DiscordChannel languagechannel;
        private DiscordChannel conelBot;
        private DiscordChannel suggestions;
        private DiscordRole WOTDrole;
        private DiscordRole CorrectMeRole;




        public static void Main(string[] args)
        {
            prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync()
        {
            //Abrir JSON file
            string json = "";
            using (FileStream fs = File.OpenRead("config.json"))
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            ConfigJson cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json);
            DiscordConfiguration cfg = new DiscordConfiguration
            {
                Token = cfgjson.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            this.Client = new DiscordClient(cfg);
            
            this.Client.Ready += this.Client_Ready;
            this.Client.GuildAvailable += this.Client_GuildAvailable;
            this.Client.ClientErrored += this.Client_ClientError;
            this.Client.MessageCreated += Client_MessageCreated;
            this.Client.GuildMemberUpdated += Client_GuildMemberUpdated;

            await this.Client.ConnectAsync();

            languagechannel = await Client.GetChannelAsync(ulong.Parse(cfgjson.WOTDChannel)); //Channel which recieves updates
            languageServer = await Client.GetGuildAsync(ulong.Parse(cfgjson.LanguageServer)); //Server
            WOTDrole = languageServer.GetRole(ulong.Parse(cfgjson.WOTDRole)); //WOTD role
            CorrectMeRole = languageServer.GetRole(ulong.Parse(cfgjson.CorrectMeRole)); //CorrectMe Role
            suggestions = await Client.GetChannelAsync(ulong.Parse(cfgjson.Suggestions)); //Channel which recieves updates
            conelBot = await Client.GetChannelAsync(ulong.Parse(cfgjson.ConElBot));
            

           Thread WOTD = new Thread(() => SetUpTimer(14, 00));
            WOTD.Start();

            await Task.Delay(-1);
        }

        private Task Client_GuildMemberUpdated(GuildMemberUpdateEventArgs e)
        {
            CheckPencil(e.Member);
            return Task.CompletedTask;
        }


        private Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            //Esto es horrible pero bueno
            string mensaje = e.Message.Content.ToLower();

            if (mensaje.StartsWith("-wote") || mensaje.StartsWith("wote") || e.Message.ChannelId == suggestions.Id)
            {
                WoteAsync(e.Message);
            }

            if (!mensaje.StartsWith("-")) return Task.CompletedTask; //OPTIMIZAAAAAAR    

            if (mensaje.StartsWith("-ping"))
            {
                e.Message.RespondAsync("Pong! " + Client.Ping + "ms");
            }

            if (mensaje.StartsWith("-help"))
            {
                DiscordMember member = (DiscordMember)e.Author;
                member.SendMessageAsync(generateHelp(member));

                e.Message.RespondAsync(
                    DiscordEmoji.FromName(Client, ":flag_es:") + "Ayuda Enviada por mensaje privado\n"
                  + DiscordEmoji.FromName(Client, ":flag_gb:") + "Help sent via direct message");
            }

            if (mensaje.StartsWith("-sendwotd") && isAdmin(e.Author))
            {
                sendWOTDAsync();
            }

            if (mensaje.StartsWith("-checkpencils") && isAdmin(e.Author))
            {
                IEnumerable<DiscordUser> users = languagechannel.Users;
                foreach (DiscordUser user in users)
                {
                    CheckPencil((DiscordMember)user);
                }
            }
          
                //END OF IF WALL
                return Task.CompletedTask;
        }

       

        private string generateHelp(DiscordMember member)
        {
            bool admin = isAdmin(member);

            //ESP
            String salida = ">>> " + DiscordEmoji.FromName(Client, ":flag_es:") +
            "\n-Help: Muestra este texto de ayuda" +
            "\n-Ping: Muestra la latencia del server" +
            "\n-Wote: Inicia una votacion";
            if (admin) salida += "\n***Solo para administradores***" +
                    "\n-SendWOTD: Envia una nueva Palabra del dia" +
            "\n-CheckPencils: Checkea todos los usuarios, y pone o quita el emoji :pencil: segun tenga o no el rol de `Correct Me`";
            //ENG
            salida += "\n" + DiscordEmoji.FromName(Client, ":flag_gb:") +
            "\n-Help: Shows this help text" +
            "\n-Ping: Shows the server latency" +
            "\n-Wote: Starts a vote";
            if (admin) salida += "\n***Admin only***" +
                    "\n-SendWOTD: Sends a new Word of the day" +
            "\n-CheckPencils: Checks all users and gives or removes the :pencil: emoji depending if the user has the `Correct Me` role";

            return salida;
        }
       
        private Task Client_Ready(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "WordOfTheDay", "Client is ready to process events.", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "WordOfTheDay", $"Guild available: {e.Guild.Name}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "WordOfTheDay", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);
            prog.RunBotAsync().GetAwaiter().GetResult();
            return Task.CompletedTask;
        }

        private Task WoteAsync(DiscordMessage message)
        {
            message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":white_check_mark:"));
            message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));

            return Task.CompletedTask;
        }

        private bool isAdmin(DiscordMember member)
        {
            IEnumerable<DiscordRole> roles = member.Roles;
            foreach (DiscordRole rol in roles)
            {
                if (rol.Permissions.HasPermission(Permissions.Administrator))
                {
                    return true;
                }
            }
            return false;
        }
        private bool isAdmin(DiscordUser user)
        {                               //heh
            return isAdmin((DiscordMember)user);
        }

        public async Task<Task> sendWOTDAsync()
        {
            WordOfTheDay TodaysWOTD = Logic.GetXMLWOTD();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();


            embedBuilder.WithTitle("Word of the Day");
            embedBuilder.WithUrl(TodaysWOTD.link);
            embedBuilder.WithThumbnailUrl("https://cdn.discordapp.com/attachments/477632242190123027/603763546836303899/dummy.png");
            embedBuilder.WithFooter("A Yoshi's bot", "https://cdn.discordapp.com/avatars/66139444276625408/0ac88686553320332a02122749508fb5.jpg");
            embedBuilder.AddField(":flag_es:", $"{TodaysWOTD.es_word}\n{TodaysWOTD.es_sentence}", true);
            embedBuilder.AddField(":flag_gb:", $"{TodaysWOTD.en_word}\n{TodaysWOTD.en_sentence}", true);

            DiscordEmbed embed = embedBuilder.Build();
            
            await languagechannel.SendMessageAsync(WOTDrole.Mention, false, embed);

            return Task.CompletedTask;
        }

        private void CheckPencil(DiscordMember member)
        {
            bool endsOnPencil = member.DisplayName.EndsWith(DiscordEmoji.FromName(Client, ":pencil:"));

            bool hasrole = false;
            IEnumerable<DiscordRole> roles = member.Roles;
            foreach (DiscordRole rol in roles)
            {
                if (rol.Id == CorrectMeRole.Id)
                {
                    hasrole = true;
                }
            }
            if (hasrole && !endsOnPencil)
            {
                member.ModifyAsync(x =>
                {
                    x.Nickname = member.DisplayName + " " + DiscordEmoji.FromName(Client, ":pencil:");
                });
            }
            if (!hasrole && endsOnPencil)
            {
                if (member.DisplayName.Length >= 3)
                {
                    member.ModifyAsync(x =>
                    {
                        x.Nickname = member.DisplayName.Substring(0, member.DisplayName.Length - 2);
                    });
                } else
                {
                    member.ModifyAsync(x =>
                    {
                        x.Nickname = member.Username;
                    });
                }
            }
        }


        /// <summary>
        /// Envia una palabra del dia a la hora y minuto estimados (CEST)
        /// </summary>
        /// <param name="hora">Hora del dia a la que se envia la WOTD</param>
        /// <param name="minuto">Minuto de la hora a la que se envia la WOTD</param>
        /// <param name="segundo">Segundo de la hora la que se envia la WOTD</param>
        public void SetUpTimer(int hora, int minuto, int segundo = 15) //FIXME YOU BASTARD
        {
            while (true)
            {
                DateTime AHORA = DateTime.Now;
                //Esto seguro que se puede mejorar. Pero no lo voy a hacer
                DateTime proximoWOTD = new DateTime(AHORA.Year, AHORA.Month, (AHORA.Hour >= hora ? AHORA.Day + 1 : AHORA.Day), hora, minuto, segundo);
                TimeSpan diff = proximoWOTD - AHORA;
                int v = (int)diff.TotalMilliseconds;
                conelBot.SendMessageAsync();
                System.Threading.Thread.Sleep((int)diff.TotalMilliseconds);
                sendWOTDAsync();
            }
        }

        public struct ConfigJson
        {
            [JsonProperty("token")]
            public string Token { get; private set; }

            [JsonProperty("LanguageServer")]
            public string LanguageServer { get; private set; }

            [JsonProperty("WOTDChannel")]
            public string WOTDChannel { get; private set; }

            [JsonProperty("WOTDRole")]
            public string WOTDRole { get; private set; }

            [JsonProperty("CorrectMeRole")]
            public string CorrectMeRole { get; private set; }

            [JsonProperty("Suggestions")]
            public string Suggestions { get; private set; }

            [JsonProperty("ConElBot")]
            public string ConElBot { get; private set; }
        }
    }
}
