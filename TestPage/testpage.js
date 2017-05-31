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
var testpage;
(function (testpage) {
    function autoLogin() {
        console.log("testpage.autoLogin");
        gapi.load("auth2", function () {
            console.assert(!!gapi.auth2, "No auth2 - did https://apis.google.com/js/platform.js fail to load?");
            console.assert(!!testpage.getQueryParams().client_id, "No client id - this page is expected to be launched via the Test");
            gapi.auth2.init({
                client_id: testpage.getQueryParams().client_id,
                fetch_basic_profile: false,
                scope: "profile openid"
            });
            var intervalToken = 0;
            intervalToken = setInterval(function () {
                if (checkIdToken()) {
                    clearInterval(intervalToken);
                    intervalToken = 0;
                }
            }, 100);
        });
    }
    testpage.autoLogin = autoLogin;
    function checkIdToken() {
        var instance = gapi.auth2.getAuthInstance();
        if (!instance)
            return false;
        var user = instance.currentUser.get();
        if (!user)
            return false;
        var auth = user.getAuthResponse();
        if (!auth)
            return false;
        if (!auth.id_token)
            return false;
        onIdToken(auth.id_token);
        return true;
    }
    function onIdToken(id_token) {
        var xhr = new XMLHttpRequest();
        xhr.open("POST", location.origin + location.pathname + "/api/0/auth", true);
        xhr.setRequestHeader("Authorization", "Bearer " + id_token);
        xhr.addEventListener("load", testpage.tryAutoClose);
        xhr.addEventListener("error", testpage.tryAutoClose);
        xhr.send();
    }
})(testpage || (testpage = {}));
var autoLogin = testpage.autoLogin;
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
var testpage;
(function (testpage) {
    function getQueryParams() {
        var q = location.search;
        if (!q)
            return {};
        if (q[0] === "?")
            q = q.substring(1);
        var kvs = q.split("&");
        var result = {};
        kvs.forEach(function (kv) {
            var _a = kv.split('=', 2), k = _a[0], v = _a[1];
            if (!!k && !!v)
                result[k] = v;
        });
        return result;
    }
    testpage.getQueryParams = getQueryParams;
})(testpage || (testpage = {}));
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
var testpage;
(function (testpage) {
    var reIE11UA = /^Mozilla\/\d+\.\d+ \(.+; rv:11\.0\) like Gecko$/;
    function tryAutoClose() {
        if (window.history.length !== 1)
            return; // This tab has history, user must have navigated.  Do not close this tab.
        if (reIE11UA.test(navigator.userAgent))
            return; // IE11 "do you want to let the script close the tab" popups are even more annoying than the extra tabs.  Do not close this tab.
        if (testpage.getQueryParams().auto_close !== "1")
            return; // This tab was not opened with exactly this autoclose option.  Do not close this tab.
        window.close();
    }
    testpage.tryAutoClose = tryAutoClose;
    addEventListener("load", function () { return setTimeout(tryAutoClose, 10000); }); // 10 seconds should be enough to run through all tests
})(testpage || (testpage = {}));
//# sourceMappingURL=testpage.js.map