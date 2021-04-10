namespace BDFramework.UFlux
{
    public interface IState
    {
        /// <summary>
        /// 状态源
        /// </summary>
        int Source { get; }

        object GetValue(string fieldName);
    }
}