namespace BDFramework.Editor.Unity3dEx.PluginsEx.Odin.Attribute
{
    /// <summary>
    /// Odin拓展，下拉列表选择文件
    /// </summary>
    public class Ex_SelectFileFromPath : System.Attribute
    {
        public string Path { get; private set; }
        public string SearchPartten { get; private set; }
        
        public int Width { get; private set; }

        public Ex_SelectFileFromPath(string path, string searchPartten = "*",int width=200)
        {
            this.Path = path;
            this.SearchPartten = searchPartten;
            Width = width;
        }
    }
}
