using UnityEditor;

using System;
using System.Collections.Generic;
using System.Linq;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    /// <summary>
    /// Node.
    /// </summary>
	public abstract class Node {

		#region Node input output types

        /// <summary>
        /// Gets the valid type of the node input.
        /// </summary>
        /// <value>The type of the node input.</value>
		public virtual Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.Assets;
			}
		}

        /// <summary>
        /// Gets the valid type of the node output.
        /// </summary>
        /// <value>The type of the node output.</value>
		public virtual Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.Assets;
			}
		}
		#endregion

        /// <summary>
        /// Category returns label string displayed at bottom of node.
        /// </summary>
        /// <value>The category.</value>
		public abstract string Category {
			get;
		}


		#region Initialization, Copy, Comparison, Validation
        /// <summary>
        /// Initialize Node with given NodeData.
        /// </summary>
        /// <param name="data">Data.</param>
		public abstract void Initialize(Model.NodeData data);

        /// <summary>
        /// Clone the node using newData.
        /// </summary>
        /// <param name="newData">New data.</param>
		public abstract Node Clone(Model.NodeData newData);

        /// <summary>
        /// Determines whether this instance is valid input connection point the specified point.
        /// </summary>
        /// <returns><c>true</c> if this instance is valid input connection point the specified point; otherwise, <c>false</c>.</returns>
        /// <param name="point">Point.</param>
		public virtual bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			return true;
		}
		#endregion

		#region Build

        /// <summary>
        /// Prepare is the method which validates and perform necessary setups in order to build.
        /// </summary>
        /// <param name="target">Target platform.</param>
        /// <param name="nodeData">NodeData instance for this node.</param>
        /// <param name="incoming">Incoming group of assets for this node on executing graph.</param>
        /// <param name="connectionsToOutput">Outgoing connections from this node.</param>
        /// <param name="outputFunc">An interface to set outgoing group of assets.</param>
		public virtual void Prepare (BuildTarget target, 
			Model.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc) 
		{
			// Do nothing
		}

        public virtual void Prepare (NodeBuildContext ctx) 
        {
            Prepare (ctx.target, ctx.nodeData, ctx.incoming, ctx.connectionsToOutput, ctx.outputFunc);
        }


        /// <summary>
        /// Build is the method which actualy performs the build. It is always called after Setup() is performed.
        /// </summary>
        /// <param name="target">Target platform.</param>
        /// <param name="nodeData">NodeData instance for this node.</param>
        /// <param name="incoming">Incoming group of assets for this node on executing graph.</param>
        /// <param name="connectionsToOutput">Outgoing connections from this node.</param>
        /// <param name="outputFunc">An interface to set outgoing group of assets.</param>
        /// <param name="progressFunc">An interface to display progress.</param>
		public virtual void Build (BuildTarget target, 
			Model.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc,
			Action<Model.NodeData, string, float> progressFunc)
		{
			// Do nothing
		}

        public virtual void Build (NodeBuildContext ctx) 
        {
            Build (ctx.target, ctx.nodeData, ctx.incoming, ctx.connectionsToOutput, ctx.outputFunc, ctx.progressFunc);
        }

		#endregion

		#region GUI
        /// <summary>
        /// Gets the active style name in GUISkin.
        /// </summary>
        /// <value>The active style.</value>
		public abstract string ActiveStyle 	 { get; }

        /// <summary>
        /// Gets the inactive style name in GUISkin.
        /// </summary>
        /// <value>The inactive style.</value>
		public abstract string InactiveStyle { get; }

        /// <summary>
        /// Raises the inspector GU event.
        /// </summary>
        /// <param name="node">NodeGUI instance for this node.</param>
        /// <param name="streamManager">Manager instance to retrieve graph's incoming/outgoing group of assets.</param>
        /// <param name="inspector">Helper instance to draw inspector.</param>
        /// <param name="onValueChanged">Action to call when OnInspectorGUI() changed value of this node.</param>
		public abstract void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged);

        /// <summary>
        /// OnContextMenuGUI() is called when Node is clicked for context menu.
        /// </summary>
        /// <param name="menu">Context menu instance.</param>
		public virtual void OnContextMenuGUI(GenericMenu menu) {
			// Do nothing
		}

        /// <summary>
        /// 绘制NodeGUI
        /// </summary>
        /// <param name="node"></param>
        public virtual void OnDrawNodeGUIContent(NodeGUI node)
        {
	        
        }
        
		#endregion

        #region Events
        /// <summary>
        /// OnAssetsReimported() is called when there are changes of assets during editing graph.
        /// </summary>
        /// <param name="nodeData">NodeGUI instance for this node.</param>
        /// <param name="streamManager">Manager instance to retrieve graph's incoming/outgoing group.</param>
        /// <param name="target">Target platform.</param>
        /// <param name="ctx">Reimport context.</param>
        public virtual bool OnAssetsReimported(
            Model.NodeData nodeData,
            AssetReferenceStreamManager streamManager,
            BuildTarget target, 
            AssetPostprocessorContext ctx,
            bool isBuilding)
        {
            return false;
        }

        /// <summary>
        /// OnNodeDelete() is called when node is about to be deleted.
        /// </summary>
        /// <param name="nodeData">NodeGUI instance for this node.</param>
        public virtual void OnNodeDelete(Model.NodeData nodeData) {
            // default does nothing
        }
        #endregion
	}

    /// <summary>
    /// Custom node attribute for custom nodes.
    /// </summary>
	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomNode : Attribute {

		private string m_name;
		private int m_orderPriority;

		public static readonly int kDEFAULT_PRIORITY = 1000;

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
		public string Name {
			get {
				return m_name;
			}
		}

        /// <summary>
        /// Gets the order priority.
        /// </summary>
        /// <value>The order priority.</value>
		public int OrderPriority {
			get {
				return m_orderPriority;
			}
		}

		public CustomNode (string name) {
			m_name = name;
			m_orderPriority = kDEFAULT_PRIORITY;
		}

		public CustomNode (string name, int orderPriority) {
			m_name = name;
			m_orderPriority = orderPriority;
		}
	}

	public struct CustomNodeInfo : IComparable {
		public CustomNode node;
		public Type type;

		public CustomNodeInfo(Type t, CustomNode n) {
			node = n;
			type = t;
		}

		public Node CreateInstance() {
            object o = type.Assembly.CreateInstance(type.FullName);
			return (Node) o;
		}

		public int CompareTo(object obj) {
			if (obj == null) {
				return 1;
			}

			CustomNodeInfo rhs = (CustomNodeInfo)obj;
			return node.OrderPriority - rhs.node.OrderPriority;
		}
	}

	public class NodeUtility {

		private static List<CustomNodeInfo> s_customNodes;

		public static List<CustomNodeInfo> CustomNodeTypes {
			get {
				if(s_customNodes == null) {
					s_customNodes = BuildCustomNodeList();
				}
				return s_customNodes;
			}
		}

		private static List<CustomNodeInfo> BuildCustomNodeList() {
			var list = new List<CustomNodeInfo>();

            var allNodes = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
	            if (assembly == null)
	            {
		            continue;
	            }

	            try
	            {
		            var nodes = assembly.GetTypes()
			            .Where(t => t != typeof(Node))
			            .Where(t => typeof(Node).IsAssignableFrom(t));
		            allNodes.AddRange (nodes);
	            }
	            catch (Exception e)
	            {
	            }

            }

            foreach (var type in allNodes) {
				CustomNode attr = 
					type.GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;

				if (attr != null) {
					list.Add(new CustomNodeInfo(type, attr));
				}
			}

			list.Sort();

			return list;
		}

		public static bool HasValidCustomNodeAttribute(Type t) {
			CustomNode attr = 
				t.GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
			return attr != null && !string.IsNullOrEmpty(attr.Name);
		}

		public static string GetNodeGUIName(Node node) {
			CustomNode attr = 
				node.GetType().GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
			if(attr != null) {
				return attr.Name;
			}
			return string.Empty;
		}

		public static string GetNodeGUIName(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomNode attr = 
                    type.GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
				if(attr != null) {
					return attr.Name;
				}
			}
			return string.Empty;
		}

		public static int GetNodeOrderPriority(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomNode attr = 
                    type.GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
				if(attr != null) {
					return attr.OrderPriority;
				}
			}
			return CustomNode.kDEFAULT_PRIORITY;
		}

		public static Node CreateNodeInstance(string assemblyQualifiedName) {
			if(assemblyQualifiedName != null) {
                var type = Type.GetType(assemblyQualifiedName);

                return (Node) type.Assembly.CreateInstance(type.FullName);
			}
			return null;
		}
	}
}
