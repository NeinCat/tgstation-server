﻿using System;
using System.Globalization;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Routes to a server actions
	/// </summary>
	public static class Routes
	{
		/// <summary>
		/// The root controller
		/// </summary>
		public const string Root = "/";

		/// <summary>
		/// The <see cref="Models.Administration"/> controller
		/// </summary>
		public const string Administration = Root + nameof(Models.Administration);

		/// <summary>
		/// The <see cref="Models.User"/> controller
		/// </summary>
		public const string User = Root + nameof(Models.User);

		/// <summary>
		/// The <see cref="Models.Instance"/> controller
		/// </summary>
		public const string InstanceManager = Root + nameof(Models.Instance);

		/// <summary>
		/// The <see cref="Models.Byond"/> controller
		/// </summary>
		public const string Byond = Root + nameof(Models.Byond);

		/// <summary>
		/// The <see cref="Models.Repository"/> controller
		/// </summary>
		public const string Repository = Root + nameof(Models.Repository);

		/// <summary>
		/// The <see cref="Models.DreamDaemon"/> controller
		/// </summary>
		public const string DreamDaemon = Root + nameof(Models.DreamDaemon);

		/// <summary>
		/// The <see cref="Models.ConfigurationFile"/> controller
		/// </summary>
		public const string Configuration = Root + "Config";

		/// <summary>
		/// To be paired with <see cref="Configuration"/> for accessing <see cref="Models.ConfigurationFile"/>s
		/// </summary>
		public const string File = "File";

		/// <summary>
		/// Full combination of <see cref="Configuration"/> and <see cref="File"/>
		/// </summary>
		public const string ConfigurationFile = Configuration + "/" + File;

		/// <summary>
		/// The <see cref="Models.InstanceUser"/> controller
		/// </summary>
		public const string InstanceUser = Root + nameof(Models.InstanceUser);

		/// <summary>
		/// The <see cref="Models.ChatBot"/> controller
		/// </summary>
		public const string Chat = Root + "Chat";

		/// <summary>
		/// The <see cref="Models.DreamMaker"/> controller
		/// </summary>
		public const string DreamMaker = Root + nameof(Models.DreamMaker);

		/// <summary>
		/// The <see cref="Models.Job"/> controller
		/// </summary>
		public const string Jobs = Root + nameof(Models.Job);

		/// <summary>
		/// The postfix for list operations
		/// </summary>
		public const string List = "List";

		/// <summary>
		/// Apply an <paramref name="id"/> postfix to a <paramref name="route"/>
		/// </summary>
		/// <param name="route">The route</param>
		/// <param name="id">The ID</param>
		/// <returns>The <paramref name="route"/> with <paramref name="id"/> appended</returns>
		public static string SetID(string route, long id) => String.Format(CultureInfo.InvariantCulture, "{0}/{1}", route, id);

		/// <summary>
		/// Get the /List postfix for a <paramref name="route"/>
		/// </summary>
		/// <param name="route">The route</param>
		/// <returns>The <paramref name="route"/> with /List appended</returns>
		public static string ListRoute(string route) => String.Format(CultureInfo.InvariantCulture, "{0}/{1}", route, List);
	}
}
