﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http.Headers;

namespace Tgstation.Server.Api.Tests
{
	/// <summary>
	/// Tests for <see cref="ApiHeaders"/>
	/// </summary>
	[TestClass]
	public sealed class TestApiHeaders
	{
		readonly ProductHeaderValue productHeaderValue = new ProductHeaderValue("Tgstation.Server.Api.Tests", "1.0.0");

		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new ApiHeaders(null, null));
			Assert.ThrowsException<ArgumentNullException>(() => new ApiHeaders(productHeaderValue, null));
			var headers = new ApiHeaders(productHeaderValue, String.Empty);
		}

		[TestMethod]
		public void TestUserAgentsAreValid()
		{
			const string BrowserHeader = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36.";
			const string ConformantHeader = "TGSClient/3.2.1.4";

			ApiHeaders TestHeader(string userAgent)
			{
				var headers = new HeaderDictionary
				{
					{ "Accept", ApiHeaders.ApplicationJson },
					{ "Api", "Tgstation.Server.Api/4.0.0.0" },
					{ "Authorization", "Bearer asdfasdf" },
					{ "User-Agent", userAgent }
				};

				return new ApiHeaders(new RequestHeaders(headers));
			};

			var header = TestHeader(BrowserHeader);
			Assert.AreEqual(BrowserHeader, header.RawUserAgent);
			Assert.IsNull(header.UserAgent);

			header = TestHeader(ConformantHeader);
			Assert.AreEqual(ConformantHeader, header.RawUserAgent);
			Assert.IsNotNull(header.UserAgent);

			Assert.ThrowsException<InvalidOperationException>(() => TestHeader(String.Empty));
		}
	}
}
