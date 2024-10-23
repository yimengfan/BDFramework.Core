using System;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    /// <summary>
    /// IFilter is an interface to create custom filter condition.
    /// </summary>
	public interface IFilter {

        /// <summary>
        /// Label string for the filter.
        /// Label string will be displayed in output point and outgoing connection.
        /// </summary>
        /// <value>The label string.</value>
		string Label { get; }

        /// <summary>
        /// Filters the asset.
        /// </summary>
        /// <returns><c>true</c>, if asset meets filter criteria, <c>false</c> otherwise.</returns>
        /// <param name="asset">Asset.</param>
		bool FilterAsset(AssetReference asset);

        /// <summary>
        /// Draw Inspector GUI for this Filter.
        /// Make sure to call <c>onValueChanged</c>() when inspector values are modified. 
        /// It will save state of AssetGenerator object.
        /// </summary>
        /// <param name="onValueChanged">Action to call when inspector value changed.</param>
		void OnInspectorGUI (Rect rect, Action onValueChanged);
	}

	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomFilter : Attribute {
		private string m_name;

		public string Name {
			get {
				return m_name;
			}
		}

		public CustomFilter (string name) {
			m_name = name;
		}
	}
}