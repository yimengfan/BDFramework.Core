namespace PtrReflection
{

    public class Box<T> : IBox
    {
        public T value;
        public Box(T value)
        {
            this.value = value;
        }
        public Box()
        {
        }
        public void SetObject(object obj)
        {
            this.value = (T)obj;
        }
        public object GetObject()
        {
            return value;
        }
    }

    public interface IBox
    {
        void SetObject(object ob);
        object GetObject();
    }
}
