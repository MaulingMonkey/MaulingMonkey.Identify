// Copyright 2017 MaulingMonkey
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using MaulingMonkey.Identify;
using MaulingMonkey.Identify.Implementation;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Test {
	static class Program {
		static void Main(string[] args) {
			ParseCommandLineArgs(args);

			TestInternal_GoogleCerts();
			TestInternal_BadCalls();
			TestInternal_BadToken();
			TestApi_ExpiredTokens();
			TestApi_ViaTestPage();
		}

		static string TestWebsitePrefix;
		static string GoogleAppId;
		static void ParseCommandLineArgs(string[] args) {
			if (args.Length != 2) {
				Console.Error.WriteLine("Expected 2 arguments:");
				Console.Error.WriteLine("\thttp://test.website.root/");
				Console.Error.WriteLine("\tYOUR_APP_ID.apps.googleusercontent.com");
				Environment.Exit(1);
			} else {
				TestWebsitePrefix = args[0];
				GoogleAppId       = args[1];
			}
		}

		struct BadTokenEntry {
			public Type     ExpectedExceptionType;
			public string[] AppIds;
			public string   Token;
		}
		class BadTokenList : List<BadTokenEntry> {
			public void Add(Type expectedExceptionType, string[] appIds, string token) {
				Add(new BadTokenEntry() { ExpectedExceptionType = expectedExceptionType, AppIds = appIds, Token = token });
			}
		}
		static BadTokenList _BadTokens;
		static BadTokenList BadTokens { get {
			if (_BadTokens == null) _BadTokens = new BadTokenList {
				{                           typeof(ArgumentNullException),  null,               ""   },
				{                           typeof(ArgumentException),      new string[0],      ""   },
				{                           typeof(ArgumentException),      new string[1],      ""   },
				{                           typeof(ArgumentNullException),  new[]{GoogleAppId}, null },
				{                           typeof(SecurityTokenException), new[]{GoogleAppId}, ""   },

				{ /* Expired             */ typeof(SecurityTokenException), new[]{GoogleAppId}, "eyJhbGciOiJSUzI1NiIsImtpZCI6IjFlZTRkOWU3ZGNmZWYyMTVkMTMzYzdlZDdhYzg3Yzk1ZjhkOGU3MTIifQ.eyJhenAiOiI1NTk5NTgwMjI4MDItOTFpOGU2NmFsbTBpdW52bXNmbzc3OXBvcW50YW05bWcuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJhdWQiOiI1NTk5NTgwMjI4MDItOTFpOGU2NmFsbTBpdW52bXNmbzc3OXBvcW50YW05bWcuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJzdWIiOiIxMDA0ODA1NzU4MzQxMDYyMzI5MTYiLCJlbWFpbCI6InBhbmRhbW9qb0BnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiYXRfaGFzaCI6InE4WENDRlR1WWI4VFV1cUxHcnJzalEiLCJpc3MiOiJhY2NvdW50cy5nb29nbGUuY29tIiwiaWF0IjoxNDk1NjE2MTE0LCJleHAiOjE0OTU2MTk3MTQsIm5hbWUiOiJNaWNoYWVsIFJpY2tlcnQiLCJwaWN0dXJlIjoiaHR0cHM6Ly9saDUuZ29vZ2xldXNlcmNvbnRlbnQuY29tLy0xR0xjUE15R1I4by9BQUFBQUFBQUFBSS9BQUFBQUFBQUFFZy9BTkw0SDRtYkdzUS9zOTYtYy9waG90by5qcGciLCJnaXZlbl9uYW1lIjoiTWljaGFlbCIsImZhbWlseV9uYW1lIjoiUmlja2VydCIsImxvY2FsZSI6ImVuIn0.X1pV9ri1HniZzjtAfhZp2GdsjYlZAtg1mmvOOdJlS203nioj5xU43Hw-Ekhv-OprAoLOJk26SVGoaUDEJr3r2Vg4bmnTAJ7cnl3eAZ8Q58tBUeSRpIFVhYLaxNoGUA07e4tLn86sGRvfIR203kF7sbdF0zdIbVzVbBDD4fvP1OH2dJF_ESinoYztOIrPTnxw7UwXklxzGzRYbUhGDXWrc2An3Q_ZV8tUOdvlCkAqRP8YM9yT4JLx6tqVzSh4UgubVL2Y5zlj3hJdhcAdWm2EcaCzd95GSnzohV4PxxpBX3TXHKOJWR0nV07J0GDvBG_bdybwmUw9zbyFgSBY8oryKg" },
				{ /* Corrupted Header    */ typeof(SecurityTokenException), new[]{GoogleAppId}, "eyJhbGciOiJSUzI1NiIsImtpZCI6IjFlZTRkOWU3ZGNmZWYyMTVkMTMzYzdlZDdhYzg3Yzk2ZjhkOGU3MTIifQ.eyJhenAiOiI1NTk5NTgwMjI4MDItOTFpOGU2NmFsbTBpdW52bXNmbzc3OXBvcW50YW05bWcuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJhdWQiOiI1NTk5NTgwMjI4MDItOTFpOGU2NmFsbTBpdW52bXNmbzc3OXBvcW50YW05bWcuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJzdWIiOiIxMDA0ODA1NzU4MzQxMDYyMzI5MTYiLCJlbWFpbCI6InBhbmRhbW9qb0BnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiYXRfaGFzaCI6InE4WENDRlR1WWI4VFV1cUxHcnJzalEiLCJpc3MiOiJhY2NvdW50cy5nb29nbGUuY29tIiwiaWF0IjoxNDk1NjE2MTE0LCJleHAiOjE0OTU2MTk3MTQsIm5hbWUiOiJNaWNoYWVsIFJpY2tlcnQiLCJwaWN0dXJlIjoiaHR0cHM6Ly9saDUuZ29vZ2xldXNlcmNvbnRlbnQuY29tLy0xR0xjUE15R1I4by9BQUFBQUFBQUFBSS9BQUFBQUFBQUFFZy9BTkw0SDRtYkdzUS9zOTYtYy9waG90by5qcGciLCJnaXZlbl9uYW1lIjoiTWljaGFlbCIsImZhbWlseV9uYW1lIjoiUmlja2VydCIsImxvY2FsZSI6ImVuIn0.X1pV9ri1HniZzjtAfhZp2GdsjYlZAtg1mmvOOdJlS203nioj5xU43Hw-Ekhv-OprAoLOJk26SVGoaUDEJr3r2Vg4bmnTAJ7cnl3eAZ8Q58tBUeSRpIFVhYLaxNoGUA07e4tLn86sGRvfIR203kF7sbdF0zdIbVzVbBDD4fvP1OH2dJF_ESinoYztOIrPTnxw7UwXklxzGzRYbUhGDXWrc2An3Q_ZV8tUOdvlCkAqRP8YM9yT4JLx6tqVzSh4UgubVL2Y5zlj3hJdhcAdWm2EcaCzd95GSnzohV4PxxpBX3TXHKOJWR0nV07J0GDvBG_bdybwmUw9zbyFgSBY8oryKg" },
				{ /* Corrupted Payload   */ typeof(SecurityTokenException), new[]{GoogleAppId}, "eyJhbGciOiJSUzI1NiIsImtpZCI6IjFlZTRkOWU3ZGNmZWYyMTVkMTMzYzdlZDdhYzg3Yzk1ZjhkOGU3MTIifQ.eyJhenAiOiI1NTk5NTgwMjI4MDItOTFpOGU2NmFsbTbpdW52bXNmbzc3OXBvcW50YW05bWcuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJhdWQiOiI1NTk5NTgwMjI4MDItOTFpOGU2NmFsbTBpdW52bXNmbzc3OXBvcW50YW05bWcuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJzdWIiOiIxMDA0ODA1NzU4MzQxMDYyMzI5MTYiLCJlbWFpbCI6InBhbmRhbW9qb0BnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiYXRfaGFzaCI6InE4WENDRlR1WWI4VFV1cUxHcnJzalEiLCJpc3MiOiJhY2NvdW50cy5nb29nbGUuY29tIiwiaWF0IjoxNDk1NjE2MTE0LCJleHAiOjE0OTU2MTk3MTQsIm5hbWUiOiJNaWNoYWVsIFJpY2tlcnQiLCJwaWN0dXJlIjoiaHR0cHM6Ly9saDUuZ29vZ2xldXNlcmNvbnRlbnQuY29tLy0xR0xjUE15R1I4by9BQUFBQUFBQUFBSS9BQUFBQUFBQUFFZy9BTkw0SDRtYkdzUS9zOTYtYy9waG90by5qcGciLCJnaXZlbl9uYW1lIjoiTWljaGFlbCIsImZhbWlseV9uYW1lIjoiUmlja2VydCIsImxvY2FsZSI6ImVuIn0.X1pV9ri1HniZzjtAfhZp2GdsjYlZAtg1mmvOOdJlS203nioj5xU43Hw-Ekhv-OprAoLOJk26SVGoaUDEJr3r2Vg4bmnTAJ7cnl3eAZ8Q58tBUeSRpIFVhYLaxNoGUA07e4tLn86sGRvfIR203kF7sbdF0zdIbVzVbBDD4fvP1OH2dJF_ESinoYztOIrPTnxw7UwXklxzGzRYbUhGDXWrc2An3Q_ZV8tUOdvlCkAqRP8YM9yT4JLx6tqVzSh4UgubVL2Y5zlj3hJdhcAdWm2EcaCzd95GSnzohV4PxxpBX3TXHKOJWR0nV07J0GDvBG_bdybwmUw9zbyFgSBY8oryKg" },
				{ /* Corrupted Signature */ typeof(SecurityTokenException), new[]{GoogleAppId}, "eyJhbGciOiJSUzI1NiIsImtpZCI6IjFlZTRkOWU3ZGNmZWYyMTVkMTMzYzdlZDdhYzg3Yzk1ZjhkOGU3MTIifQ.eyJhenAiOiI1NTk5NTgwMjI4MDItOTFpOGU2NmFsbTBpdW52bXNmbzc3OXBvcW50YW05bWcuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJhdWQiOiI1NTk5NTgwMjI4MDItOTFpOGU2NmFsbTBpdW52bXNmbzc3OXBvcW50YW05bWcuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJzdWIiOiIxMDA0ODA1NzU4MzQxMDYyMzI5MTYiLCJlbWFpbCI6InBhbmRhbW9qb0BnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiYXRfaGFzaCI6InE4WENDRlR1WWI4VFV1cUxHcnJzalEiLCJpc3MiOiJhY2NvdW50cy5nb29nbGUuY29tIiwiaWF0IjoxNDk1NjE2MTE0LCJleHAiOjE0OTU2MTk3MTQsIm5hbWUiOiJNaWNoYWVsIFJpY2tlcnQiLCJwaWN0dXJlIjoiaHR0cHM6Ly9saDUuZ29vZ2xldXNlcmNvbnRlbnQuY29tLy0xR0xjUE15R1I4by9BQUFBQUFBQUFBSS9BQUFBQUFBQUFFZy9BTkw0SDRtYkdzUS9zOTYtYy9waG90by5qcGciLCJnaXZlbl9uYW1lIjoiTWljaGFlbCIsImZhbWlseV9uYW1lIjoiUmlja2VydCIsImxvY2FsZSI6ImVuIn0.X1pV9ri1HniZzjtAfhZp2GdsjYlZAtg1mmvOOdJlS203nioj5xU43Hw-Ekhv-OprAoLOJk26SVGoaUDEJr3r2Vg4bmnTAJ7cnl3eAZ8Q58tBUeSRpIFVhYLaxNoGUA07e4tLn86sGRvfIR203kF7sbdF0zdIbVzVbBDD4fvP1OH2dJF_ESinoYztOIrPTnxw7UwXklxzGzRYbUhGDXWrc2An3Q_ZV8tUOdvlCkAqRP8YM9yT4JLx6tqVzSh4UgubVL2Y5zlj3hJdhcAdWm2EcaCzd95GSnzohV4PxxpBX3TXHKOJWR0nV07J0GDvBG_bdybwmUw9zbyFgSBY8orykg" }
			};
			return _BadTokens;
		}}

		static void TestInternal_GoogleCerts() {
			var certs = Google.GetCertificatesByThumbprint(@"https://www.googleapis.com/oauth2/v1/certs");
			Debug.Assert(certs.Count>0);
			Console.WriteLine(nameof(TestInternal_GoogleCerts));
			foreach (var cert in certs) Console.WriteLine("\t{0}\t\t{1}", cert.Key, cert.Value.SubjectName.Name);
			//Console.WriteLine();
		}

		static void TestInternal_BadCalls() {
			// ...
		}

		static void TestInternal_BadToken() {
			Console.WriteLine(nameof(TestInternal_BadToken));
			foreach (var badToken in BadTokens) {
				try {
					var nullId = Google.TryValidateIdToken(badToken.Token, badToken.AppIds);
					Debug.Assert(nullId == null, "Google.TryValidateIdToken should have returned a null token");
				}
				catch (ArgumentNullException) { }
				catch (ArgumentException    ) { }
				catch (Exception            ) { Debug.Fail("Google.TryValidateIdToken should never throw anything but ArgumentNullException/ArgumentException"); }

				try {
					var thrownId = Google.ValidateIdToken(badToken.Token, badToken.AppIds);
					Debug.Fail("Google.ValidateIdToken should have thrown for badToken");
				}
				catch (WebException  ) { } // TODO: Inconclusive
				catch (Exception     ) { } // Don't test for specific internal exceptions
				//catch (Exception    e) { Debug.Assert(badToken.ExpectedExceptionType.IsAssignableFrom(e.GetType()), "Google.ValidateIdToken should have thrown a SecurityTokenException for badToken"); }
			}
		}

		static void TestApi_ExpiredTokens() {
			Console.WriteLine(nameof(TestApi_ExpiredTokens));
			foreach (var badToken in BadTokens) {
				try {
					var nullId = GoogleUserId.TryParseIdToken(badToken.Token, badToken.AppIds);
					Debug.Assert(nullId == null, "GoogleUserId.TryValidateIdToken should have returned a null token");
				}
				catch (ArgumentNullException) { }
				catch (ArgumentException    ) { }
				catch (Exception            ) { Debug.Fail("Google.TryValidateIdToken should never throw anything but ArgumentNullException/ArgumentException"); }

				try {
					var thrownId = GoogleUserId.FromIdToken(badToken.Token, badToken.AppIds);
					Debug.Fail("GoogleUserId.FromIdToken should have thrown for badToken");
				}
				catch (WebException  ) { } // TODO: Inconclusive
				catch (Exception    e) { Debug.Assert(badToken.ExpectedExceptionType.IsAssignableFrom(e.GetType()), "Google.ValidateIdToken should have thrown a SecurityTokenException for badToken"); }
			}
		}

		static readonly Regex reAuthorizationHeader = new Regex("^Bearer (?<jwt>.+?)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		static void TestApi_ViaTestPage() {
			var exe = @"I:\home\projects\new\MicroServer\StaticMicroServer\bin\Release\StaticMicroServer.exe";
			var testPage = Path.GetFullPath(@"..\..\..\TestPage\");
			var args = TestWebsitePrefix+" "+testPage+" --timeout=10s";
			var launchUrl = TestWebsitePrefix + "?auto_close=1&client_id="+GoogleAppId;

			string auth;

			var psi = new ProcessStartInfo(exe, args) {
				CreateNoWindow   = true,
				WindowStyle      = ProcessWindowStyle.Hidden,
				WorkingDirectory = Environment.CurrentDirectory,
			};
			using (var server = Process.Start(psi)) {
				if (server.WaitForExit(100)) Debug.Fail("Server failed already");

				var l = new HttpListener() { Prefixes = { TestWebsitePrefix + "api/" } };
				l.Start();
				Process.Start(launchUrl);
				var c = l.GetContext();

				try {
					auth = c.Request.Headers["Authorization"];
					if ((c.Request.HttpMethod == "POST") && c.Request.Url.AbsolutePath.EndsWith("/api/0/auth")) {
						c.Response.StatusCode = 200;
						c.Response.StatusDescription = "Recieved response";
					} else {
						c.Response.StatusCode = 500;
					}
				} finally {
					c.Response.Close();
					l.Close();
				}
			}

			var m = reAuthorizationHeader.Match(auth ?? "");
			Debug.Assert(m.Success);
			var goodIdToken = m.Groups["jwt"].Value;

			try {
				var tryGetId = GoogleUserId.TryParseIdToken(goodIdToken, new[] {GoogleAppId});
				Debug.Assert(tryGetId != null);

				var getId    = GoogleUserId.FromIdToken(goodIdToken, new[] {GoogleAppId});
				Debug.Assert(getId    != null);
			}
			catch (WebException) { } // TODO: Inconclusive
			catch (Exception) when (!Debugger.IsAttached) { Debug.Fail("Exception trying to handle known good tokens"); }
		}
	}
}
