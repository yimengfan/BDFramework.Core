namespace BDFramework.UFlux
{
    public interface IPropertyChange
    { 
        /// <summary>
        /// 设置一个属性改变
        /// </summary>
        /// <param name="name"></param>
        void SetPropertyChange(string name);
        /// <summary>
        /// 设置所有属性都改变
        /// </summary>
        void SetAllPropertyChanged();
        /// <summary>
        /// 获取改变的属性
        /// </summary>
        /// <returns></returns>
        string GetPropertyChange();
    }
}