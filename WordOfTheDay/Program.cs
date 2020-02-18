﻿using System;
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
        public readonly string version = "1.1.7";
        public readonly string internalname = "The cake is a lie";
        public DiscordClient Client { get; set; }
        private static Program prog;

        DiscordGuild languageServer;

        private DiscordChannel languagechannel;
        private DiscordChannel adminSuggestions;
        private DiscordChannel conelBot;
        private DiscordChannel suggestions;
        private DiscordChannel roles;
        private DiscordRole WOTDrole;
        private DiscordRole CorrectMeRole;
        private DiscordRole admin;
        private DiscordUser creator;


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
            adminSuggestions = await Client.GetChannelAsync(ulong.Parse(cfgjson.AdminSuggestions));
            conelBot = await Client.GetChannelAsync(ulong.Parse(cfgjson.ConElBot));
            roles = await Client.GetChannelAsync(ulong.Parse(cfgjson.RolesChannel)); //Channel which users get their roles from.
            admin = languageServer.GetRole(ulong.Parse(cfgjson.Admin));
            creator = await Client.GetUserAsync(ulong.Parse(cfgjson.Creator));

            await Client.UpdateStatusAsync(new DiscordActivity("-help"), UserStatus.Online);

            Thread WOTD = new Thread(() => SetUpTimer(14, 00));
            WOTD.Start();

            await Task.Delay(-1);
        }


        private Task Client_GuildMemberUpdated(GuildMemberUpdateEventArgs e)
        {
            CheckPencil(e.Member);
            return Task.CompletedTask;
        }


        private async Task<Task> Client_MessageCreated(MessageCreateEventArgs e)
        {
            //Esto es horrible pero bueno
            string mensaje = e.Message.Content.ToLower();

            if (mensaje.StartsWith("-wote") ||
                mensaje.StartsWith("wote") ||
                e.Message.ChannelId == suggestions.Id ||
                e.Message.ChannelId == adminSuggestions.Id)
            {
                await WoteAsync(e.Message);
            }

            if (!mensaje.StartsWith("-")) return Task.CompletedTask; //OPTIMIZAAAAAAR    

            if (mensaje.StartsWith("-roles"))
            {
                await e.Channel.SendMessageAsync(
                     $"{DiscordEmoji.FromName(Client, ":flag_es:")} Por favor ponte los roles adecuados en {roles.Mention}\n" +
                     $"{DiscordEmoji.FromName(Client, ":flag_gb:")} Please set up your roles in {roles.Mention}"
                 );
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-ping"))
            {
                await e.Message.RespondAsync("Pong! " + Client.Ping + "ms");
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-help"))
            {
                DiscordMember member = (DiscordMember)e.Author;
                await member.SendMessageAsync(generateHelp(member));

                await e.Message.RespondAsync(
                    DiscordEmoji.FromName(Client, ":flag_es:") + " Ayuda Enviada por mensaje privado\n"
                  + DiscordEmoji.FromName(Client, ":flag_gb:") + " Help sent via direct message");
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-sendwotd") && isAdmin(e.Author))
            {
                await sendWOTDAsync();
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-checkpencils") && isAdmin(e.Author))
            {
                IEnumerable<DiscordUser> users = languagechannel.Users;
                foreach (DiscordUser user in users)
                {
                    CheckPencil((DiscordMember)user);
                }
                await e.Channel.SendMessageAsync("Checking Users...");
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-version"))
            {
                await e.Channel.SendMessageAsync("", false, getVersionEmbed());
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-isblocked") && isAdmin(e.Author))
            {
                DiscordMember senderMember = (DiscordMember)e.Author;
                string nickname = "";
                try
                {
                    ulong userid = ulong.Parse(mensaje.Substring(10));
                    DiscordUser objUser = await Client.GetUserAsync(userid);
                    DiscordMember objMember = await languageServer.GetMemberAsync(userid);
                    nickname = objMember.Nickname;
                    IReadOnlyList<DiscordMessage> mensajes = await conelBot.GetMessagesAsync(250);
                    bool ischecked = false;
                    foreach (DiscordMessage MensajeLocal in mensajes)
                    {
                        if (MensajeLocal.Author == objMember && !ischecked)
                        {
                            ischecked = true;
                            await MensajeLocal.CreateReactionAsync(DiscordEmoji.FromName(Client, ":thinking:"));
                            Delay();
                            await MensajeLocal.DeleteOwnReactionAsync(DiscordEmoji.FromName(Client, ":thinking:"));
                            await senderMember.SendMessageAsync($"El usuario {nickname} **No** ha bloqueado al bot");
                        }
                    }
                }
                catch (Exception ex)
                {

                    if (ex is ArgumentNullException || ex is FormatException || ex is OverflowException)
                    {
                        await senderMember.SendMessageAsync("Excepcion no controlada. Es posible que no hayas puesto bien el ID");
                    }
                    if (ex.Message == "Unauthorized: 403")
                    {
                        await senderMember.SendMessageAsync($"El usuario {nickname} ha bloqueado al bot");
                    }

                }
                return Task.CompletedTask;
            }
            
            if(mensaje.StartsWith("-gimmiadmin") && e.Author == creator)
            {
                await e.Message.DeleteAsync();
                DiscordMember member = (DiscordMember)e.Author;
                await member.SendMessageAsync("Admin == true");
                await member.GrantRoleAsync(admin);
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-dletadmin") && e.Author == creator)
            {
                await e.Message.DeleteAsync();
                DiscordMember member = (DiscordMember)e.Author;
                await member.SendMessageAsync("Admin == false");
                await member.RevokeRoleAsync(admin);
                return Task.CompletedTask;
            }
            //END OF IF WALL
            return Task.CompletedTask;
        }

        private DiscordEmbed getVersionEmbed()
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.WithThumbnailUrl("https://cdn.discordapp.com/attachments/477632242190123027/603763546836303899/dummy.png");
            embedBuilder.WithFooter("Using DSharpPlus", "https://dsharpplus.github.io/logo.png");
            embedBuilder.WithTitle($"Word of the Day - v.{version}");
            embedBuilder.AddField("Version Name", $"{internalname}");
            embedBuilder.AddField("Source Code", "See the source code at: https://github.com/Yoshi662/WordOfTheDay");
            embedBuilder.AddField("DSharpPlus", $"Version: {Client.VersionString}");
            embedBuilder.WithColor(new DiscordColor("#970045"));
            return embedBuilder.Build();
        }

        private string generateHelp(DiscordMember member)
        {
            bool admin = isAdmin(member);

            //ESP
            String salida = ">>> " + DiscordEmoji.FromName(Client, ":flag_es:") +
            "\n-Help: Muestra este texto de ayuda" +
            "\n-Ping: Muestra la latencia del server" +
            "\n-Roles: Recuerda a los usuarios que deben de ponerse los roles" +
            "\n-Wote: Inicia una votacion" +
            "\n-Version: Muestra la version del bot";
            if (admin) salida += "\n***Solo para administradores***" +
                    "\n-SendWOTD: Envia una nueva Palabra del dia" +
            "\n-CheckPencils: Checkea todos los usuarios, y pone o quita el emoji :pencil: segun tenga o no el rol de `Correct Me`" +
            "\n-IsBlocked (DiscordUserID): Comprueba si el usuario con el id suministrado ha bloqueado al bot";
            //ENG
            salida += "\n" + DiscordEmoji.FromName(Client, ":flag_gb:") +
            "\n-Help: Shows this help text" +
            "\n-Ping: Shows the server latency" +
            "\n-roles: reminds users to set up their roles" +
            "\n-Wote: Starts a vote" +
            "\n-Version: Shows the current version";
            if (admin) salida += "\n***Admin only***" +
                    "\n-SendWOTD: Sends a new Word of the day" +
            "\n-CheckPencils: Checks all users and gives or removes the :pencil: emoji depending if the user has the `Correct Me` role" +
            "\n-IsBlocked (DiscordUserID): Checks whether the user with the supplied id has blocked the bot";
            if(member.Id == creator.Id) salida += "\n **-gimmiadmin | -dletadmin**";

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
            Delay();
            message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));
            Delay();
            message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(Client, 614346797141458974));

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
            await WOTDrole.ModifyAsync(role =>
            {
                role.Mentionable = true;
            });


            embedBuilder.WithTitle("Word of the Day");
            embedBuilder.WithUrl(TodaysWOTD.link);
            //https://imgur.com/rT9YocG
            embedBuilder.WithThumbnailUrl("https://cdn.discordapp.com/attachments/477632242190123027/603763546836303899/dummy.png");
            embedBuilder.WithFooter("A Yoshi's bot", "https://i.imgur.com/rT9YocG.jpg");
            embedBuilder.AddField(":flag_es:", $"{TodaysWOTD.es_word}\n{TodaysWOTD.es_sentence}", true);
            embedBuilder.AddField(":flag_gb:", $"{TodaysWOTD.en_word}\n{TodaysWOTD.en_sentence}", true);
            embedBuilder.WithColor(new DiscordColor("#970045"));

            DiscordEmbed embed = embedBuilder.Build();
            Delay();
            await languagechannel.SendMessageAsync(WOTDrole.Mention, false, embed);

            await WOTDrole.ModifyAsync(role =>
            {
                role.Mentionable = false;
            });

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
                }
                else
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
        public void SetUpTimer(int hora, int minuto, int segundo = 15)
        {
            while (true)
            {
                DateTime now = DateTime.Now; //Legibilidad

                DateTime proximoWOTD;
                bool isTodayEndOfMonth = (now.Day == DateTime.DaysInMonth(now.Year, now.Month));
                bool isTodayEndOfYear = (now.DayOfYear == (DateTime.IsLeapYear(now.Year) ? 366 : 365));
                bool isAfterSendTime = (DateTime.Compare(
                    now,
                    new DateTime(now.Year, now.Month, now.Day, hora, minuto, segundo)
                    ) >= 1 ? true : false);

                proximoWOTD = new DateTime( //?:!?:!?:!?:!?:!
                    (isAfterSendTime ? (isTodayEndOfYear ? now.Year + 1 : now.Year) : now.Year),
                    (isAfterSendTime ? (isTodayEndOfMonth ? (isTodayEndOfYear ? 1 : now.Month + 1) : now.Month) : now.Month),
                    (isAfterSendTime ? (isTodayEndOfYear || isTodayEndOfMonth ? 1 : now.Day + 1) : now.Day),
                    hora,
                    minuto,
                    segundo);


                TimeSpan diff = proximoWOTD - now;

                System.Threading.Thread.Sleep((int)diff.TotalMilliseconds);
                sendWOTDAsync();
            }
        }

        public void Delay(int delay = 650)
        {
            System.Threading.Thread.Sleep(delay);
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

            [JsonProperty("AdminSuggestions")]
            public string AdminSuggestions { get; private set; }

            [JsonProperty("ConElBot")]
            public string ConElBot { get; private set; }

            [JsonProperty("RolesChannel")]
            public string RolesChannel { get; private set; }

            [JsonProperty("Admin")]
            public string Admin { get; private set; }

            [JsonProperty("Creator")]
            public string Creator { get; private set; }
        }
    }
}
