﻿// Copyright 2017 MaulingMonkey
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

namespace testpage {
	const reIE11UA = /^Mozilla\/\d+\.\d+ \(.+; rv:11\.0\) like Gecko$/;

	export function tryAutoClose() {
		if (window.history.length !== 1) return;         // This tab has history, user must have navigated.  Do not close this tab.
		if (reIE11UA.test(navigator.userAgent)) return;  // IE11 "do you want to let the script close the tab" popups are even more annoying than the extra tabs.  Do not close this tab.
		if (getQueryParams().auto_close !== "1") return; // This tab was not opened with exactly this autoclose option.  Do not close this tab.
		window.close();
	}

	addEventListener("load", ()=>setTimeout(tryAutoClose, 10000)); // 10 seconds should be enough to run through all tests
}