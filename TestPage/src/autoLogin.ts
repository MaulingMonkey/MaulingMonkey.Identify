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

declare namespace gapi {
	function load(id: string, onLoad: ()=>void);
}

namespace testpage {
	export function autoLogin() {
		console.log("testpage.autoLogin");

		gapi.load("auth2", function(){
			console.assert(!!gapi.auth2,                 "No auth2 - did https://apis.google.com/js/platform.js fail to load?");
			console.assert(!!getQueryParams().client_id, "No client id - this page is expected to be launched via the Test");

			gapi.auth2.init({
				client_id:           getQueryParams().client_id,
				fetch_basic_profile: false,
				scope:               "profile openid"
			});

			var intervalToken = 0;
			intervalToken = setInterval(function(){
				if (checkIdToken()) {
					clearInterval(intervalToken);
					intervalToken = 0;
				}
			}, 100);
		});
	}

	function checkIdToken(): boolean {
		let instance = gapi.auth2.getAuthInstance();
		if (!instance) return false;
		let user = instance.currentUser.get();
		if (!user) return false;
		let auth = user.getAuthResponse();
		if (!auth) return false;
		if (!auth.id_token) return false;

		onIdToken(auth.id_token);
		return true;
	}

	function onIdToken(id_token: string) {
		let xhr = new XMLHttpRequest();
		xhr.open("POST", location.origin + location.pathname + "/api/0/auth", true);
		xhr.setRequestHeader("Authorization", "Bearer "+id_token);
		xhr.addEventListener("load",  tryAutoClose);
		xhr.addEventListener("error", tryAutoClose);
		xhr.send();
	}
}

let autoLogin = testpage.autoLogin;
