﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security.Tests
{
	/// <summary>
	/// Tests for <see cref="AuthenticationContext"/>
	/// </summary>
	[TestClass]
	public sealed class TestAuthenticationContext
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new AuthenticationContext(null, null, null));
			var mockSystemIdentity = new Mock<ISystemIdentity>();

			var user = new User();

			var authContext = new AuthenticationContext(null, user, null);
			Assert.ThrowsException<ArgumentNullException>(() => new AuthenticationContext(mockSystemIdentity.Object, null, null));

			var instanceUser = new InstanceUser();

			Assert.ThrowsException<ArgumentNullException>(() => new AuthenticationContext(null, null, instanceUser));
			Assert.ThrowsException<ArgumentNullException>(() => new AuthenticationContext(mockSystemIdentity.Object, null, instanceUser));
			authContext = new AuthenticationContext(mockSystemIdentity.Object, user, null);
			authContext = new AuthenticationContext(null, user, instanceUser);
			authContext = new AuthenticationContext(mockSystemIdentity.Object, user, instanceUser);
			user.SystemIdentifier = "root";
			Assert.ThrowsException<ArgumentNullException>(() => new AuthenticationContext(null, user, null));
		}


		[TestMethod]
		public void TestGetRightsGeneric()
		{
			var user = new User();
			var instanceUser = new InstanceUser();
			var authContext = new AuthenticationContext(null, user, instanceUser);

			user.AdministrationRights = AdministrationRights.WriteUsers;
			instanceUser.ByondRights = ByondRights.ChangeVersion | ByondRights.ReadActive;
			Assert.AreEqual((ulong)user.AdministrationRights, authContext.GetRight(RightsType.Administration));
			Assert.AreEqual((ulong)instanceUser.ByondRights, authContext.GetRight(RightsType.Byond));
		}
	}
}
