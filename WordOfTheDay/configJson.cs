using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WordOfTheDay
{
	public class ConfigJson
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

		[JsonProperty("Introductions")]
		public string Introductions { get; private set; }

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

		[JsonProperty("SpanishNative")]
		public string SpanishNative { get; private set; }

		[JsonProperty("EnglishNative")]
		public string EnglishNative { get; private set; }

		[JsonProperty("OtherNative")]
		public string OtherNative { get; private set; }

		[JsonProperty("Admin")]
		public string Admin { get; private set; }

		[JsonProperty("Creator")]
		public string Yoshi { get; private set; }

	}
}
