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

namespace gapi {}

namespace testpage {
	interface QueryParams {
		auto_close?: string;
		client_id?:  string;
	}

	export function getQueryParams(): QueryParams {
		let q = location.search;
		if (!q) return {};
		if (q[0] === "?") q = q.substring(1);
		let kvs = q.split("&");
		let result = {};
		kvs.forEach(kv => {
			let [k,v] = kv.split('=', 2);
			if (!!k && !!v) result[k] = v;
		});
		return <QueryParams>result;
	}
}
