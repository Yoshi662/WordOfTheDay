using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Interactivity.Extensions;

namespace WordOfTheDay
{
    public class Program
    {
        public readonly string version = "1.7.1";
        public readonly string internalname = "Study Session Tracker";
        public DiscordClient Client { get; set; }
        private static Program prog;
        static CommandsNextExtension commands;

        private DiscordGuild languageServer;

        private DiscordChannel languagechannel;
        private DiscordChannel adminSuggestions;
        private DiscordChannel conelBot;
        private DiscordChannel suggestions;
        private DiscordChannel roles;
        private DiscordChannel botupdates;
        private DiscordChannel modlog;
        private DiscordChannel usercount;

        private DiscordRole WOTDrole;
        private DiscordRole CorrectMeRole;
        private DiscordRole admin;
        private DiscordRole onVC;
        private DiscordRole StudyRole;

        private DiscordUser yoshi;

        private Exception lastException;
        private DateTime lastExceptionDatetime;

        /// <summary>
        /// Key is the ID of the message
        /// Value is a Dictionary whose Key is the DiscordName of the Emoji Ex: ":eyes:"
        /// and the Value is the ID of the role
        /// </summary>
        public Dictionary<ulong, Dictionary<String, ulong>> ReactionRole;

        #region Main Logic
        public static void Main(string[] args)
        {
            prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync()
        {
            //Abrir JSON file
            string jsonConfString = "";
            using (FileStream fs = File.OpenRead("config.json"))
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                jsonConfString = await sr.ReadToEndAsync();

            ConfigJson cfgjson = JsonConvert.DeserializeObject<ConfigJson>(jsonConfString);
            DiscordConfiguration cfg = new DiscordConfiguration
            {
                Token = cfgjson.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug, //TODO
            };

            this.Client = new DiscordClient(cfg);

            this.Client.Ready += Client_Ready;
            this.Client.GuildAvailable += Client_GuildAvailable;
            this.Client.ClientErrored += Client_ClientError;
            this.Client.MessageCreated += Client_MessageCreated;
            this.Client.GuildMemberUpdated += Client_GuildMemberUpdated;
            this.Client.MessageReactionAdded += Client_MessageReactionAdded;
            this.Client.MessageReactionRemoved += Client_MessageReactionRemoved;
            this.Client.GuildMemberAdded += Client_GuildMemberAdded;
            this.Client.VoiceStateUpdated += Client_VoiceStateUpdated;


            await this.Client.ConnectAsync();

            ReactionRole = JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<String, ulong>>>(File.ReadAllText("RR.Json"));

            languagechannel = await Client.GetChannelAsync(ulong.Parse(cfgjson.WOTDChannel)); //Channel which recieves updates
            languageServer = await Client.GetGuildAsync(ulong.Parse(cfgjson.LanguageServer)); //Server
            WOTDrole = languageServer.GetRole(ulong.Parse(cfgjson.WOTDRole)); //WOTD role
            CorrectMeRole = languageServer.GetRole(ulong.Parse(cfgjson.CorrectMeRole)); //CorrectMe Role
            onVC = languageServer.GetRole(ulong.Parse(cfgjson.OnVC));
            suggestions = await Client.GetChannelAsync(ulong.Parse(cfgjson.Suggestions)); //Channel which recieves updates
            adminSuggestions = await Client.GetChannelAsync(ulong.Parse(cfgjson.AdminSuggestions));
            conelBot = await Client.GetChannelAsync(ulong.Parse(cfgjson.ConElBot));
            modlog = await Client.GetChannelAsync(ulong.Parse(cfgjson.ModLog));
            roles = await Client.GetChannelAsync(ulong.Parse(cfgjson.RolesChannel)); //Channel which users get their roles from.
            usercount = await Client.GetChannelAsync(ulong.Parse(cfgjson.UserCountChannel));
            botupdates = await Client.GetChannelAsync(ulong.Parse(cfgjson.BotUpdates));
            StudyRole = languageServer.GetRole(ulong.Parse(cfgjson.StudyRole));
            admin = languageServer.GetRole(ulong.Parse(cfgjson.Admin));
            yoshi = await Client.GetUserAsync(ulong.Parse(cfgjson.Yoshi));

            await Client.UpdateStatusAsync(new DiscordActivity("-help"), UserStatus.Online);

            if (!(lastException is null) && (lastExceptionDatetime != DateTime.MinValue))
            {
                //Programar sin dormir es !bien
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder
                    .WithTitle("Bot Breaking Exception")
                    .WithColor(new DiscordColor("#FF0000"))
                    .WithFooter(
                                "A Yoshi's Bot",
                                "https://i.imgur.com/rT9YocG.jpg"
                                );
                if (lastException.HelpLink != null) builder.WithUrl(lastException.HelpLink);
                if (lastException.StackTrace != null) builder.AddField("StackTrace", lastException.StackTrace);
                if (lastException.Message != null) builder.AddField("Mensaje", lastException.Message);
                if (lastException.InnerException != null)
                {
                    builder.AddField("**INNER EXCEPTION**", "**——————————————————————————————————————**");
                    if (lastException.InnerException.HelpLink != null) builder.WithUrl(lastException.InnerException.HelpLink);
                    if (lastException.InnerException.Message != null) builder.AddField("Mensaje", lastException.InnerException.Message);
                }
                if (lastExceptionDatetime != null) { builder.WithTimestamp(lastExceptionDatetime); };



                await botupdates.SendFileAsync(
                    lastExceptionDatetime.ToString("s") + "WOTD_EX_StackTrace.txt",
                    new MemoryStream(Encoding.UTF8.GetBytes(lastException.InnerException.StackTrace)),
                        "**Bot Breaking Exception**  -  " + lastExceptionDatetime.ToString("yyyy-mm-dd HH_mm_ss") + " - " + yoshi.Mention,
                    false,
                    builder.Build()
                    );
                Delay();
            }
            this.Client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes(2)
            });

            commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "-" },
                EnableDefaultHelp = false,
                DmHelp = false,
                IgnoreExtraArguments = true
            });

            commands.RegisterCommands<StudyCommands>();

            commands.CommandExecuted += this.Commands_CommandExecuted;
            commands.CommandErrored += this.Commands_CommandErrored;

            Thread WOTD = new Thread(() => SetUpTimer(14, 00));
            WOTD.Start();

            await Task.Delay(-1);
        }

        private Task Client_VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
        {
            //TODO test this
            DiscordUser user = e.User;
            bool isuserconnected = false;
            try
            {
                isuserconnected = e.Channel.Users.Contains(user);
            }
            catch (NullReferenceException)
            {
                isuserconnected = false;
            }

            try
            {
                DiscordMember member = (DiscordMember)e.User; //a veces esto peta y no se por que
                if (isuserconnected)
                {
                    member.GrantRoleAsync(onVC);
                }
                else
                {
                    member.RevokeRoleAsync(onVC);
                }
            }
            catch (Exception)
            {

            }

            return Task.CompletedTask;
        }

        private Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
        {
            DiscordMember miembro = e.Member;
            modlog.SendMessageAsync(null, false, HelperMethods.QuickEmbed($"New Member: {miembro.Username}#{miembro.Discriminator}",
                $"Discord ID: {miembro.Id}\n" +
                $"Fecha de creacion de cuenta: {miembro.CreationTimestamp}",
                false
                ), null);
            return Task.CompletedTask;
        }


        #endregion

        #region events
        private Task Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs e)
        {
            CheckPencil(e.Member);
            return Task.CompletedTask;
        }

        private async Task<Task> Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            //Esto es horrible pero bueno
            string mensaje = e.Message.Content.ToLower();

            if (mensaje.StartsWith("-wote") ||
                mensaje.StartsWith("wote") ||
                e.Message.ChannelId == suggestions.Id ||
                e.Message.ChannelId == adminSuggestions.Id)
            {
                await WoteAsync(e.Message, true);
            }

            if (!mensaje.StartsWith("-")) return Task.CompletedTask; //OPTIMIZAAAAAAR    

            string[] content = mensaje.Trim().Split(' ');

            if (mensaje.StartsWith("-roles"))
            {
                await e.Channel.SendMessageAsync(
                     $"{DiscordEmoji.FromName(Client, ":flag_es:")} Por favor ponte los roles adecuados en {roles.Mention} ¡No te olvides el rol de nativo!\n" +
                     $"{DiscordEmoji.FromName(Client, ":flag_gb:")} Please set up your roles in {roles.Mention} Don't forget the native role!"
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
                await member.SendMessageAsync(GenerateHelp(member));

                await e.Message.RespondAsync(
                    DiscordEmoji.FromName(Client, ":flag_es:") + " Ayuda Enviada por mensaje privado\n"
                  + DiscordEmoji.FromName(Client, ":flag_gb:") + " Help sent via direct message");
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-sendwotd") && IsAdmin(e.Author))
            {
                bool confirmacion = await VerificarAsync(e.Channel, e.Author, 15);
                if (confirmacion)
                {
                    await SendWOTDAsync();
                }
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-version"))
            {
                await e.Channel.SendMessageAsync("", false, GetVersionEmbed());
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-checkpencils") && IsAdmin(e.Author))
            {
                IEnumerable<DiscordUser> users = languagechannel.Users;
                foreach (DiscordUser user in users)
                {
                    CheckPencil((DiscordMember)user);
                }
                await e.Channel.SendMessageAsync("Checking Users...");
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-isblocked") && IsAdmin(e.Author))
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

            if (mensaje.StartsWith("-gimmiadmin") && e.Author == yoshi)
            {
                await e.Message.DeleteAsync();
                DiscordMember member = (DiscordMember)e.Author;
                await member.SendMessageAsync("Admin == true");
                await member.GrantRoleAsync(admin);
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-dletadmin") && e.Author == yoshi)
            {
                await e.Message.DeleteAsync();
                DiscordMember member = (DiscordMember)e.Author;
                await member.SendMessageAsync("Admin == false");
                await member.RevokeRoleAsync(admin);
                return Task.CompletedTask;
            }

            if (mensaje.StartsWith("-restart") && IsAdmin(e.Author))
            {
                bool confirmacion = await VerificarAsync(e.Channel, e.Author, 15);
                if (confirmacion)
                {
                    await e.Channel.SendMessageAsync(EasyDualLanguageFormatting("Reiniciando Bot, por favor espere", "Restarting Bot, please wait"));
                    Delay(1500);
                    Environment.Exit(0);
                }
                else
                {
                    await e.Message.DeleteAsync();
                }
            }

            if (mensaje.StartsWith("-removereactions") && IsAdmin(e.Author))
            {
                ulong channelid = 0;
                ulong mensajeid = 0;
                if (ulong.TryParse(mensaje.Split(' ')[1], out channelid) && ulong.TryParse(mensaje.Split(' ')[2], out mensajeid))
                {
                    DiscordMessage DisMensaje = await languageServer.GetChannel(channelid).GetMessageAsync(mensajeid);
                    await DisMensaje.DeleteAllReactionsAsync();
                    await e.Channel.SendMessageAsync(EasyDualLanguageFormatting("Se han borrado las reacciones", "The reactions have been removed"));
                }
                else
                {
                    await e.Channel.SendMessageAsync(EasyDualLanguageFormatting("No se ha podido encontrar el mensaje, por favor comprueba las IDs", "The message couldn't be found. Please check the IDs"));
                }

            }

            if (mensaje.StartsWith("-embed") && IsAdmin(e.Author))
            {

                String message = e.Message.Content.Substring(6);
                DiscordMember member = (DiscordMember)e.Author;
                bool hasfile = e.Message.Attachments.Count > 0;
                bool isfilenamevaild = false;
                String filecontent = "";

                if (hasfile)
                {
                    DiscordAttachment file = e.Message.Attachments[0];

                    isfilenamevaild = file.FileName.Equals("message.txt");
                    string FileName = DateTime.Now.Ticks.ToString("X16") + @".txt"; //https://stackoverflow.com/questions/7874111/convert-datetime-now-to-a-valid-windows-filename => I hate the world

                    using (var client = new WebClient())
                    {
                        client.DownloadFile(file.Url, FileName);
                    }
                    filecontent = File.ReadAllText(FileName);

                    File.Delete(FileName);
                }

                Delay(350);


                await e.Message.DeleteAsync();
                try
                {
                    if (!hasfile)
                    {
                        await e.Channel.SendMessageAsync(null, false, HelperMethods.QuickEmbed($"Embed de {member.Nickname ?? member.DisplayName: member.Nickname}", message, false));
                    }
                    else if (hasfile && isfilenamevaild)
                    {
                        await e.Channel.SendMessageAsync(null, false, HelperMethods.QuickEmbed($"Embed de {member.Nickname ?? member.DisplayName: member.Nickname}", filecontent, false));
                    }
                }
                catch (Exception ex)
                {
                    await botupdates.SendMessageAsync("Excepcion con un Embed", false, GenerateErrorEmbed(ex));
                    DiscordMessage errorembed = await e.Channel.SendMessageAsync(null, false, HelperMethods.QuickEmbed(":warning: Error :warning:",
                         EasyDualLanguageFormatting("Mensaje demasiado largo o contiene caracteres no validos", "Message too large or has invalid characters"), false, "#FF0000"));
                    Delay(5000);
                    await errorembed.DeleteAsync();
                }

                return Task.CompletedTask;
            }
            if (mensaje.StartsWith("-addemoji") && IsAdmin(e.Author))
            {

                String[] imageformats = { "png", "jpg", "gif", "WebP" };

                if (content.Length >= 2 &&
                    Uri.IsWellFormedUriString(content[1], UriKind.Absolute) &&
                    imageformats.Contains(content[1].Split('.').Last().Substring(0, 3)))
                {
                    using (WebClient wc = new WebClient())
                    {
                        string currentformat = content[1].Split('.').Last().Substring(0, 3);
                        string filepath = DateTime.Now.Ticks.ToString("X16") + "." + currentformat;
                        wc.DownloadFile(content[1], filepath);
                        DiscordGuildEmoji guildEmoji = await e.Guild.CreateEmojiAsync(content[2], File.OpenRead(filepath));
                        await e.Channel.SendMessageAsync(guildEmoji.ToString());
                        File.Delete(filepath);
                    }
                }
                else
                {
                    await e.Message.DeleteAsync();
                    await e.Channel.SendMessageAsync(null, false, HelperMethods.QuickEmbed("No se puedo añadir el emoji", "Puede que no tenga el formato correcto[png, jpg, gif, WebP]\nUso:-Addemoji<URL> < Nombre_emoji >", false, "#ff0000"));
                }
            }

            if (mensaje.StartsWith("-usercount") && IsAdmin(e.Author))
            {
                UpdateUserCountChannel();
            }


            //END OF IF WALL
            return Task.CompletedTask;
        }

        private Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            sender.Logger.LogInformation("Client is ready to process events.");
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.LogInformation($"Guild available: { e.Guild.Name}");
            return Task.CompletedTask;
        }

        private Task Client_ClientError(DiscordClient sender, ClientErrorEventArgs e)
        {
            sender.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");
            lastException = e.Exception;
            lastExceptionDatetime = DateTime.Now;
            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            // let's log the name of the command and user
            sender.Client.Logger.LogInformation($"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'");

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {

            // let's log the error details
            sender.Client.Logger.LogError($"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            // let's check if the error is a result of lack
            // of required permissions
            if (e.Exception is ChecksFailedException ex)
            {
                // yes, the user lacks required permissions, 
                // let them know

                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync("", embed: embed);
            }
        }

        private Task Client_MessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            if (e.Channel != roles) { return Task.CompletedTask; }
            String emojiname = e.Emoji.GetDiscordName();

            if (ReactionRole.ContainsKey(e.Message.Id))
            {
                Dictionary<string, ulong> auxDict = ReactionRole[e.Message.Id];
                DiscordMember member = (DiscordMember)e.User;
                member.GrantRoleAsync(languageServer.GetRole(auxDict[emojiname]));
            }
            return Task.CompletedTask;
        }

        private Task Client_MessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
        {
            if (e.Channel != roles) { return Task.CompletedTask; }
            String emojiname = e.Emoji.GetDiscordName();

            if (ReactionRole.ContainsKey(e.Message.Id))
            {
                Dictionary<string, ulong> auxDict = ReactionRole[e.Message.Id];
                DiscordMember member = (DiscordMember)e.User;
                member.RevokeRoleAsync(languageServer.GetRole(auxDict[emojiname]));
            }
            return Task.CompletedTask;
        }



        #endregion

        #region main methods
        public async Task<Task> SendWOTDAsync()
        {
            WordOfTheDay TodaysWOTD = Logic.GetXMLWOTD();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

            embedBuilder.WithTitle("Word of the Day");
            embedBuilder.WithUrl(TodaysWOTD.link);
            //https://imgur.com/rT9YocG
            embedBuilder.WithThumbnail("https://cdn.discordapp.com/attachments/477632242190123027/603763546836303899/dummy.png");
            embedBuilder.WithFooter("A Yoshi's bot", "https://i.imgur.com/rT9YocG.jpg");
            embedBuilder.AddField(":flag_es: - " + TodaysWOTD.es_word, $"{TodaysWOTD.es_sentence}", true);
            embedBuilder.AddField(":flag_gb: - " + TodaysWOTD.en_word, $"{TodaysWOTD.en_sentence}", true);
            embedBuilder.WithColor(new DiscordColor("#970045"));

            DiscordEmbed embed = embedBuilder.Build();
            await languagechannel.SendMessageAsync(WOTDrole.Mention, false, embed);

            return Task.CompletedTask;
        }

        private Task WoteAsync(DiscordMessage message, bool dunno = false)
        {
            message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":white_check_mark:"));
            Delay();
            message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));
            if (dunno)
            {
                Delay();

                message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(Client, 614346797141458974));
            }
            return Task.CompletedTask;
        }
        private void CheckPencil(DiscordMember member)
        {
            bool endsOnPencil = member.DisplayName.EndsWith(DiscordEmoji.FromName(Client, ":pencil:"));

            bool hasrole = false;

            if (member.Roles.Contains(CorrectMeRole)) hasrole = true;

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
                    ) >= 1);

                proximoWOTD = new DateTime( //?:!?:!?:!?:!?:!
                    (isAfterSendTime ? (isTodayEndOfYear ? now.Year + 1 : now.Year) : now.Year),
                    (isAfterSendTime ? (isTodayEndOfMonth ? (isTodayEndOfYear ? 1 : now.Month + 1) : now.Month) : now.Month),
                    (isAfterSendTime ? (isTodayEndOfYear || isTodayEndOfMonth ? 1 : now.Day + 1) : now.Day),
                    hora,
                    minuto,
                    segundo);


                TimeSpan diff = proximoWOTD - now;

                Delay((int)diff.TotalMilliseconds);
                UpdateUserCountChannel();
                SendWOTDAsync();
            }
        }

        private void UpdateUserCountChannel()
        {
            if (usercount.Name != "User Count: " + languageServer.MemberCount)
            {
                usercount.ModifyAsync(ch =>
                {
                    ch.Name = "User Count: " + languageServer.MemberCount;
                });
            }
        }


        #endregion

        #region helper methods
        public static void Delay(int delay = 650)
        {
            System.Threading.Thread.Sleep(delay);
        }
        private DiscordEmbed GetVersionEmbed()
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.WithThumbnail("https://cdn.discordapp.com/attachments/477632242190123027/603763546836303899/dummy.png");
            embedBuilder.WithFooter("Using DSharpPlus", "https://dsharpplus.github.io/logo.png");
            embedBuilder.WithTitle($"Word of the Day - v.{version}");
            embedBuilder.AddField("Version Name", $"{internalname}");
            embedBuilder.AddField("Source Code", "See the source code at: https://github.com/Yoshi662/WordOfTheDay");
            embedBuilder.AddField("DSharpPlus", $"Version: {Client.VersionString}");
            embedBuilder.WithColor(new DiscordColor("#970045"));
            return embedBuilder.Build();
        }
        private DiscordEmbed GenerateErrorEmbed(Exception ex)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder
                .WithTitle("Algo Paso")
                .WithColor(new DiscordColor("#FF0000"))
                .WithFooter(
                            "A Yoshi's Bot",
                            "https://i.imgur.com/rT9YocG.jpg"
                            );
            if (ex.HelpLink != null) builder.WithUrl(ex.HelpLink);
            if (ex.StackTrace != null) builder.AddField("StackTrace", ex.StackTrace);
            if (ex.Message != null) builder.AddField("Mensaje", ex.Message);
            if (ex.InnerException != null)
            {
                builder.AddField("**INNER EXCEPTION**", "**——————————————————————————————————————**");
                if (ex.InnerException.HelpLink != null) builder.WithUrl(ex.InnerException.HelpLink);
                if (ex.InnerException.Message != null) builder.AddField("Mensaje", ex.InnerException.Message);
            }
            builder.WithTimestamp(DateTime.Now);
            return builder.Build();
        }

        private string GenerateHelp(DiscordMember member)
        {
            bool admin = IsAdmin(member);
            bool study = member.Roles.Contains(StudyRole);

            //ESP
            String salida = ">>> " + DiscordEmoji.FromName(Client, ":flag_es:") +
            "\n-Help: Muestra este texto de ayuda" +
            "\n-Ping: Muestra la latencia del server" +
            "\n-Roles: Recuerda a los usuarios que deben de ponerse los roles" +
            "\n-Wote: Inicia una votacion" +
            "\n-Version: Muestra la version del bot";
            if (study) salida +=
                     "\n-***Study Session Tracker***" +
                     "\n-Study *<Asignatura>*: Empieza una sesion de estudio" +
                     "\n-AddHours *<Horas> <Asignatura>*: Añade horas a tu perfil" +
                     "\n-GetHours: Obtiene tu tiempo estudiado total" +
                     "\n-Ranking: Muestra los 5 mejores estudiantes";
            if (admin) salida += "\n***Solo para administradores***" +
                    "\n-SendWOTD: Envia una nueva Palabra del dia" +
            "\n-CheckPencils: Checkea todos los usuarios, y pone o quita el emoji :pencil: segun tenga o no el rol de `Correct Me`" +
            "\n-RemoveReactions <Channel ID> <Message ID>: Borra todas las reacciones de un mensaje" +
            "\n-Embed: Transforma el mensaje enviado en un embed" +
            "\n-UserCount: Actualiza el canal User count" +
            "\n-AddEmoji: Añade un emoji al servidor" +
             "\n-IsBlocked (DiscordUserID): Comprueba si el usuario con el id suministrado ha bloqueado al bot";
            //ENG
            salida += "\n" + DiscordEmoji.FromName(Client, ":flag_gb:") +
            "\n-Help: Shows this help text" +
            "\n-Ping: Shows the server latency" +
            "\n-roles: reminds users to set up their roles" +
            "\n-Wote: Starts a vote" +
            "\n-Version: Shows the current version";
            if (study) salida +=
                 "\n-***Study Session Tracker***" +
                 "\n-Study *<Subject>*: Starts a study session" +
                 "\n-AddHours *<Hours> <subject>*: Adds hours to your profile" +
                 "\n-GetHours: Get your total time studied" +
                 "\n-Ranking: Shows the top 5 students ";
            if (admin) salida += "\n***Admin only***" +
                    "\n-SendWOTD: Sends a new Word of the day" +
            "\n-CheckPencils: Checks all users and gives or removes the :pencil: emoji depending if the user has the `Correct Me` role" +
            "\n-RemoveReactions <Channel ID> <Message ID>: Removes all the reactions from a message" +
            "\n-Embed: Converts the message sent into an embed" +
            "\n-UserCount: Updates the User count channel" +
            "\n-AddEmoji: Adds an emoji to the server" +
            "\n-IsBlocked (DiscordUserID): Checks whether the user with the supplied id has blocked the bot";
            if (member.Id == yoshi.Id) salida += "\n `-gimmiadmin, -dletadmin, -exec, -execq`";

            return salida;
        }
        private bool IsAdmin(DiscordMember member)
        {
            if (member.Roles.Contains(admin)) return true;
            else return false;
        }
        private bool IsAdmin(DiscordUser user)
        {                               //heh
            return IsAdmin((DiscordMember)user);
        }

        private async Task<bool> VerificarAsync(DiscordChannel channel, DiscordUser autor, int time = 30)
        {
            DiscordMessage verificacion = await channel.SendMessageAsync(EasyDualLanguageFormatting("¿Estas seguro?", "Are you sure?"));
            await WoteAsync(verificacion, false);
            Delay(1000);
            for (int i = 0; i < time; i++)
            {
                IReadOnlyList<DiscordUser> reaccionesOK = await verificacion.GetReactionsAsync(DiscordEmoji.FromName(Client, ":white_check_mark:"));
                IReadOnlyList<DiscordUser> reaccionesNOPE = await verificacion.GetReactionsAsync(DiscordEmoji.FromName(Client, ":x:"));
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
                Delay(1000);
            }

            await verificacion.DeleteAsync();
            return false;
        }

        private string EasyDualLanguageFormatting(string EScontent, string ENcontent)
        {
            return $":flag_es: {EScontent}\n:flag_gb: {ENcontent}";
        }

        private bool HasInternetConnection()
        {
            Ping sender = new Ping();
            PingReply respuesta = sender.Send("discordapp.com");
            return respuesta.Status.HasFlag(IPStatus.Success);
        }

        #endregion

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

            [JsonProperty("OnVC")]
            public string OnVC { get; private set; }

            [JsonProperty("CorrectMeRole")]
            public string CorrectMeRole { get; private set; }

            [JsonProperty("Suggestions")]
            public string Suggestions { get; private set; }

            [JsonProperty("AdminSuggestions")]
            public string AdminSuggestions { get; private set; }

            [JsonProperty("ConElBot")]
            public string ConElBot { get; private set; }

            [JsonProperty("ModLog")]
            public string ModLog { get; private set; }

            [JsonProperty("BotUpdates")]
            public string BotUpdates { get; private set; }

            [JsonProperty("RolesChannel")]
            public string RolesChannel { get; private set; }

            [JsonProperty("UserCountChannel")]
            public string UserCountChannel { get; private set; }

            [JsonProperty("StudyRole")]
            public string StudyRole { get; private set; }

            [JsonProperty("Admin")]
            public string Admin { get; private set; }

            [JsonProperty("Creator")]
            public string Yoshi { get; private set; }
        }
    }
}
