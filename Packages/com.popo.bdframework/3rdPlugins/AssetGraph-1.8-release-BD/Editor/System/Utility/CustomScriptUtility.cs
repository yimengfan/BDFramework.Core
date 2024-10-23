using System;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public static class CustomScriptUtility {

		static readonly bool debug = false;

		public static string DecodeString(string data) {
			if(data.StartsWith(Model.Settings.BASE64_IDENTIFIER)) {
				var bytes = Convert.FromBase64String(data.Substring(Model.Settings.BASE64_IDENTIFIER.Length));
				data = System.Text.Encoding.UTF8.GetString(bytes);
			}
			return data;
		}
		public static string EncodeString(string data) {
			if(debug) {
				return data;
			} else {
				return Model.Settings.BASE64_IDENTIFIER + 
					Convert.ToBase64String( System.Text.Encoding.UTF8.GetBytes(data));
			}
		}
	}
}
