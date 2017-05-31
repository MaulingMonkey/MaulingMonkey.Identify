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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace MaulingMonkey.Identify.Implementation {
	static partial class Google {
		static readonly Regex reCertWrapper = new Regex(@"-----BEGIN CERTIFICATE-----[\r]?\n(?<certificate>.*?)[\r]?\n-----END CERTIFICATE-----", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.Singleline);
		public static Dictionary<string,X509Certificate2> GetCertificatesByThumbprint(string url) {
			var request = WebRequest.CreateHttp(url);
			request.UserAgent = "https://github.com/MaulingMonkey/MaulingMonkey.Identify/";
			request.ContentType = "application/json";
			request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default); // Apparently "BypassCache" is the default for (Http)WebRequest, not "Default".
			request.Timeout = request.ReadWriteTimeout = 10000; // Is this sane?
			// TODO: Avoid simultanious request herds with a lock or two?
			// TODO: Exponential backoff on failure?

			var response = (HttpWebResponse)request.GetResponse();
			Trace.WriteLine(string.Format("{0} {1}", response.IsFromCache ? "cached" : "REQUEST", url), "Google");
			// TODO: Inspect/validate this tomorrow after the 5h cached entry expires?
			try {
				if (response.StatusCode != HttpStatusCode.OK) throw new WebException("Failed to get certificates", null, WebExceptionStatus.ProtocolError, response);
				return GetCertificatesByThumbprint(response.GetResponseStream(), response);
			} finally { response.Close(); }
		}

		static Dictionary<string,X509Certificate2> GetCertificatesByThumbprint(Stream stream, HttpWebResponse response) {
			var reader = new JsonTextReader(new StreamReader(stream));
			var root = JToken.ReadFrom(reader) as JObject;
			if (root == null) throw new WebException("Certificates response isn't a JSON object", null, WebExceptionStatus.ProtocolError, response);

			var result = new Dictionary<string,X509Certificate2>();
			foreach (var entry in root) {
				string thumbprint = NormalizeFingerprint(entry.Key);

				string certWrapper; // includes begin/end tags
				try {
					certWrapper = (string)entry.Value;
					if (certWrapper == null) throw new ArgumentException("certText is null");
				}
				catch (ArgumentException innerException) { throw new WebException("Certificate key/value pairs contained a non-string value", innerException, WebExceptionStatus.ProtocolError, response); }

				var certText = reCertWrapper.Match(certWrapper); // stripped of begin/end tags
				if (!certText.Success) throw new WebException("Certificate key/value pairs contained a non-certificate value", null, WebExceptionStatus.ProtocolError, response);

				var certBytes = Encoding.UTF8.GetBytes(certText.Groups["certificate"].Value); // still base64(?) encoded

				var cert = new X509Certificate2(certBytes);
				Debug.Assert(thumbprint == NormalizeFingerprint(cert.Thumbprint));
				result.Add(cert.Thumbprint, cert);
			}

			return result;
		}
	}
}
