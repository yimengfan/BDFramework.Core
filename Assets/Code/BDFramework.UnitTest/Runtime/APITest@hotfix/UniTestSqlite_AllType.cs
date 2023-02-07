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
    
    public class UniTestSqlite_AllType
    {
        // 原子类型测试
        [PrimaryKey]
        public int Id { get; set; } = 1;
        public int TestInt { get; set; } = 1;
        public float TestFloat { get; set; } = 1.1111f;
        public double TestDouble { get; set; } = 1.12345678d;
        public string TestString { get; set; } = "string";
        public bool TestBool { get; set; } = false;
        // List 测试
        public List<int> TestIntList { get; set; } = new List<int>() {1, 2, 3, 4};
        public List<float> TestFloatList { get; set; } = new List<float>() {1.1111f, 2.1111f, 3.1111f, 4.1111f};
        public List<double> TestDoubleList { get; set; } = new List<double>() {1.12345678d, 2.12345678d, 3.12345678d, 4.12345678d};
        public List<string> TestStringList { get; set; } = new List<string>() {"1", "2", "3", "4"};
        public List<bool> TestBoolList { get; set; } = new List<bool>() {true, false, true, false};
    }

}
