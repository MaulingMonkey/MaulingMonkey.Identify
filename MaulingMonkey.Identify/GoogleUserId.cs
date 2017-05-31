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

using MaulingMonkey.Identify.Implementation;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace MaulingMonkey.Identify {
	/// <summary>
	/// TODO: Add rationale for this stronger type alias
	/// </summary>
	[DebuggerDisplay("GoogleUserId: {Value}")]
	public struct GoogleUserId {
		public readonly string Value;
		public bool HasValue { get { return Value != null; } }
		private GoogleUserId(string value) { Value = value; }

		public override int GetHashCode() { return Value.GetHashCode(); }
		public override bool Equals(object obj) {
			return (Value == null && obj == null)
				|| (obj is GoogleUserId && Value == ((GoogleUserId)obj).Value)
				|| (obj is string && Value == (string)obj);
		}
		public static implicit operator string(GoogleUserId id) { return id.Value; }
		// operator== and operator!= are defined in terms of the implicit conversion operator.
		// Avoid defining our own:  doing so would cause the implicit generation of e.g. operator==(GoogleUserId?,GoogleUserId?) which causes id == null checks to fail!

		/// <summary>Example: GoogleUserId.FromIdToken("google.jwt.here", new[]{"YOUR_APP_ID_HERE.apps.googleusercontent.com"});</summary>
		/// <example>GoogleUserId.FromIdToken("google.jwt.here", new[]{"YOUR_APP_ID_HERE.apps.googleusercontent.com"});</example>
		/// <param name="idToken"    >A google idToken JWT</param>
		/// <param name="validAppIds">An array of 1 or more valid google app IDs (of which 1 is expected to be the audience of this JWT)</param>
		/// <returns>A valid GoogleUserId if and only if idToken validates - otherwise an exception will be thrown.  Never null.</returns>
		/// <exception cref="ArgumentNullException" >idToken is null</exception>
		/// <exception cref="ArgumentNullException" >validAppIds is null</exception>
		/// <exception cref="ArgumentException"     >idToken is not a valid JWT</exception>
		/// <exception cref="ArgumentException"     >validAppIds does not list at least 1 valid app id</exception>
		/// <exception cref="SecurityTokenException">idToken is not a valid JWT, has expired, is malformed, is missigned, has the wrong issuer, has the wrong audience, ...</exception>
		/// <exception cref="WebException"          >Certificates cannot be downloaded to validate idToken or are misformed (may be temporary)</exception>
		public static GoogleUserId FromIdToken(string idToken, string[] validAppIds) {
			if (idToken == null                  ) throw new ArgumentNullException(nameof(idToken), "idToken cannot be null");
			if (validAppIds == null              ) throw new ArgumentNullException(nameof(validAppIds), "validAppIds cannot be null");
			if (validAppIds.Length < 1           ) throw new ArgumentException("validAppIds must contain at least one valid app ID", nameof(validAppIds));
			if (validAppIds.Any(id => id == null)) throw new ArgumentException("validAppIds cannot contain null app IDs", nameof(validAppIds));
			if (idToken == ""                    ) throw new SecurityTokenException("idToken cannot be empty");

			var jwt = Google.ValidateIdToken(idToken, validAppIds);
			if (jwt == null) throw new SecurityTokenException("Google.ValidateIdToken failed to return a valid token");
			return new GoogleUserId(jwt.Subject);
		}

		/// <summary>Example: GoogleUserId.TryParseIdToken("google.jwt.here", new[]{"YOUR_APP_ID_HERE.apps.googleusercontent.com"});</summary>
		/// <example>GoogleUserId.TryParseIdToken("google.jwt.here", new[]{"YOUR_APP_ID_HERE.apps.googleusercontent.com"});</example>
		/// <param name="idToken"    >A google idToken JWT</param>
		/// <param name="validAppIds">An array of 1 or more valid google app IDs (of which 1 is expected to be the audience of this JWT)</param>
		/// <returns>A valid GoogleUserId if and only if idToken validates - otherwise null.</returns>
		/// <exception cref="ArgumentNullException" >validAppIds is null</exception>
		/// <exception cref="ArgumentException"     >validAppIds does not list at least 1 valid app id</exception>
		public static GoogleUserId TryParseIdToken(string idToken, string[] validAppIds) {
			if (validAppIds == null              ) throw new ArgumentNullException(nameof(validAppIds), "validAppIds cannot be null");
			if (validAppIds.Length < 1           ) throw new ArgumentException("validAppIds must contain at least one valid app ID", nameof(validAppIds));
			if (validAppIds.Any(id => id == null)) throw new ArgumentException("validAppIds cannot contain null app IDs", nameof(validAppIds));
			if (idToken == null                  ) return new GoogleUserId(null);
			if (idToken == ""                    ) return new GoogleUserId(null);

			try {
				var jwt = Google.ValidateIdToken(idToken, validAppIds);
				if (jwt == null) return new GoogleUserId(null);
				return new GoogleUserId(jwt.Subject);
			}
			catch (SecurityTokenException) { return new GoogleUserId(null); } // e.g. idToken is malformed, expired, missigned, has the wrong audience, etc.
			catch (WebException)           { return new GoogleUserId(null); } // e.g. https://www.googleapis.com/oauth2/v1/certs failed to respond and could not be satisfied by a cached result
		}
	}
}
