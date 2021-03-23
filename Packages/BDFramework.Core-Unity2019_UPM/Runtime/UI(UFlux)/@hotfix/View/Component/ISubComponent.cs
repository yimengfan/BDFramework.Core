namespace BDFramework.UFlux
{
    public interface ISubComponent
    {
        
        void RegisterSubComponent(string fieldname, IComponent component);
        
    }
}