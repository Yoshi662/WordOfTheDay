using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;

namespace WordOfTheDay
{
	public class LanguageServer
	{
		public DiscordGuild guild{ get; private set; }

		public DiscordChannel languagechannel{ get; private set; }
		public DiscordChannel adminSuggestions{ get; private set; }
		public DiscordChannel conelBot{ get; private set; }
		public DiscordChannel suggestions{ get; private set; }
		public DiscordChannel roles{ get; private set; }
		public DiscordChannel botupdates{ get; private set; }
		public DiscordChannel modlog{ get; private set; }
		public DiscordChannel usercount{ get; private set; }
		public DiscordChannel introductions{ get; private set; }

		public DiscordRole WOTDrole{ get; private set; }
		public DiscordRole CorrectMeRole{ get; private set; }
		public DiscordRole admin{ get; private set; }
		public DiscordRole onVC{ get; private set; }
		public DiscordRole StudyRole{ get; private set; }
		public DiscordRole EnglishNative{ get; private set; }
		public DiscordRole SpanishNative{ get; private set; }
		public DiscordRole OtherNative{ get; private set; }

		public DiscordUser yoshi{ get; private set; }

		public LanguageServer(DiscordClient Client, ConfigJson cfgjson)
		{
			guild = Client.GetGuildAsync(ulong.Parse(cfgjson.LanguageServer)).Result; //Server

			languagechannel = Client.GetChannelAsync(ulong.Parse(cfgjson.WOTDChannel)).Result; //Channel which recieves updates
			suggestions = Client.GetChannelAsync(ulong.Parse(cfgjson.Suggestions)).Result; //Channel which recieves updates
			adminSuggestions = Client.GetChannelAsync(ulong.Parse(cfgjson.AdminSuggestions)).Result;
			conelBot = Client.GetChannelAsync(ulong.Parse(cfgjson.ConElBot)).Result;
			modlog = Client.GetChannelAsync(ulong.Parse(cfgjson.ModLog)).Result;
			roles = Client.GetChannelAsync(ulong.Parse(cfgjson.RolesChannel)).Result; //Channel which users get their roles from.
			usercount = Client.GetChannelAsync(ulong.Parse(cfgjson.UserCountChannel)).Result;
			botupdates = Client.GetChannelAsync(ulong.Parse(cfgjson.BotUpdates)).Result;
			introductions = Client.GetChannelAsync(ulong.Parse(cfgjson.Introductions)).Result;

			EnglishNative = guild.GetRole(ulong.Parse(cfgjson.EnglishNative));
			SpanishNative = guild.GetRole(ulong.Parse(cfgjson.SpanishNative));
			OtherNative = guild.GetRole(ulong.Parse(cfgjson.OtherNative));
			WOTDrole = guild.GetRole(ulong.Parse(cfgjson.WOTDRole)); //WOTD role
			CorrectMeRole = guild.GetRole(ulong.Parse(cfgjson.CorrectMeRole)); //CorrectMe Role
			onVC = guild.GetRole(ulong.Parse(cfgjson.OnVC));
			StudyRole = guild.GetRole(ulong.Parse(cfgjson.StudyRole));
			admin = guild.GetRole(ulong.Parse(cfgjson.Admin));

			yoshi = Client.GetUserAsync(ulong.Parse(cfgjson.Yoshi)).Result;
		}
	}

}

