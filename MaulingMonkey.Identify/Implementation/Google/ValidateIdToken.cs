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

using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace MaulingMonkey.Identify.Implementation {
	static partial class Google {
		static readonly string[] ValidIssuers = "accounts.google.com https://accounts.google.com".Split(' ');

		// https://github.com/googleplus/gplus-verifytoken-csharp/blob/master/verifytoken.ashx.cs
		public static JwtSecurityToken ValidateIdToken(string idToken, IEnumerable<string> googleClientIds) {
			var validationParams = new TokenValidationParameters() {
				ValidateAudience         = true,
				ValidAudiences           = googleClientIds,
				ValidateIssuer           = true,
				ValidIssuers             = ValidIssuers,
				ValidateIssuerSigningKey = true,
				RequireSignedTokens      = true,
				IssuerSigningKeyResolver = IssuerSigningKeyResolver,
				ValidateLifetime         = true,
				RequireExpirationTime    = true,
				ClockSkew                = TimeSpan.FromHours(13),
			};

			var handler = new JwtSecurityTokenHandler();

			SecurityToken validatedToken;
			var claimsPrincipal = handler.ValidateToken(idToken, validationParams, out validatedToken);
			if (claimsPrincipal != null) {
				return (JwtSecurityToken)validatedToken;
				//return claimsPrincipal.Claims.ToArray();
				//var claims = claimsPrincipal.Claims.ToDictionary(c => c.Type);
				// https://www.iana.org/assignments/jwt/jwt.xhtml
			}
			return null;
		}

		public static JwtSecurityToken TryValidateIdToken(string idToken, IEnumerable<string> googleClientIds) {
			try { return ValidateIdToken(idToken, googleClientIds); }
			//catch (SecurityTokenInvalidSignatureException) {}
			//catch (SecurityTokenValidationException)       {}
			catch (SecurityTokenException)                 {} // e.g. idToken is malformed, expired, missigned, has the wrong audience, etc.
			catch (WebException)                           {} // e.g. https://www.googleapis.com/oauth2/v1/certs failed to respond and could not be satisfied by a cached result
			return null;
		}

		static IEnumerable<SecurityKey> IssuerSigningKeyResolver(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters) {
			var certs = GetCertificatesByThumbprint("https://www.googleapis.com/oauth2/v1/certs");
			X509Certificate2 cert;
			if (!certs.TryGetValue(NormalizeFingerprint(kid), out cert)) return null;
			return new[] { new X509SecurityKey(cert) };
		}
	}
}
