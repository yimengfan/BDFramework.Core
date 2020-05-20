using LitJson;
using BDFramework.DataListener;
namespace BDFramework.DataListener
{
    public class DataListener_Json : ADataListener
    {
        public void SetMap(object o)
        {
            var fs = o.GetType().GetFields();

            foreach (var f in fs)
            {
                AddData(f.Name);
                SetData(f.Name,f.GetValue(o));
            }
        }

        public string Tojson()
        {
            return JsonMapper.ToJson(this.dataMap);
        }
    }
}