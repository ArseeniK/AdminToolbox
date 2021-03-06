using Smod2;
using Smod2.Attributes;
using Smod2.Events;
using Smod2.EventHandlers;
using Smod2.API;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace AdminToolbox
{
	[PluginDetails(
		author = "Evan (AKA Rnen)",
		name = "Admin Toolbox",
		description = "Plugin for advanced admin tools",
		id = "rnen.admin.toolbox",
		version = "1.3.5",
		SmodMajor = 3,
		SmodMinor = 1,
		SmodRevision = 13
		)]
	class AdminToolbox : Plugin
	{
		internal static bool isRoundFinished = false, lockRound = false, isColored = false, isColoredCommand = false, intercomLock = false, intercomLockChanged = false;
		public static Dictionary<string, AdminToolboxPlayerSettings> playerdict = new Dictionary<string, AdminToolboxPlayerSettings>();
		public static Dictionary<string, Vector> warpVectors = new Dictionary<string, Vector>();
		public static Dictionary<string, Vector> presetWarps { get; private set; } = new Dictionary<string, Vector>()
			{
				{ "mtf",new Vector(181,994,-61) },
				{ "grass",new Vector(237,999,17) },
				{ "ci",new Vector(10,989,-60) },
				{ "jail",new Vector(53,1020,-44) },
				{ "flat",new Vector(250,980,110) },
				{ "heli",new Vector(293,977,-62) },
				{ "car",new Vector(-96,987,-59) },
				{ "topsitedoor",new Vector(89,989,-69)},
				{ "escape", new Vector(179,996,27) }
			};

		public static int roundCount { get; internal set; } = 1;
		public static LogHandlers AdminToolboxLogger { get; internal set; } = new LogHandlers();
		public static string _roundStartTime;

		public static AdminToolbox plugin;

		public class AdminToolboxPlayerSettings
		{
			public bool spectatorOnly = false,
				godMode = false,
				dmgOff = false,
				destroyDoor = false,
				keepSettings = false,
				lockDown = false,
				instantKill = false,
				isJailed = false,
				isInJail = false;
			public int Kills = 0,
				TeamKills = 0,
				Deaths = 0,
				RoundsPlayed = 0,
				previousHealth = 100,
				prevAmmo5 = 0,
				prevAmmo7 = 0,
				prevAmmo9 = 0;
			public Vector DeathPos = Vector.Zero,
				originalPos = Vector.Zero;
			public Role previousRole = Role.CLASSD;
			public List<Smod2.API.Item> playerPrevInv = new List<Smod2.API.Item>();
			public DateTime JailedToTime = DateTime.Now, joinTime = DateTime.Now;
			public double minutesPlayed = 1;
		}

		public override void OnDisable()
		{
			if (isColored)
				this.Info(this.Details.name + " v." + this.Details.version + " - @#fg=Red;Disabled@#fg=Default;");
			else
				this.Info(this.Details.name + " v." + this.Details.version + " - Disabled");
		}

		public override void OnEnable()
		{
			plugin = this;
			WriteVersionToFile();
			//CheckCurrVersion(this, this.Details.version);
			if (isColored)
				this.Info(this.Details.name + " v." + this.Details.version + " - @#fg=Green;Enabled@#fg=Default;");
			else
				this.Info(this.Details.name + " v." + this.Details.version + " - Enabled");
			_roundStartTime = DateTime.Now.Year.ToString() + "-" + ((DateTime.Now.Month >= 10) ? DateTime.Now.Month.ToString() : ("0" + DateTime.Now.Month.ToString())) + "-" + ((DateTime.Now.Day >= 10) ? DateTime.Now.Day.ToString() : ("0" + DateTime.Now.Day.ToString())) + " " + ((DateTime.Now.Hour >= 10) ? DateTime.Now.Hour.ToString() : ("0" + DateTime.Now.Hour.ToString())) + "." + ((DateTime.Now.Minute >= 10) ? DateTime.Now.Minute.ToString() : ("0" + DateTime.Now.Minute.ToString())) + "." + ((DateTime.Now.Second >= 10) ? DateTime.Now.Second.ToString() : ("0" + DateTime.Now.Second.ToString()));
			warpVectors = new Dictionary<string, Vector>(presetWarps);
			AdminToolbox.WriteToLog(new string[] { "\"Plugin Started\"" }, LogHandlers.ServerLogType.Misc);
		}

		public override void Register()
		{
			#region EventHandlers Registering Eventhandlers
			// Register Events
			this.AddEventHandlers(new RoundEventHandler(this), Priority.Normal);
			this.AddEventHandler(typeof(IEventHandlerPlayerHurt), new DamageDetect(this), Priority.Normal);
			this.AddEventHandler(typeof(IEventHandlerPlayerDie), new DieDetect(this), Priority.Normal);
			this.AddEventHandlers(new MyMiscEvents(this), Priority.Normal);
			#endregion
			#region Commands Registering Commands
			// Register Commands
			this.AddCommands(new string[] { "spec", "spectator" }, new Command.SpectatorCommand());
			this.AddCommands(new string[] { "p", "player" }, new Command.PlayerCommand());
			this.AddCommands(new string[] { "players", "playerlist", "plist" }, new Command.PlayerListCommand());
			this.AddCommands(new string[] { "atheal", "at-heal" }, new Command.HealCommand());
			this.AddCommands(new string[] { "atgod", "atgodmode", "at-god", "at-godmode" }, new Command.GodModeCommand());
			this.AddCommand("nodmg", new Command.NoDmgCommand());
			this.AddCommands(new string[] { "tut", "tutorial" }, new Command.TutorialCommand());
			this.AddCommand("role", new Command.RoleCommand());
			this.AddCommands(new string[] { "keep", "keepsettings" }, new Command.KeepSettingsCommand());
			this.AddCommands(new string[] { "athp", "atsethp", "at-hp", "at-sethp" }, new Command.SetHpCommand());
			this.AddCommand("pos", new Command.PosCommand());
			this.AddCommand("tpx", new Command.TeleportCommand());
			this.AddCommand("warp", new Command.WarpCommmand());
			this.AddCommand("warps", new Command.WarpsCommmand());
			this.AddCommands(new string[] { "roundlock", "lockround", "rlock", "lockr" }, new Command.RoundLockCommand(this));
			this.AddCommands(new string[] { "breakdoor", "bd", "breakdoors" }, new Command.BreakDoorsCommand());
			this.AddCommands(new string[] { "pl", "playerlockdown", "plock", "playerlock" }, new Command.LockdownCommand());
			this.AddCommand("atcolor", new Command.ATColorCommand(this));
			this.AddCommand("atdisable", new Command.ATDisableCommand(this));
			this.AddCommands(new string[] { "ik", "instakill", "instantkill" }, new Command.InstantKillCommand());
			this.AddCommands(new string[] { "j", "jail" }, new Command.JailCommand());
			this.AddCommands(new string[] { "il", "ilock", "INTERLOCK", "intercomlock" }, new Command.IntercomLockCommand(this));
			this.AddCommands(new string[] { "s", "server", "serverinfo" }, new Command.ServerCommand());
			this.AddCommands(new string[] { "e", "empty" }, new Command.EmptyCommand());
			this.AddCommands(new string[] { "atban","offlineban","oban" }, new Command.ATBanCommand(this));
			this.AddCommands(new string[] { "kill", "slay" }, new Command.KillCommand(this));

			#endregion
			#region Config Registering Config Entries
			// Register config settings
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_enable", true, Smod2.Config.SettingType.BOOL, true, "Enable/Disable AdminToolbox"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_colors", false, Smod2.Config.SettingType.BOOL, true, "Enable/Disable AdminToolbox colors in server window"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_tracking", true, Smod2.Config.SettingType.BOOL, true, "Appends the AdminToolbox version to your server name, this is for tracking how many servers are running the plugin"));

			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_tutorial_dmg_allowed", new int[] { -1 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "What (int)damagetypes TUTORIAL is allowed to take"));

			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_debug_damagetypes", new int[] { 5, 13, 14, 15, 16, 17 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "What (int)damagetypes to debug"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_debug_server", false, Smod2.Config.SettingType.BOOL, true, "Debugs damage dealt by server"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_debug_spectator", false, Smod2.Config.SettingType.BOOL, true, "Debugs damage done to/by spectators"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_debug_tutorial", false, Smod2.Config.SettingType.BOOL, true, "Debugs damage done to tutorial"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_debug_player_damage", false, Smod2.Config.SettingType.BOOL, true, "Debugs damage to all players except teammates"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_debug_friendly_damage", false, Smod2.Config.SettingType.BOOL, true, "Debugs damage to teammates"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_debug_player_kill", false, Smod2.Config.SettingType.BOOL, true, "Debugs player kills except teamkills"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_debug_friendly_kill", true, Smod2.Config.SettingType.BOOL, true, "Debugs team-kills"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_debug_scp_and_self_killed", false, Smod2.Config.SettingType.BOOL, true, "Debug suicides and SCP kills"));

			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_endedRound_damagemultiplier", 1f, Smod2.Config.SettingType.FLOAT, true, "Damage multiplier after end of round"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_round_damagemultiplier", 1f, Smod2.Config.SettingType.FLOAT, true, "Damage multiplier"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_decontamination_damagemultiplier", 1f, Smod2.Config.SettingType.FLOAT, true, "Damage multiplier for the decontamination of LCZ"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_friendlyfire_damagemultiplier", 1f, Smod2.Config.SettingType.FLOAT, true, "Damage multiplier for friendly fire"));

			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_custom_nuke_cards", false, Smod2.Config.SettingType.BOOL, true, "Enables the use of custom keycards for the activation of the nuke"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_nuke_card_list", new int[] { 6, 9, 11 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "List of all cards that can enable the nuke"));

			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_log_teamkills", false, Smod2.Config.SettingType.BOOL, true, "Writing logfiles for teamkills"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_log_kills", false, Smod2.Config.SettingType.BOOL, true, "Writing logfiles for regular kills"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_log_commands", false, Smod2.Config.SettingType.BOOL, true, "Writing logfiles for all AT command usage"));

			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_round_info", true, Smod2.Config.SettingType.BOOL, true, "Prints round count and player number on round start & end"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_player_join_info", true, Smod2.Config.SettingType.BOOL, true, "Writes player name in console on players joining"));

			//this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_intercom_whitelist", new string[] { string.Empty }, Smod2.Config.SettingType.LIST, true, "What ServerRank can use the Intercom to your specified settings"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_intercom_steamid_blacklist", new string[] { string.Empty }, Smod2.Config.SettingType.LIST, true, "Blacklist of steamID's that cannot use the intercom"));
			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_intercomlock", false, Smod2.Config.SettingType.BOOL, true, "If set to true, locks the command for all non-whitelist players"));

			this.AddConfig(new Smod2.Config.ConfigSetting("admintoolbox_block_role_damage", new string[] { string.Empty }, Smod2.Config.SettingType.LIST, true, "What roles cannot attack other roles"));
			#endregion
		}

		public static void AddMissingPlayerVariables(List<Player> players = null)
		{
			if (PluginManager.Manager.Server.GetPlayers().Count == 0) return;
			else if (players != null && players.Count > 0)
				foreach (Player player in players)
					AdminToolbox.AddToPlayerDict(player);
			else
				foreach (Player player in PluginManager.Manager.Server.GetPlayers())
					AdminToolbox.AddToPlayerDict(player);
		}
		private static void AddToPlayerDict(Player player)
		{
			if (player.SteamId != null && player.SteamId != string.Empty)
				if (!playerdict.ContainsKey(player.SteamId))
					playerdict.Add(player.SteamId, new AdminToolboxPlayerSettings());
		}

		public static List<Player> GetJailedPlayers(string filter = "")
		{
			List<Player> myPlayers = new List<Player>();
			if (PluginManager.Manager.Server.GetPlayers().Count > 0)
				if (filter != string.Empty)
					foreach (Player pl in PluginManager.Manager.Server.GetPlayers(filter))
					{
						if (AdminToolbox.playerdict.ContainsKey(pl.SteamId) && AdminToolbox.playerdict[pl.SteamId].isJailed)
							myPlayers.Add(pl);
					}
				else
					foreach (Player pl in PluginManager.Manager.Server.GetPlayers())
						if (AdminToolbox.playerdict.ContainsKey(pl.SteamId) && AdminToolbox.playerdict[pl.SteamId].isJailed)
							myPlayers.Add(pl);
			return myPlayers;
		}
		public static void CheckJailedPlayers(Player myPlayer = null)
		{
			bool isInsideJail(Player pl)
			{
				float x = System.Math.Abs(pl.GetPosition().x - presetWarps["jail"].x), y = System.Math.Abs(pl.GetPosition().y - presetWarps["jail"].y), z = System.Math.Abs(pl.GetPosition().z - presetWarps["jail"].z);
				if (x > 7 || y > 5 || z > 7) return false;
				else return true;
			}
			if (myPlayer != null)
			{
				if (!AdminToolbox.playerdict.ContainsKey(myPlayer.SteamId)) { AddMissingPlayerVariables(new List<Player> { myPlayer }); return; }
				AdminToolbox.playerdict[myPlayer.SteamId].isInJail = isInsideJail(myPlayer);
				if (!AdminToolbox.playerdict[myPlayer.SteamId].isInJail) SendToJail(myPlayer);
				else if (AdminToolbox.playerdict[myPlayer.SteamId].JailedToTime <= DateTime.Now) ReturnFromJail(myPlayer);
			}
			else
				foreach (Player pl in GetJailedPlayers())
				{
					if(!AdminToolbox.playerdict.ContainsKey(pl.SteamId)) { AddMissingPlayerVariables(new List<Player> { pl }); continue; }
					AdminToolbox.playerdict[pl.SteamId].isInJail = isInsideJail(pl);
					if (!AdminToolbox.playerdict[pl.SteamId].isInJail) SendToJail(pl);
					else if (AdminToolbox.playerdict[pl.SteamId].JailedToTime <= DateTime.Now) ReturnFromJail(pl);
				}
		}

		public static void SendToJail(Player ply)
		{
			if (AdminToolbox.playerdict.ContainsKey(ply.SteamId))
			{
				//Saves original variables
				AdminToolbox.playerdict[ply.SteamId].originalPos = ply.GetPosition();
				if (!AdminToolbox.playerdict[ply.SteamId].isJailed)
				{
					AdminToolbox.playerdict[ply.SteamId].previousRole = ply.TeamRole.Role;
					AdminToolbox.playerdict[ply.SteamId].playerPrevInv = ply.GetInventory();
					AdminToolbox.playerdict[ply.SteamId].previousHealth = ply.GetHealth();
					AdminToolbox.playerdict[ply.SteamId].prevAmmo5 = ply.GetAmmo(AmmoType.DROPPED_5);
					AdminToolbox.playerdict[ply.SteamId].prevAmmo7 = ply.GetAmmo(AmmoType.DROPPED_7);
					AdminToolbox.playerdict[ply.SteamId].prevAmmo9 = ply.GetAmmo(AmmoType.DROPPED_9);
				}
				//Changes role to Tutorial, teleports to jail, removes inv.
				ply.ChangeRole(Role.TUTORIAL, true, false);
				ply.Teleport(AdminToolbox.warpVectors["jail"]);
				foreach (Smod2.API.Item item in ply.GetInventory())
					item.Remove();
				AdminToolbox.playerdict[ply.SteamId].isJailed = true;
			}
			else
				plugin.Info("Player not in PlayerDict!");
		}
		public static void ReturnFromJail(Player ply)
		{
			if (AdminToolbox.playerdict.ContainsKey(ply.SteamId))
			{
				AdminToolbox.playerdict[ply.SteamId].isJailed = false;
				ply.ChangeRole(AdminToolbox.playerdict[ply.SteamId].previousRole, true, false);
				ply.Teleport(AdminToolbox.playerdict[ply.SteamId].originalPos);
				AdminToolbox.playerdict[ply.SteamId].isInJail = false;
				ply.SetHealth(AdminToolbox.playerdict[ply.SteamId].previousHealth);
				foreach (Smod2.API.Item item in ply.GetInventory())
					item.Remove();
				foreach (Smod2.API.Item item in AdminToolbox.playerdict[ply.SteamId].playerPrevInv)
					ply.GiveItem(item.ItemType);
				ply.SetAmmo(AmmoType.DROPPED_5, AdminToolbox.playerdict[ply.SteamId].prevAmmo5);
				ply.SetAmmo(AmmoType.DROPPED_7, AdminToolbox.playerdict[ply.SteamId].prevAmmo7);
				ply.SetAmmo(AmmoType.DROPPED_9, AdminToolbox.playerdict[ply.SteamId].prevAmmo9);
				AdminToolbox.playerdict[ply.SteamId].playerPrevInv = null;
			}
			else
				plugin.Info("Player not in PlayerDict!");
		}

		public static string WriteParseableLogKills(Player attacker, Player victim, DamageType dmgType)
		{
			return " ";
		}
		public static void WriteToLog(string[] str, LogHandlers.ServerLogType logType = LogHandlers.ServerLogType.Misc)
		{
			string str2 = string.Empty;
			if (str.Length != 0)
				foreach (string st in str)
					str2 += st;
			switch (logType)
			{
				case LogHandlers.ServerLogType.TeamKill:
					if (ConfigManager.Manager.Config.GetBoolValue("admintoolbox_log_teamkills", false, false))
						AdminToolboxLogger.AddLog(str2, logType);
					break;
				case LogHandlers.ServerLogType.KillLog:
					if (ConfigManager.Manager.Config.GetBoolValue("admintoolbox_log_kills", false, false))
						AdminToolboxLogger.AddLog(str2, logType);
					break;
				case LogHandlers.ServerLogType.RemoteAdminActivity:
					if (ConfigManager.Manager.Config.GetBoolValue("admintoolbox_log_commands", false, false))
						AdminToolboxLogger.AddLog(str2, logType);
					break;
				default:
					AdminToolboxLogger.AddLog(str2, logType);
					break;
			}
		}
		public static void WriteVersionToFile()
		{
			if (Directory.Exists(FileManager.AppFolder))
			{
				StreamWriter streamWriter = new StreamWriter(FileManager.AppFolder + "at_version.md", false);
				string text = "at_version=" + plugin.Details.version;
				streamWriter.Write(text);
				streamWriter.Close();
				if (File.Exists(FileManager.AppFolder + "n_at_version.md"))
					File.Delete(FileManager.AppFolder + "n_at_version.md");
			}
			else
				plugin.Info("Could not find SCP Secret Lab folder!");
		}
		public static void CheckCurrVersion(AdminToolbox plugin, string version)
		{
			try
			{
				string host = "http://raw.githubusercontent.com/Rnen/AdminToolbox/master/version.md";
				if (!int.TryParse(version.Replace(".", string.Empty), out int currentVersion))
					plugin.Info("Coult not get Int16 from currentVersion");
				if (int.TryParse(new System.Net.WebClient().DownloadString(host).Replace(".", string.Empty).Replace("at_version=", string.Empty), out int onlineVersion))
				{
					if (onlineVersion > currentVersion)
						plugin.Info("Your version is out of date, please run the \"AT_AutoUpdate.bat\" or visit the AdminToolbox GitHub");
				}
				else
					plugin.Info("Could not get Int16 from onlineVersion");

			}
			catch (System.Exception e)
			{
				plugin.Error("Could not fetch latest version: " + e.Message);
			}
		}
	}

	public static class LevenshteinDistance
	{
		/// <summary>
		/// Compute the distance between two strings.
		/// </summary>
		public static int Compute(string s, string t)
		{
			int n = s.Length;
			int m = t.Length;
			int[,] d = new int[n + 1, m + 1];

			// Step 1
			if (n == 0)
			{
				return m;
			}

			if (m == 0)
			{
				return n;
			}

			// Step 2
			for (int i = 0; i <= n; d[i, 0] = i++)
			{
			}

			for (int j = 0; j <= m; d[0, j] = j++)
			{
			}

			// Step 3
			for (int i = 1; i <= n; i++)
			{
				//Step 4
				for (int j = 1; j <= m; j++)
				{
					// Step 5
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

					// Step 6
					d[i, j] = Math.Min(
						Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
						d[i - 1, j - 1] + cost);
				}
			}
			// Step 7
			return d[n, m];
		}
	}
	public class GetPlayerFromString
	{
		public static Player GetPlayer(string args)
		{
			Player playerOut = null;
			if (int.TryParse(args, out int pID))
			{
				foreach (Player pl in PluginManager.Manager.Server.GetPlayers())
					if (pl.PlayerId == pID || pl.SteamId == pID.ToString())
						return pl;
			}
			else
			{
				//Takes a string and finds the closest player from the playerlist
				int maxNameLength = 31, LastnameDifference = 31/*, lastNameLength = 31*/;
				string str1 = args.ToLower();
				foreach (Player pl in PluginManager.Manager.Server.GetPlayers(str1))
				{
					if (!pl.Name.ToLower().Contains(args.ToLower())) { goto NoPlayer; }
					if (str1.Length < maxNameLength)
					{
						int x = maxNameLength - str1.Length;
						int y = maxNameLength - pl.Name.Length;
						string str2 = pl.Name;
						for (int i = 0; i < x; i++)
						{
							str1 += "z";
						}
						for (int i = 0; i < y; i++)
						{
							str2 += "z";
						}
						int nameDifference = LevenshteinDistance.Compute(str1, str2);
						if (nameDifference < LastnameDifference)
						{
							LastnameDifference = nameDifference;
							playerOut = pl;
						}
					}
					NoPlayer:;
				}
			}
			return playerOut;
		}
	}
	public class LogHandlers
	{
		public class LogHandler
		{
			public string Content;

			public string Type;

			public string Time;

			public bool Saved;
		}
		private readonly List<LogHandler> logs = new List<LogHandler>();

		private int _port;

		private int _maxlen;

		private bool unifiedStats = ConfigManager.Manager.Config.GetBoolValue("admintoolbox_stats_unified", true);

		private static string AdminToolboxFolder = (ConfigManager.Manager.Config.GetStringValue("admintoolbox_folder_path", string.Empty) != string.Empty) ? ConfigManager.Manager.Config.GetStringValue("admintoolbox_folder_path", FileManager.AppFolder + "AdminToolbox") : FileManager.AppFolder + "AdminToolbox";
		private static string AdminToolboxPlayerStats = (ConfigManager.Manager.Config.GetBoolValue("admintoolbox_stats_unified", true)) ? AdminToolboxFolder + Path.DirectorySeparatorChar + "PlayerStats" + Path.DirectorySeparatorChar + "Global" : AdminToolboxFolder + Path.DirectorySeparatorChar + "PlayerStats" + Path.DirectorySeparatorChar + PluginManager.Manager.Server.Port, AdminToolboxLogs = AdminToolboxFolder + Path.DirectorySeparatorChar + "ServerLogs";


		public enum ServerLogType
		{
			RemoteAdminActivity,
			KillLog,
			TeamKill,
			Suicice,
			GameEvent,
			Misc
		}
		public static readonly string[] Txt = new string[]
		{
		"Remote Admin",
		"Kill",
		"TeamKill",
		"Suicide",
		"Game Event",
		"Misc"
		};
		private void Awake()
		{
			Txt.ToList().ForEach(delegate (string txt)
			{
				_maxlen = Math.Max(_maxlen, txt.Length);
			});
		}
		void Start()
		{
			_port = PluginManager.Manager.Server.Port;
		}

		public void AddLog(string msg, ServerLogType type)
		{
			_port = PluginManager.Manager.Server.Port;
			string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");
			logs.Add(new LogHandler
			{
				Content = msg,
				Type = Txt[(int)type],
				Time = time
			});
			string mystring = System.Reflection.Assembly.GetAssembly(this.GetType()).Location;
			if (Directory.Exists(FileManager.AppFolder))
			{
				if (!Directory.Exists(AdminToolboxFolder))
					Directory.CreateDirectory(AdminToolboxFolder);
				if (!Directory.Exists(AdminToolboxLogs))
					Directory.CreateDirectory(AdminToolboxLogs);
				if (Directory.Exists(FileManager.AppFolder + "ATServerLogs"))
					MoveOldFiles();
				if (!Directory.Exists(AdminToolboxLogs + Path.DirectorySeparatorChar + _port))
					Directory.CreateDirectory(AdminToolboxLogs + Path.DirectorySeparatorChar + _port);

				StreamWriter streamWriter = new StreamWriter(AdminToolboxLogs + Path.DirectorySeparatorChar + _port + Path.DirectorySeparatorChar + AdminToolbox._roundStartTime + "_Round-" + AdminToolbox.roundCount + ".txt", true);
				string text = string.Empty;
				foreach (LogHandler log in logs)
				{
					if (!log.Saved)
					{
						log.Saved = true;
						string text2 = text;
						text = text2 + log.Time + " | " + ToMax(log.Type, _maxlen) + " | " + log.Content + Environment.NewLine;
					}
				}
				streamWriter.Write(text);
				streamWriter.Close();
			}
		}
		private void MoveOldFiles()
		{
			string infoString = (Directory.GetDirectories(FileManager.AppFolder + "ATServerLogs").Length > 0) ? "\n\n Relocated folders: " : string.Empty;
			string appdataPath = FileManager.AppFolder;
			foreach (string path in Directory.GetDirectories(FileManager.AppFolder + "ATServerLogs"))
			{
				if (!Directory.Exists(Path.DirectorySeparatorChar + path.Replace(FileManager.AppFolder + "ATServerLogs" + Path.DirectorySeparatorChar, string.Empty)))
				{
					Directory.Move(path, AdminToolboxLogs + Path.DirectorySeparatorChar + path.Replace(FileManager.AppFolder + "ATServerLogs" + Path.DirectorySeparatorChar, string.Empty));
					infoString += "\n" + " - " + path.Replace(appdataPath, string.Empty);
				}
			}
			if (infoString != string.Empty) AdminToolbox.plugin.Info(infoString + "\n\n New Path: " + AdminToolboxLogs.Replace(appdataPath, string.Empty));
			Directory.Delete(FileManager.AppFolder + "ATServerLogs");
		}
		private static string ToMax(string text, int max)
		{
			while (text.Length < max)
			{
				text += " ";
			}
			return text;
		}

		public enum PlayerFile
		{
			Read = 0,
			Write = 1
		}
		public void PlayerStatsFileManager(List<Player> players = null, PlayerFile FileOperation = PlayerFile.Read)
		{
			if (Directory.Exists(FileManager.AppFolder))
			{
				char splitChar = ';';
				if (!Directory.Exists(AdminToolboxFolder))
					Directory.CreateDirectory(AdminToolboxFolder);
				if (!Directory.Exists(AdminToolboxPlayerStats))
					Directory.CreateDirectory(AdminToolboxPlayerStats);

				if (players != null && players.Count > 0)
					foreach (Player player in players)
						ReadWriteHandler(player, FileOperation);
				else
					foreach (Player player in PluginManager.Manager.Server.GetPlayers())
						ReadWriteHandler(player, FileOperation);

				void ReadWriteHandler(Player pl, PlayerFile Operation)
				{
					if (pl.SteamId == string.Empty || pl.SteamId == null || pl.Name == "Server" || pl.Name == string.Empty) return;
					if (!AdminToolbox.playerdict.ContainsKey(pl.SteamId)) AdminToolbox.AddMissingPlayerVariables(new List<Player> { pl });
					switch (Operation)
					{
						case PlayerFile.Read:
							ReadFromFile(pl);
							break;
						case PlayerFile.Write:
							WriteToFile(pl);
							break;
					}
				}
				void WriteToFile(Player pl)
				{
					string playerFilePath = (AdminToolbox.playerdict.ContainsKey(pl.SteamId)) ? AdminToolboxPlayerStats + Path.DirectorySeparatorChar + pl.SteamId + ".txt" : AdminToolboxPlayerStats + Path.DirectorySeparatorChar + "server" + ".txt";
					if (!File.Exists(playerFilePath))
						File.Create(playerFilePath).Dispose();
					
					//AdminToolbox.AdminToolboxPlayerSettings playerSettings = (AdminToolbox.playerdict.ContainsKey(pl.SteamId)) ? AdminToolbox.playerdict[pl.SteamId] : new AdminToolbox.AdminToolboxPlayerSettings();
					int Kills = (AdminToolbox.playerdict.ContainsKey(pl.SteamId) && AdminToolbox.playerdict[pl.SteamId].Kills > 0) ? AdminToolbox.playerdict[pl.SteamId].Kills : 0;
					int TeamKills = (AdminToolbox.playerdict.ContainsKey(pl.SteamId) && AdminToolbox.playerdict[pl.SteamId].TeamKills > 0) ? AdminToolbox.playerdict[pl.SteamId].TeamKills : 0;
					int Deaths = (AdminToolbox.playerdict.ContainsKey(pl.SteamId) && AdminToolbox.playerdict[pl.SteamId].Deaths > 0) ? AdminToolbox.playerdict[pl.SteamId].Deaths : 0;
					double minutesPlayed = (AdminToolbox.playerdict.ContainsKey(pl.SteamId) && AdminToolbox.playerdict[pl.SteamId].minutesPlayed > 0) ? DateTime.Now.Subtract(AdminToolbox.playerdict[pl.SteamId].joinTime).TotalMinutes + AdminToolbox.playerdict[pl.SteamId].minutesPlayed : 0;
					if(AdminToolbox.playerdict.ContainsKey(pl.SteamId)) AdminToolbox.playerdict[pl.SteamId].joinTime = DateTime.Now;
					StreamWriter streamWriter = new StreamWriter(playerFilePath, false);
					string str = string.Empty + Kills + splitChar + TeamKills + splitChar + Deaths + splitChar +  minutesPlayed;
					streamWriter.Write(str);
					streamWriter.Close();
					ReadFromFile(pl);
				}
				void ReadFromFile(Player pl)
				{
					string playerFilePath = (AdminToolbox.playerdict.ContainsKey(pl.SteamId)) ? AdminToolboxPlayerStats + Path.DirectorySeparatorChar + pl.SteamId + ".txt" : AdminToolboxPlayerStats + Path.DirectorySeparatorChar + "server" + ".txt";
					if (!File.Exists(playerFilePath))
						PlayerStatsFileManager(new List<Player> { pl }, LogHandlers.PlayerFile.Write);
					string[] fileStrings = (File.ReadAllLines(playerFilePath).Length > 0) ? File.ReadAllLines(playerFilePath) : new string[] { "0;0;0;0" };
					string[] playerStats = fileStrings[0].Split(splitChar);
					if (AdminToolbox.playerdict.ContainsKey(pl.SteamId))
					{
						//AdminToolbox.AdminToolboxPlayerSettings myPlayer = AdminToolbox.playerdict[pl.SteamId];
						AdminToolbox.playerdict[pl.SteamId].Kills = (playerStats.Length > 0 && int.TryParse(playerStats[0], out int x1) && x1 > AdminToolbox.playerdict[pl.SteamId].Kills) ? x1 : AdminToolbox.playerdict[pl.SteamId].Kills;
						AdminToolbox.playerdict[pl.SteamId].TeamKills = (playerStats.Length > 1 && int.TryParse(playerStats[1], out int x2) && x2 > AdminToolbox.playerdict[pl.SteamId].TeamKills) ? x2 : AdminToolbox.playerdict[pl.SteamId].TeamKills;
						AdminToolbox.playerdict[pl.SteamId].Deaths = (playerStats.Length > 2 && int.TryParse(playerStats[2], out int x3) && x3 > AdminToolbox.playerdict[pl.SteamId].Deaths) ? x3 : AdminToolbox.playerdict[pl.SteamId].Deaths;
						AdminToolbox.playerdict[pl.SteamId].minutesPlayed = (playerStats.Length > 3 && double.TryParse(playerStats[3], out double x4) && x4 > AdminToolbox.playerdict[pl.SteamId].minutesPlayed) ? x4 : AdminToolbox.playerdict[pl.SteamId].minutesPlayed;
						//AdminToolbox.playerdict[pl.SteamId] = myPlayer;
					}
				}
			}
		}
	}
	public class SetPlayerVariables
	{
		public static void SetPlayerBools(string steamID, bool? spectatorOnly = null, bool? godMode = null, bool? dmgOff = null, bool? destroyDoor = null, bool? keepSettings = null, bool? lockDown = null, bool? instantKill = null, bool? isJailed = null)
		{
			if (!AdminToolbox.playerdict.ContainsKey(steamID)) return;
			AdminToolbox.playerdict[steamID].spectatorOnly = (spectatorOnly.HasValue) ? (bool)spectatorOnly : AdminToolbox.playerdict[steamID].spectatorOnly;
			AdminToolbox.playerdict[steamID].godMode = (godMode.HasValue) ? (bool)godMode : AdminToolbox.playerdict[steamID].godMode;
			AdminToolbox.playerdict[steamID].dmgOff = (dmgOff.HasValue) ? (bool)dmgOff : AdminToolbox.playerdict[steamID].dmgOff;
			AdminToolbox.playerdict[steamID].destroyDoor = (destroyDoor.HasValue) ? (bool)destroyDoor : AdminToolbox.playerdict[steamID].destroyDoor;
			AdminToolbox.playerdict[steamID].lockDown = (lockDown.HasValue) ? (bool)lockDown : AdminToolbox.playerdict[steamID].lockDown;
			AdminToolbox.playerdict[steamID].instantKill = (instantKill.HasValue) ? (bool)instantKill : AdminToolbox.playerdict[steamID].instantKill;
			AdminToolbox.playerdict[steamID].isJailed = (isJailed.HasValue) ? (bool)isJailed : AdminToolbox.playerdict[steamID].isJailed;
		}
		public static void SetPlayerStats(string steamID, int? Kills = null, int? TeamKills = null, int? Deaths = null, int? RoundsPlayed = null)
		{
			if (!AdminToolbox.playerdict.ContainsKey(steamID)) return;
			AdminToolbox.playerdict[steamID].Kills = (Kills.HasValue) ? (int)Kills : AdminToolbox.playerdict[steamID].Kills;
			AdminToolbox.playerdict[steamID].TeamKills = (TeamKills.HasValue) ? (int)TeamKills : AdminToolbox.playerdict[steamID].TeamKills; ;
			AdminToolbox.playerdict[steamID].Deaths = (Deaths.HasValue) ? (int)Deaths : AdminToolbox.playerdict[steamID].Deaths;
			AdminToolbox.playerdict[steamID].RoundsPlayed = (RoundsPlayed.HasValue) ? (int)RoundsPlayed : AdminToolbox.playerdict[steamID].RoundsPlayed;
		}
	}
}