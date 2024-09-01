using System;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class NodeException : Exception {
        public readonly string Reason;
        public readonly string HowToFix;
        public readonly Model.NodeData Node;
        public readonly AssetReference Asset;

        public string NodeId {
            get {
                return Node.Id;
            }
        }

        public NodeException (string reason, string howToFix) {

            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();
            if (gc == null || gc.CurrentNode == null) {
                throw new AssetGraphException ("Attempted to create NodeException outside node execution.");
            }

            this.Reason = reason;
            this.HowToFix = howToFix;
            this.Node = gc.CurrentNode;
            this.Asset = null;
        }

        public NodeException (string reason, string howToFix, AssetReference a) {

            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();
            if (gc == null || gc.CurrentNode == null) {
                throw new AssetGraphException ("Attempted to create NodeException outside node execution.");
            }

            this.Reason = reason;
            this.HowToFix = howToFix;
            this.Node = gc.CurrentNode;
            this.Asset = a;
        }

        public NodeException (string reason, string howToFix, Model.NodeData node) {
            this.Reason = reason;
            this.HowToFix = howToFix;
            this.Node = node;
            this.Asset = null;
        }

        public NodeException (string reason, string howToFix, Model.NodeData node, AssetReference a) {
            this.Reason = reason;
            this.HowToFix = howToFix;
            this.Node = node;
            this.Asset = a;
        }
	}
}