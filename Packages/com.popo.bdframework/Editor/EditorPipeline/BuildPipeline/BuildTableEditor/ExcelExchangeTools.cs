using System.Collections.Generic;
using Excel;
using System.Data;
using System.IO;
using System.Text;
using System.Reflection;
using System;
using LitJson;
using UnityEngine;


namespace BDFramework.Editor.Table
{
    /// <summary>
    /// Excel转换工具
    /// </summary>
    public class ExcelExchangeTools
    {
        /// <summary>
        /// 表格数据集合
        /// </summary>
        // private DataSet mResultSet;
        private DataTable mSheet = null;


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="excelFile">Excel file.</param>
        public ExcelExchangeTools(string excelFile)
        {
            FileStream mStream = File.Open(excelFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            IExcelDataReader mExcelReader = ExcelReaderFactory.CreateOpenXmlReader(mStream);
            var mResultSet = mExcelReader.AsDataSet();
            if (mResultSet.Tables.Count > 0)
            { 
                //默认读取第一个数据表,存在多sheet页签时，添加进去
                mSheet = mResultSet.Tables[0];
                for (int i = 1; i < mResultSet.Tables.Count; i++)
                {
                    foreach (DataRow row in mResultSet.Tables[i].Rows)
                    {
                        mSheet.Rows.Add(row.ItemArray);
                    }
                }
               
            }
        }

        public ExcelExchangeTools(DataTable sheet)
        {
            mSheet = sheet;
        }

        /// <summary>
        /// 转换为实体类列表
        /// </summary>
        public List<T> ConvertToList<T>()
        {
            //判断Excel文件中是否存在数据表
            if (mSheet == null) return null;

            //判断数据表内是否存在数据
            if (mSheet.Rows.Count < 1) return null;

            //读取数据表行数和列数
            int rowCount = mSheet.Rows.Count;
            int colCount = mSheet.Columns.Count;

            //准备一个列表以保存全部数据
            List<T> list = new List<T>();

            //读取数据
            for (int i = 1; i < rowCount; i++)
            {
                //创建实例
                Type t = typeof(T);
                ConstructorInfo ct = t.GetConstructor(System.Type.EmptyTypes);
                T target = (T)ct.Invoke(null);
                for (int j = 0; j < colCount; j++)
                {
                    //读取第1行数据作为表头字段
                    string field = mSheet.Rows[1][j].ToString();
                    object value = mSheet.Rows[i][j];
                    //设置属性值
                    SetTargetProperty(target, field, value);
                }

                //添加至列表
                list.Add(target);
            }

            return list;
        }

        /// <summary>
        /// 转换为Json
        /// </summary>
        /// <param name="JsonPath">Json文件路径</param>
        /// <param name="Header">表头行数</param>
        public void ConvertToJson(string JsonPath, Encoding encoding)
        {
            var json = GetJson(DBType.Local);
            //写入文件
            using (FileStream fileStream = new FileStream(JsonPath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter textWriter = new StreamWriter(fileStream, encoding))
                {
                    textWriter.Write(json);
                }
            }
        }

        /// <summary>
        /// 后补的接口，适配旧的调用
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="tableName"></param>
        public (bool, string) GetJson(DBType dbType)
        {
            int x = -1;
            int y = -1;
            var list = new List<object>();
            var (json, isSuccess) = GetJson(dbType.ToString(), ref x, ref y, ref list);
            return (isSuccess, json);
        }


        /// <summary>
        /// 获取json
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="IdX"></param>
        /// <param name="IdY"></param>
        /// <param name="keepFieldList"></param>
        /// <returns>返回json 和是否失败</returns>
        public (string, bool) GetJson(string tableName, ref int IdX, ref int IdY, ref List<object> keepFieldList)
        {
            IdX = -1;
            IdY = -1;
            //判断Excel文件中是否存在数据表
            if (mSheet == null)
            {
                return ("", false);
            }

            //默认读取第一个数据表

            //判断数据表内是否存在数据
            if (mSheet.Rows.Count < 1)
            {
                return ("", false);
            }

            //准备一个列表存储整个表的数据
            List<Dictionary<string, object>> table = new List<Dictionary<string, object>>();
            /************Keep * Mode 保留带*的行列 ********************/
            /*   server |      |   *   |
             *   local  |  *   |   *   |
             *          | Id   |   xxx |
             *     *    | 1    |       |
             *     *    | 2    |       |
             *
             * 带*的行列保留
             */
            List<object> serverRowDatas = new List<object>();
            List<object> localRowDatas = new List<object>();
            List<object> fieldNameRowDatas = new List<object>();
            //
            List<object> fieldTypeRowDatas = new List<object>();
            //第一行为备注，
            //寻找到id字段行数，以下全为数据
            int skipRowCount = -1;
            int skipColCount = -1;
            for (int i = 0; i < 10; i++)
            {
                var rows = this.GetRowDatas(i);
                if (rows.Count == 0)
                {
                    break;
                }

                //判断是否为Skip模式
                if (rows[0].ToString().ToLower().Equals("server"))
                {
                    serverRowDatas = rows;
                }
                else if (rows[0].ToString().ToLower().Equals("local"))
                {
                    localRowDatas = rows;
                }
            }

            //这里skip 防止有人在 备注行直接输入id
            int skipLine = 1;
            if (serverRowDatas.Count > 0)
            {
                skipLine = 3;
            }
            else
            {
                Debug.Log("【无server local模式】");
            }

            for (int i = skipLine; i < 10 && skipColCount == -1; i++)
            {
                var rows = this.GetRowDatas(i);
                //遍历rows
                for (int j = 0; j < rows.Count; j++)
                {
                    if (rows[j].Equals("Id"))
                    {
                        skipRowCount = i;
                        skipColCount = j;
                        fieldNameRowDatas = rows;
                        //获取字段类型
                        var rowtype = this.GetRowDatas(i - 1);
                        fieldTypeRowDatas = rowtype;
                        //
                        break;
                    }
                }
            }

            if (skipRowCount == -1)
            {
                Debug.LogError("表格数据可能有错,没发现Id字段,请检查");
                return ("{}", false);
            }


            IdX = skipColCount;
            IdY = skipRowCount;
            keepFieldList = new List<object>();
            if (tableName == "Local")
            {
                keepFieldList = localRowDatas;
            }
            else if (tableName == "Server")
            {
                keepFieldList = serverRowDatas;
            }

            bool isKeepStarMode = false;
            if (keepFieldList.Count > 0)
            {
                isKeepStarMode = true;
            }

            bool isSuccess = true;
            //读取数据
            for (int i = skipRowCount + 1; i < mSheet.Rows.Count; i++)
            {
                //准备一个字典存储每一行的数据
                Dictionary<string, object> row = new Dictionary<string, object>();

                for (int j = skipColCount; j < mSheet.Columns.Count; j++)
                {
                    string field = fieldNameRowDatas[j].ToString();
                    //跳过空字段
                    if (string.IsNullOrEmpty(field))
                    {
                        continue;
                    }

                    //根据*保留字段
                    if (keepFieldList.Count > 0)
                    {
                        if (!keepFieldList[j].Equals("*"))
                        {
                            continue;
                        }
                    }

                    //根据*保留记录
                    if (isKeepStarMode)
                    {
                        if (!mSheet.Rows[i][0].Equals("*"))
                        {
                            continue;
                        }
                    }

                    //Key-Value对应
                    var rowdata = mSheet.Rows[i][j];
                    //根据null判断
                    if (rowdata == null)
                    {
                        Debug.LogErrorFormat("表格数据为空：[{0},{1}]", i, j);
                        continue;
                    }

                    var fieldType = fieldTypeRowDatas[j].ToString().ToLower();
                    if (rowdata is DBNull) //空类型判断，赋默认值
                    {
                        if (fieldType == "int" || fieldType == "float" || fieldType == "double")
                        {
                            row[field] = 0;
                        }
                        else if (fieldType.Equals("string", StringComparison.OrdinalIgnoreCase))
                        {
                            row[field] = "";
                        }
                        else if (fieldType.Equals("bool", StringComparison.OrdinalIgnoreCase))
                        {
                            row[field] = false;
                        }
                        else if (fieldType.Contains("[]") || fieldType.Contains("list<")) //空数组
                        {
                            if (fieldType.Equals("int[]", StringComparison.OrdinalIgnoreCase) ||
                                fieldType.Equals("list<int>", StringComparison.OrdinalIgnoreCase))
                            {
                                row[field] = new int[0];
                            }
                            else if (fieldType.Equals("float[]", StringComparison.OrdinalIgnoreCase) ||
                                     fieldType.Equals("list<float>", StringComparison.OrdinalIgnoreCase))
                            {
                                row[field] = new float[0];
                            }
                            else if (fieldType.Equals("double[]", StringComparison.OrdinalIgnoreCase) ||
                                     fieldType.Equals("list<double>", StringComparison.OrdinalIgnoreCase))
                            {
                                row[field] = new double[0];
                            }
                            else if (fieldType.Equals("string[]", StringComparison.OrdinalIgnoreCase) || fieldType.Equals("list<string>", StringComparison.OrdinalIgnoreCase))
                            {
                                row[field] = new string[0];
                            }
                            else if (fieldType.Equals("bool[]", StringComparison.OrdinalIgnoreCase) ||
                                     fieldType.Equals("list<bool>", StringComparison.OrdinalIgnoreCase))
                            {
                                row[field] = new bool[0];
                            }
                        }
                    }
                    else
                    {
                        //string数组，对单个元素加上""
                        if (fieldType.Contains("[]") || fieldType.Contains("list<")) //空数组
                        {
                            var value = rowdata.ToString();
                            if (fieldType.Equals("string[]") || fieldType.Equals("list<string>", StringComparison.OrdinalIgnoreCase))
                            {
                                string[] strArray = null;
                                //空数组
                                if (value == "[]" || value == "\"[]\"")
                                {
                                    strArray = new string[0];
                                }
                                else
                                {
                                    //非空数组
                                    value = value.Replace("\"[", "").Replace("]\"", "");
                                    value = value.Replace("[", "").Replace("]", "");
                                    var strs = value.Split(',');
                                    strArray = new string[strs.Length];
                                    for (int k = 0; k < strs.Length; k++)
                                    {
                                        var str = strs[k];
                                        //移除首尾 "
                                        if (str.StartsWith("\""))
                                        {
                                            str = str.Remove(0, 1);
                                           
                                        }
                                        if (str.StartsWith(" \""))//空格情况
                                        {
                                            str = str.Remove(0, 2);
                                           
                                        }
                                        //移除末尾
                                        if (str.EndsWith("\""))
                                        {
                                            str = str.Remove(str.Length - 1, 1);
                                        }
                                        if (str.EndsWith("\" "))//空格情况
                                        {
                                            str = str.Remove(str.Length - 2, 2);
                                        }
                                        
                                        //
                                        if (str.Contains("\""))
                                        {
                                            int errorCount = 0;
                                            foreach (var s in str)
                                            {
                                                if (s== '\"')
                                                {
                                                    errorCount++;
                                                }
                                            }
                                            //
                                            if (errorCount % 2 != 0)
                                            {
                                                Debug.LogError($"引号数量不对，为基数:{errorCount} + {str}");
                                            }
                                        }
                                        strArray[k] = str;
                                    }
                                }



                                //解析
                                row[field] = strArray;
                            }
                            else
                            {
                                value = value.Replace("\"[", "[").Replace("]\"", "]");
                                if (fieldType.Equals("int[]", StringComparison.OrdinalIgnoreCase)|| fieldType.Equals("list<int>", StringComparison.OrdinalIgnoreCase))
                                {
                                    var ret = JsonMapper.ToObject<int[]>(value);
                                    row[field] = ret;
                                }
                                else if (fieldType.Equals("float[]", StringComparison.OrdinalIgnoreCase)|| fieldType.Equals("list<float>", StringComparison.OrdinalIgnoreCase))
                                {
                                    var ret = JsonMapper.ToObject<float[]>(value);
                                    row[field] = ret;
                                }
                                else if (fieldType.Equals("double[]", StringComparison.OrdinalIgnoreCase)|| fieldType.Equals("list<double>", StringComparison.OrdinalIgnoreCase))
                                {
                                    var ret = JsonMapper.ToObject<double[]>(value);
                                    row[field] = ret;
                                }
                                else if (fieldType.Equals("bool[]", StringComparison.OrdinalIgnoreCase)|| fieldType.Equals("list<bool>", StringComparison.OrdinalIgnoreCase))
                                {
                                    var ret = JsonMapper.ToObject<bool[]>(value);
                                    row[field] = ret;
                                }
                            }
                        }
                        //数值
                        else if (fieldType == "int" || fieldType == "float" || fieldType == "double")
                        {
                            var oldValue = rowdata.ToString();
                            if (fieldType == "int")
                            {
                                int value = 0;
                                if (int.TryParse(oldValue, out value))
                                {
                                    row[field] = value;
                                }
                                else
                                {
                                    row[field] = 0;
                                    Debug.LogError(
                                        $"表格数据类型出错:{i}-{j}, 字段名:{fieldNameRowDatas[j]}！要求类型:{fieldTypeRowDatas[j]}，实际类型：{oldValue.GetType()} - value:{oldValue} ");
                                    isSuccess = false;
                                }
                            }
                            else if (fieldType == "float")
                            {
                                float value = 0;
                                if (float.TryParse(oldValue, out value))
                                {
                                    row[field] = value;
                                }
                                else
                                {
                                    row[field] = 0;
                                    Debug.LogError(
                                        $"表格数据类型出错:{i}-{j}, 字段名:{fieldNameRowDatas[j]}！要求类型:{fieldTypeRowDatas[j]}，实际类型：{oldValue.GetType()} - value:{oldValue} ");
                                    isSuccess = false;
                                }
                            }
                            else if (fieldType == "double")
                            {
                                double value = 0;
                                if (double.TryParse(oldValue, out value))
                                {
                                    row[field] = value;
                                }
                                else
                                {
                                    row[field] = 0;
                                    Debug.LogError(
                                        $"表格数据类型出错:{i}-{j}, 字段名:{fieldNameRowDatas[j]}！要求类型:{fieldTypeRowDatas[j]}，实际类型：{oldValue.GetType()} - value:{oldValue} ");
                                    isSuccess = false;
                                }
                            }
                        }
                        else if (fieldType.Equals("string"))
                        {
                            var str = rowdata.ToString();
                            if (str == "\"\"")
                            {
                                row[field] = "";
                            }
                            else
                            {
                                row[field] = str;
                            }
                        }
                        else
                        {
                            row[field] = rowdata;
                        }
                    }
                }

                //添加到表数据中
                if (row.Count > 0)
                {
                    table.Add(row);
                }
            }

            //生成Json字符串
            string json = JsonMapper.ToJson(table);
            var jsonObject = JsonMapper.ToObject(json);
            foreach (JsonData jo in jsonObject)
            {
                //对比字段类型：
                // var keys = jo.Keys.ToArray();
                // foreach (var fieldName in keys)
                // {
                //     var jsonField = jo[fieldName];
                //     if (jsonField.IsString)
                //     {
                //         var jstr = jsonField.GetString();
                //
                //         if (jstr.Contains("[")) //把当字符串的数组"[]",重新处理成数组[]
                //         {
                //             jstr = jstr.Replace("\"[", "[").Replace("]\"", "]");
                //         }
                //         else
                //         {
                //             //把当字符串"\"\"",重新处理成数组""
                //             jstr = jstr.Replace("\\\"", "\"");
                //             jstr = jstr.Replace("\"\"\"\"", "\"\"");
                //         }
                //
                //         //重新赋值
                //         jo[fieldName] = jstr;
                //     }
                // }

                var joStr = jo.ToJson();
                //校验
                try
                {
                    var chekjo = JsonMapper.ToObject(joStr);
                }
                catch (Exception e)
                {
                    Debug.LogError($"校验失败,string内容：{joStr}");
                }
            }


            return (json, isSuccess);
        }

        /// <summary>
        /// 获取一行数据
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public List<object> GetRowDatas(int index)
        {
            List<object> list = new List<object>();

            //判断Excel文件中是否存在数据表
            if (mSheet == null)
            {
                return list;
            }

            //默认读取第一个数据表

            //判断数据表内是否存在数据
            if (mSheet.Rows.Count <= index)
            {
                return list;
            }

            //读取数据
            int colCount = mSheet.Columns.Count;
            for (int j = 0; j < colCount; j++)
            {
                object item = mSheet.Rows[index][j];
                list.Add(item);
            }


            return list;
        }

        /// <summary>
        /// 转换为CSV
        /// </summary>
        public void ConvertToCSV(string CSVPath, Encoding encoding)
        {
            //判断Excel文件中是否存在数据表
            if (mSheet == null) return;

            //默认读取第一个数据表

            //判断数据表内是否存在数据
            if (mSheet.Rows.Count < 1) return;

            //读取数据表行数和列数
            int rowCount = mSheet.Rows.Count;
            int colCount = mSheet.Columns.Count;

            //创建一个StringBuilder存储数据
            StringBuilder stringBuilder = new StringBuilder();

            //读取数据
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    //使用","分割每一个数值
                    stringBuilder.Append(mSheet.Rows[i][j] + ",");
                }

                //使用换行符分割每一行
                stringBuilder.Append("\r\n");
            }

            //写入文件
            using (FileStream fileStream = new FileStream(CSVPath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter textWriter = new StreamWriter(fileStream, encoding))
                {
                    textWriter.Write(stringBuilder.ToString());
                }
            }
        }

        /// <summary>
        /// 导出为Xml
        /// </summary>
        public void ConvertToXml(string XmlFile)
        {
            //判断Excel文件中是否存在数据表
            if (mSheet == null) return;

            //默认读取第一个数据表

            //判断数据表内是否存在数据
            if (mSheet.Rows.Count < 1) return;

            //读取数据表行数和列数
            int rowCount = mSheet.Rows.Count;
            int colCount = mSheet.Columns.Count;

            //创建一个StringBuilder存储数据
            StringBuilder stringBuilder = new StringBuilder();
            //创建Xml文件头
            stringBuilder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            stringBuilder.Append("\r\n");
            //创建根节点
            stringBuilder.Append("<Table>");
            stringBuilder.Append("\r\n");
            //读取数据
            for (int i = 1; i < rowCount; i++)
            {
                //创建子节点
                stringBuilder.Append("  <Row>");
                stringBuilder.Append("\r\n");
                for (int j = 0; j < colCount; j++)
                {
                    stringBuilder.Append("   <" + mSheet.Rows[0][j].ToString() + ">");
                    stringBuilder.Append(mSheet.Rows[i][j].ToString());
                    stringBuilder.Append("</" + mSheet.Rows[0][j].ToString() + ">");
                    stringBuilder.Append("\r\n");
                }

                //使用换行符分割每一行
                stringBuilder.Append("  </Row>");
                stringBuilder.Append("\r\n");
            }

            //闭合标签
            stringBuilder.Append("</Table>");
            //写入文件
            using (FileStream fileStream = new FileStream(XmlFile, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter textWriter = new StreamWriter(fileStream, Encoding.GetEncoding("utf-8")))
                {
                    textWriter.Write(stringBuilder.ToString());
                }
            }
        }

        /// <summary>
        /// 设置目标实例的属性
        /// </summary>
        private void SetTargetProperty(object target, string propertyName, object propertyValue)
        {
            //获取类型
            Type mType = target.GetType();
            //获取属性集合
            PropertyInfo[] mPropertys = mType.GetProperties();
            foreach (PropertyInfo property in mPropertys)
            {
                if (property.Name == propertyName)
                {
                    property.SetValue(target, Convert.ChangeType(propertyValue, property.PropertyType), null);
                }
            }
        }
    }
}