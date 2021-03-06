﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="User"/>s.
	/// </summary>
	[Route(Routes.User)]
	public sealed class UserController : ApiController
	{
		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="UserController"/>
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="UserController"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="UserController"/>
		/// </summary>
		readonly ILogger<UserController> logger;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="UserController"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Construct a <see cref="UserController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/></param>
		public UserController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, ISystemIdentityFactory systemIdentityFactory, ICryptographySuite cryptographySuite, ILogger<UserController> logger, IOptions<GeneralConfiguration> generalConfigurationOptions) : base(databaseContext, authenticationContextFactory, logger, false, true)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Check if a given <paramref name="model"/> has a valid <see cref="Api.Models.Internal.User.Name"/> specified.
		/// </summary>
		/// <param name="model">The <see cref="UserUpdate"/> to check.</param>
		/// <returns><see langword="null"/> if <paramref name="model"/> is valid, a <see cref="BadRequestObjectResult"/> otherwise.</returns>
		BadRequestObjectResult CheckValidName(UserUpdate model)
		{
			if (model.Name != null && model.Name.Contains(':', StringComparison.InvariantCulture))
				return BadRequest(new ErrorMessage { Message = "Username must not contain colons!" });
			return null;
		}

		/// <summary>
		/// Attempt to change the password of a given <paramref name="dbUser"/>.
		/// </summary>
		/// <param name="dbUser">The user to update.</param>
		/// <param name="newPassword">The new password.</param>
		/// <returns><see langword="null"/> on success, <see cref="BadRequestObjectResult"/> if <paramref name="newPassword"/> is too short.</returns>
		BadRequestObjectResult TrySetPassword(Models.User dbUser, string newPassword)
		{
			if (newPassword.Length < generalConfiguration.MinimumPasswordLength)
				return BadRequest(new ErrorMessage { Message = $"Password must be at least {generalConfiguration.MinimumPasswordLength} characters long!" });
			cryptographySuite.SetUserPassword(dbUser, newPassword, true);
			return null;
		}

		/// <summary>
		/// Create a <see cref="Api.Models.User"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.User"/> to create.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="201"><see cref="Api.Models.User"/> created successfully.</response>
		/// <response code="410">The <see cref="Api.Models.Internal.User.SystemIdentifier"/> requested could not be loaded.</response>
		/// <response code="501">A system user was requested but this is not implemented on POSIX.</response>
		[HttpPut]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(typeof(Api.Models.User), 201)]
		[ProducesResponseType(410)]
		[ProducesResponseType(501)]
		public async Task<IActionResult> Create([FromBody] UserUpdate model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (!(model.Password == null ^ model.SystemIdentifier == null))
				return BadRequest(new ErrorMessage { Message = "User must have exactly one of either a password or system identifier!" });

			model.Name = model.Name?.Trim();
			if (model.Name?.Length == 0)
				model.Name = null;

			if (!(model.Name == null ^ model.SystemIdentifier == null))
				return BadRequest(new ErrorMessage { Message = "User must have a name if and only if user has no system identifier!" });

			var fail = CheckValidName(model);
			if (fail != null)
				return fail;

			var dbUser = new Models.User
			{
				AdministrationRights = model.AdministrationRights ?? AdministrationRights.None,
				CreatedAt = DateTimeOffset.Now,
				CreatedBy = AuthenticationContext.User,
				Enabled = model.Enabled ?? false,
				InstanceManagerRights = model.InstanceManagerRights ?? InstanceManagerRights.None,
				Name = model.Name,
				SystemIdentifier = model.SystemIdentifier,
				InstanceUsers = new List<Models.InstanceUser>()
			};

			if (model.SystemIdentifier != null)
				try
				{
					using (var sysIdentity = await systemIdentityFactory.CreateSystemIdentity(dbUser, cancellationToken).ConfigureAwait(false))
					{
						if (sysIdentity == null)
							return StatusCode((int)HttpStatusCode.Gone);
						dbUser.Name = sysIdentity.Username;
						dbUser.SystemIdentifier = sysIdentity.Uid;
					}
				}
				catch (NotImplementedException)
				{
					return StatusCode((int)HttpStatusCode.NotImplemented);
				}
			else
			{
				var result = TrySetPassword(dbUser, model.Password);
				if (result != null)
					return result;
			}

			dbUser.CanonicalName = dbUser.Name.ToUpperInvariant();

			DatabaseContext.Users.Add(dbUser);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			return StatusCode((int)HttpStatusCode.Created, dbUser.ToApi(true));
		}

		/// <summary>
		/// Update a <see cref="Api.Models.User"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.User"/> to update.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200"><see cref="Api.Models.User"/> updated successfully.</response>
		/// <response code="404">Requested <see cref="Api.Models.Internal.User.Id"/> does not exist.</response>
		[HttpPost]
		[TgsAuthorize(AdministrationRights.WriteUsers | AdministrationRights.EditOwnPassword)]
		[ProducesResponseType(typeof(Api.Models.User), 200)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> Update([FromBody] UserUpdate model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var callerAdministrationRights = (AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration);
			var passwordEditOnly = !callerAdministrationRights.HasFlag(AdministrationRights.WriteUsers);

			var originalUser = passwordEditOnly
				? AuthenticationContext.User
				: await DatabaseContext.Users.Where(x => x.Id == model.Id)
					.Include(x => x.CreatedBy)
					.FirstOrDefaultAsync(cancellationToken)
					.ConfigureAwait(false);
			if (originalUser == default)
				return NotFound();

			// Ensure they are only trying to edit password (system identity change will trigger a bad request)
			if (passwordEditOnly && (model.Id != originalUser.Id || model.InstanceManagerRights.HasValue || model.AdministrationRights.HasValue || model.Enabled.HasValue || model.Name != null))
				return Forbid();

			if (model.SystemIdentifier != null && model.SystemIdentifier != originalUser.SystemIdentifier)
				return BadRequest(new ErrorMessage { Message = "Cannot change a user's system identifier!" });

			if (model.Password != null)
			{
				var result = TrySetPassword(originalUser, model.Password);
				if (result != null)
					return result;
			}

			if (model.Name != null && model.Name.ToUpperInvariant() != originalUser.CanonicalName)
				return BadRequest(new ErrorMessage { Message = "Can only change capitalization of a user's name!" });

			originalUser.InstanceManagerRights = model.InstanceManagerRights ?? originalUser.InstanceManagerRights;
			originalUser.AdministrationRights = model.AdministrationRights ?? originalUser.AdministrationRights;
			originalUser.Enabled = model.Enabled ?? originalUser.Enabled;

			var fail = CheckValidName(model);
			if (fail != null)
				return fail;

			originalUser.Name = model.Name ?? originalUser.Name;

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			// return id only if not a self update or and cannot read users
			return Json(
				model.Id == originalUser.Id
				|| callerAdministrationRights.HasFlag(AdministrationRights.ReadUsers)
				? originalUser.ToApi(true)
				: new Api.Models.User
				{
					Id = originalUser.Id
				});
		}

		/// <summary>
		/// Get information about the current <see cref="Api.Models.User"/>.
		/// </summary>
		/// <returns>The <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="Api.Models.User"/> was retrieved successfully.</response>
		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Api.Models.User), 200)]
		public IActionResult Read() => Json(AuthenticationContext.User.ToApi(true));

		/// <summary>
		/// List all <see cref="Api.Models.User"/>s in the server.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.User"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		[ProducesResponseType(typeof(IEnumerable<Api.Models.User>), 200)]
		public async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			var users = await DatabaseContext.Users
				.Include(x => x.CreatedBy)
				.ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(users.Select(x => x.ToApi(true)));
		}

		/// <summary>
		/// Get a specific <see cref="Api.Models.User"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.Internal.User.Id"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="Api.Models.User"/> was retrieved successfully.</response>
		/// <response code="404">The <see cref="Api.Models.User"/> does not exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Api.Models.User), 200)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			if (id == AuthenticationContext.User.Id)
				return Read();

			if (!((AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration)).HasFlag(AdministrationRights.ReadUsers))
				return Forbid();

			var user = await DatabaseContext.Users
				.Where(x => x.Id == id)
				.Include(x => x.CreatedBy)
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (user == default)
				return NotFound();
			return Json(user.ToApi(true));
		}
	}
}
