namespace UnityEngine.AssetGraph {

	public class LogUtility {

		public static readonly string kTag = "AssetGraph";

		private static Logger s_logger;

		public static Logger Logger {
			get {
				if(s_logger == null) {
					s_logger = new Logger(Debug.unityLogger.logHandler);
                    ShowVerboseLog (UserPreference.DefaultVerboseLog);
				}

				return s_logger;
			}
		}

        public static void ShowVerboseLog(bool bVerbose) {
            var curValue = (bVerbose)? LogType.Log : LogType.Warning;
            if (curValue != Logger.filterLogType) {
                Logger.filterLogType = curValue;
            }
        }
	}
}
