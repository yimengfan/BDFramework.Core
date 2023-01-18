using System.Collections.Generic;
using SQLite4Unity3d;

namespace BDFramework.UnitTest.Data
{
    public class APITestHero
    {
        // id
        [PrimaryKey]
        public double Id { get; set; } = 1;

        // 名称
        public string Name { get; set; } = "xx";

        // 级别
        public string Level { get; set; } = "";

        // 星级 
        public double StarLevel { get; set; } = 1;

        // 下个等级
        public double NextLevel { get; set; } = 1;

        // 属性名
        public List<string> AttributeName { get; set; } = new List<string>();

        // 属性值
        public List<double> AttributeValue { get; set; } = new List<double>();

        // 拥有技能id
        public List<double> Skills { get; set; } = new List<double>();
    }
}