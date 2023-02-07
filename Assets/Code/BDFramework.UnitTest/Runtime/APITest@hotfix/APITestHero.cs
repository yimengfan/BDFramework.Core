using System.Collections.Generic;
using SQLite4Unity3d;

namespace BDFramework.UnitTest.Data
{
    // public class APITestHero
    // {
    //     // id
    //     [PrimaryKey]
    //     public double Id { get; set; } = 1;
    //
    //     // 名称
    //     public string Name { get; set; } = "xx";
    //
    //     // 级别
    //     public string Level { get; set; } = "";
    //
    //     // 星级 
    //     public double StarLevel { get; set; } = 1;
    //
    //     // 下个等级
    //     public double NextLevel { get; set; } = 1;
    //
    //     // 属性名
    //     public List<string> AttributeName { get; set; } = new List<string>(){"1","2","3","4"};
    //
    //     // 属性值
    //     public List<double> AttributeValue { get; set; } = new List<double>(){1,2,3,4};
    //
    //     // 拥有技能id
    //     public List<double> Skills { get; set; } = new List<double>(){1,2,3,4};
    // }
    
    public class APITestHero
    {
        // 原子类型测试
        [PrimaryKey]
        public int Id { get; set; } = 1;
        public int TestInt { get; set; } = 1;
        public float TestFloat { get; set; } = 1.888f;
        public double TestDouble { get; set; } = 1.11111d;
        public string TestString { get; set; } = "string";
        public bool TestBool { get; set; } = false;
        // List 测试
        public List<double> TestIntList { get; set; } = new List<double>() {1, 2, 3, 4};
        public List<float> TestFloatList { get; set; } = new List<float>() {1.1f, 2.2f, 3.3f, 4.4f};
        public List<double> TestDoubleList { get; set; } = new List<double>() {1d, 2d, 3d, 4};
        public List<string> TestStringList { get; set; } = new List<string>() {"1", "2", "3", "4"};
        public List<bool> TestBoolList { get; set; } = new List<bool>() {true, false, true, false};
    }

}
