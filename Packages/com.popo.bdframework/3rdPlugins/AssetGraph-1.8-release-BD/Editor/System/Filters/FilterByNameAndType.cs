using UnityEditor;

using System;
using System.Text.RegularExpressions;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	[CustomFilter("Filter by Filename and Type")]
	public class FilterByNameAndType : IFilter {

        static readonly int kVERSION = 1;

		[SerializeField] private string m_filterKeyword;
        [SerializeField] private string m_filterKeytype;
        [SerializeField] private int m_version;

		public string Label { 
			get {
				if(m_filterKeytype == Model.Settings.DEFAULT_FILTER_KEYTYPE) {
					return m_filterKeyword;
				} else {
					var pointIndex = m_filterKeytype.LastIndexOf('.');
					var keytypeName = (pointIndex > 0)? m_filterKeytype.Substring(pointIndex+1):m_filterKeytype;
					return $"{m_filterKeyword}[{keytypeName}]";
				}
			}
		}

		public FilterByNameAndType() {
			m_filterKeyword = Model.Settings.DEFAULT_FILTER_KEYWORD;
			m_filterKeytype = Model.Settings.DEFAULT_FILTER_KEYTYPE;
            m_version = kVERSION;
		}

		public FilterByNameAndType(string name, string type) {
			m_filterKeyword = name;
			m_filterKeytype = type;
            m_version = kVERSION;
		}

		public bool FilterAsset(AssetReference a) {

            CheckVersionAndUpgrade ();

			bool keywordMatch = Regex.IsMatch(a.importFrom, m_filterKeyword, 
				RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
	
			bool match = keywordMatch;
	
			if(keywordMatch && m_filterKeytype != Model.Settings.DEFAULT_FILTER_KEYTYPE) 
			{
                var incomingType = a.filterType;

                var filterType = FilterTypeUtility.FindFilterTypeFromGUIName (m_filterKeytype);

                match = incomingType != null && filterType == incomingType;
			}
	
			return match;
		}

        public void OnInspectorGUI (Rect rect, Action onValueChanged) {

            CheckVersionAndUpgrade ();

			var keyword = m_filterKeyword;

			GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");

            Rect filerKeywordRect = rect;
            Rect popupRect = rect;
            filerKeywordRect.width = 120f;
            popupRect.x     += 124f;
            popupRect.width -= 124f;

            keyword = EditorGUI.TextField(filerKeywordRect, m_filterKeyword, s);
            if (GUI.Button(popupRect, m_filterKeytype , "Popup")) {
                NodeGUI.ShowFilterKeyTypeMenu(
                    m_filterKeytype,
                    (string selectedTypeStr) => {
                        m_filterKeytype = selectedTypeStr;
                        onValueChanged();
                    } 
                );
            }
            if (keyword != m_filterKeyword) {
                m_filterKeyword = keyword;
                onValueChanged();
            }

		}

        private void CheckVersionAndUpgrade() {
            if(kVERSION < m_version) {
                throw new AssetGraphException("Graph Asset is created with newer version of AssetGraph. Please upgrade your project with newer version.");
            }

            if(kVERSION > m_version) {
                if (m_filterKeytype != Model.Settings.DEFAULT_FILTER_KEYTYPE) {
                    Type t = Type.GetType (m_filterKeytype);
                    m_filterKeytype = FilterTypeUtility.FindGUINameFromType (t);
                }
                m_version = kVERSION;
            }
        }
	}
}