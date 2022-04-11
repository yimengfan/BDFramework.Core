using UnityEditor;

namespace UnityEngine.AssetGraph {
	public class GraphBackground {

		protected const float kNodeGridSize = 12.0f;
		private const float kMajorGridSize = 120.0f;
		private static readonly Color kGridMinorColorDark  = new Color (0f, 0f, 0f, 0.18f);
		private static readonly Color kGridMajorColorDark  = new Color (0f, 0f, 0f, 0.28f);
		private static readonly Color kGridMinorColorLight = new Color (0f, 0f, 0f, 0.10f);
		private static readonly Color kGridMajorColorLight = new Color (0f, 0f, 0f, 0.15f);

		private Rect m_graphRegion;
		private Vector2 m_scrollPosition;

		private Material m_lineMaterial;

		private static Color gridMinorColor
		{
			get
			{
				if (EditorGUIUtility.isProSkin)
					return kGridMinorColorDark;
				else
					return kGridMinorColorLight;
			}
		}
		private static Color gridMajorColor
		{
			get
			{
				if (EditorGUIUtility.isProSkin)
					return kGridMajorColorDark;
				else
					return kGridMajorColorLight;
			}
		}

		private Material CreateLineMaterial ()
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
            Shader shader = Shader.Find ("Hidden/AssetGraph/LineDraw");
			Material m = new Material (shader);
			m.hideFlags = HideFlags.HideAndDontSave;
			return m;
		}

			
		public void Draw(Rect position, Vector2 scroll)
		{
			m_graphRegion = position;
			m_scrollPosition = scroll;

			if (Event.current.type == EventType.Repaint) {
				UnityEditor.Graphs.Styles.graphBackground.Draw (position, false, false, false, false);
			}

			DrawGrid ();
		}

		private void DrawGrid ()
		{
			if (Event.current.type != EventType.Repaint) {
				return;
			}

			if(m_lineMaterial == null) {
				m_lineMaterial = CreateLineMaterial();
			}

			m_lineMaterial.SetPass(0);

			GL.PushMatrix ();
			GL.Begin (GL.LINES);

			DrawGridLines (kNodeGridSize, gridMinorColor);
			DrawGridLines (kMajorGridSize, gridMajorColor);

			GL.End ();
			GL.PopMatrix ();
		}

		private void DrawGridLines (float gridSize, Color gridColor)
		{
			GL.Color (gridColor);
			for (float x = m_graphRegion.xMin - (m_graphRegion.xMin % gridSize) - m_scrollPosition.x; x < m_graphRegion.xMax; x += gridSize) {
				if(x < m_graphRegion.xMin) {
					continue;
				}
				DrawLine (new Vector2 (x, m_graphRegion.yMin), new Vector2 (x, m_graphRegion.yMax));
			}
			GL.Color (gridColor);
			for (float y = m_graphRegion.yMin - (m_graphRegion.yMin % gridSize) - m_scrollPosition.y; y < m_graphRegion.yMax; y += gridSize) {
				if(y < m_graphRegion.yMin) {
					continue;
				}
				DrawLine (new Vector2 (m_graphRegion.xMin, y), new Vector2 (m_graphRegion.xMax, y));
			}
		}

		private void DrawLine (Vector2 p1, Vector2 p2)
		{
			GL.Vertex (p1);
			GL.Vertex (p2);
		}
	}
}