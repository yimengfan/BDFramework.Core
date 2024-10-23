using UnityEngine;
using System;
using System.Collections.Generic;

namespace AssetBundleGraph {

	[Serializable]
	public class ConnectionPointData {

		private const string ID = "id";
		private const string LABEL = "label";
		private const string PRIORITY = "orderPriority";
		private const string SHOWLABEL = "showLabel";

		/**
		* In order to support Unity serialization for Undo, cyclic reference need to be avoided.
		* For that reason, we are storing parentId instead of pointer to parent NodeData
		*/

		[SerializeField] private string id;
		[SerializeField] private string label;
		[SerializeField] private string parentId;
		[SerializeField] private bool isInput;
		[SerializeField] private Rect buttonRect;

//		private int orderPriority;
//		private bool showLabel;

		public ConnectionPointData(string id, string label, NodeData parent, bool isInput/*, int orderPriority, bool showLabel */) {
			this.id = id;
			this.label = label;
			this.parentId = parent.Id;
			this.isInput = isInput;
			this.buttonRect = new Rect();
					//			this.orderPriority = orderPriority;
//			this.showLabel = showLabel;
		}

		public ConnectionPointData(string label, NodeData parent, bool isInput) {
			this.id = Guid.NewGuid().ToString();
			this.label = label;
			this.parentId = parent.Id;
			this.isInput = isInput;
			this.buttonRect = new Rect();
//			this.orderPriority = pointGui.orderPriority;
//			this.showLabel = pointGui.showLabel;
		}

		public ConnectionPointData(Dictionary<string, object> dic, NodeData parent, bool isInput) {

			this.id = dic[ID] as string;
			this.label = dic[LABEL] as string;
			this.parentId = parent.Id;
			this.isInput = isInput;
			this.buttonRect = new Rect();
			//			this.orderPriority = pointGui.orderPriority;
			//			this.showLabel = pointGui.showLabel;
		}


		public string Id {
			get {
				return id;
			}
		}

		public string Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}

		public string NodeId {
			get {
				return parentId;
			}
		}

		public bool IsInput {
			get {
				return isInput;
			}
		}

		public bool IsOutput {
			get {
				return !isInput;
			}
		}

		public Rect Region {
			get {
				return buttonRect;
			}
		}

//		public int OrderPriority {
//			get {
//				return orderPriority;
//			}
//		}
//		public bool ShowLabel {
//			get {
//				return showLabel;
//			}
//		}

		public Dictionary<string, object> ToJsonDictionary() {
			return new Dictionary<string, object> () {
				{ID, this.id},
				{LABEL, this.label}
			};
		}
	}
}
