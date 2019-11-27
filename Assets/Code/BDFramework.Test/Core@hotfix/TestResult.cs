namespace BDFramework.Test.@hotfix
{
    public class TestResult
    {
        public bool IsPass;
        public string ErrorMsg = "";
        
        public  void  Equals(object obj,object obj2)
        {
            this.IsPass = obj.Equals(obj2);
        }
    }
}