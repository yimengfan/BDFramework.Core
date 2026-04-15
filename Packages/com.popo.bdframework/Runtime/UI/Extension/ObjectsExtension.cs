namespace BDFramework.UFlux.Extension
{
   static public class ObjectsExtension
    {

       static public T GetValue<T>(this object o,string fieldName)
       {
           var t = o.GetType();
           var fi = t.GetField(fieldName);
           return (T)fi.GetValue(o);
       }
    }
}