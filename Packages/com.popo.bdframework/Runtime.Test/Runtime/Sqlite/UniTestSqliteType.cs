using System.Collections;
using System.Collections.Generic;
using SQLite4Unity3d;
using UnityEngine;

namespace BDFramework.EditorTest
{

    public class UniTestSqliteType
    {
        // 原子类型测试
        [PrimaryKey]
        public int Id { get; set; } = 1;
        
        public string IdStr { get; set; } = "0";

        public int TestInt { get; set; } = 1;
        public float TestFloat { get; set; } = 1.1111f;
        public double TestDouble { get; set; } = 1.12345678d;
        public string TestString { get; set; } = "string";
        public bool TestBool { get; set; } = false;

        // List 测试
        // public List<int> TestIntList { get; set; } = new List<int>() {1, 2, 3, 4};
        // public List<float> TestFloatList { get; set; } = new List<float>() {1.1111f, 2.1111f, 3.1111f, 4.1111f};
        // public List<double> TestDoubleList { get; set; } = new List<double>() {1.12345678d, 2.12345678d, 3.12345678d, 4.12345678d};
        // public List<string> TestStringList { get; set; } = new List<string>() {"1", "2", "3", "4"};
        // public List<bool> TestBoolList { get; set; } = new List<bool>() {true, false, true, false};

        // Array 测试
        public int[] TestIntArray { get; set; } = new int[] {1, 2, 3, 4};
        public float[] TestFloatArray { get; set; } = new float[] {1.1111f, 2.1111f, 3.1111f, 4.1111f};
        public double[] TestDoubleArray { get; set; } = new double[] {1.12345678d, 2.12345678d, 3.12345678d, 4.12345678d};
        public string[] TestStringArray { get; set; } = new string[] {"1", "2", "3", "4"};
        public bool[] TestBoolArray { get; set; } = new bool[] {true, false, true, false};
    }
}