namespace UnityEngine.AssetGraph {
	public class ConnectionEvent {
		public enum EventType : int {
			EVENT_NONE,

			EVENT_CONNECTION_TAPPED,
			EVENT_CONNECTION_DELETED,
		}

		public readonly EventType eventType;
		public readonly ConnectionGUI eventSourceCon;

		public ConnectionEvent (EventType type, ConnectionGUI con) {
			this.eventType = type;
			this.eventSourceCon = con;
		}
	}
}