using DSharpPlus;
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
	public class WOTDCommands : BaseCommandModule
	{
		[Command("base"), RequireBotPermissions(Permissions.Administrator), RequireGuild(), Description("Example command for future use")]
		public async Task Base(CommandContext ctx)
		{
			//code goes here
		}
	}
}
