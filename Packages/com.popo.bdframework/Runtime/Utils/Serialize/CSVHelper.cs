using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.ComponentModel;

namespace BDFramework.Serialize
{
    // This class uses Reflection and Linq so it's not the fastest thing in the
    // world; however I only use it in development builds where we want to allow
    // game data to be easily tweaked so this isn't an issue; I would recommend
    // you do the same.
    public static class CsvUtil
    {
        // Quote semicolons too since some apps e.g. Numbers don't like them
        static char[] quotedChars = new char[] {',', ';', '\n'};
        
        // Load a CSV into a list of struct/classes from a file where each line = 1 object
        // First line of the CSV must be a header containing property names
        // Can optionally include any other columns headed with #foo, which are ignored
        // E.g. you can include a #Description column to provide notes which are ignored
        // This method throws file exceptions if file is not found
        // Field names are matched case-insensitive for convenience
        // @param filename File to load
        // @param strict If true, log errors if a line doesn't have enough
        //   fields as per the header. If false, ignores and just fills what it can
        public static List<T> LoadObjects<T>(string filename, bool strict = true) where T : new()
        {
            using (var stream = File.OpenRead(filename))
            {
                using (var rdr = new StreamReader(stream))
                {
                    return LoadObjects<T>(rdr, strict);
                }
            }
        }

        // Load a CSV into a list of struct/classes from a stream where each line = 1 object
        // First line of the CSV must be a header containing property names
        // Can optionally include any other columns headed with #foo, which are ignored
        // E.g. you can include a #Description column to provide notes which are ignored
        // Field names are matched case-insensitive for convenience
        // @param rdr Input reader
        // @param strict If true, log errors if a line doesn't have enough
        //   fields as per the header. If false, ignores and just fills what it can
        public static List<T> LoadObjects<T>(TextReader rdr, bool strict = true) where T : new()
        {
            var ret = new List<T>();
            string header = rdr.ReadLine();
            var fieldDefs = ParseHeader(header);
            FieldInfo[] fi = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo[] pi = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            bool isValueType = typeof(T).IsValueType;
            string line;
            while ((line = rdr.ReadLine()) != null)
            {
                var obj = new T();
                // box manually to avoid issues with structs
                object boxed = obj;
                if (ParseLineToObject(line, fieldDefs, fi, pi, boxed, strict))
                {
                    // unbox value types
                    if (isValueType)
                        obj = (T) boxed;
                    ret.Add(obj);
                }
            }

            return ret;
        }

        // Load a CSV file containing fields for a single object from a file
        // No header is required, but it can be present with '#' prefix
        // First column is property name, second is value
        // You can optionally include other columns for descriptions etc, these are ignored
        // If you want to include a header, make sure the first line starts with '#'
        // then it will be ignored (as will any lines that start that way)
        // This method throws file exceptions if file is not found
        // Field names are matched case-insensitive for convenience
        public static void LoadObject<T>(string filename, ref T destObject)
        {
            using (var stream = File.Open(filename, FileMode.Open))
            {
                using (var rdr = new StreamReader(stream))
                {
                    LoadObject<T>(rdr, ref destObject);
                }
            }
        }

        // Load a CSV file containing fields for a single object from a stream
        // No header is required, but it can be present with '#' prefix
        // First column is property name, second is value
        // You can optionally include other columns for descriptions etc, these are ignored
        // Field names are matched case-insensitive for convenience
        public static void LoadObject<T>(TextReader rdr, ref T destObject)
        {
            FieldInfo[] fi = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo[] pi = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // prevent auto-boxing causing problems with structs
            object nonValueObject = destObject;
            string line;
            while ((line = rdr.ReadLine()) != null)
            {
                // Ignore optional header lines
                if (line.StartsWith("#"))
                    continue;

                string[] vals = EnumerateCsvLine(line).ToArray();
                if (vals.Length >= 2)
                {
                    SetField(RemoveSpaces(vals[0].Trim()), vals[1], fi, pi, nonValueObject);
                }
                else
                {
                    Debug.LogWarning(string.Format("CsvUtil: ignoring line '{0}': not enough fields", line));
                }
            }

            if (typeof(T).IsValueType)
            {
                // unbox
                destObject = (T) nonValueObject;
            }
        }

        // Save a single object to a CSV file
        // Will write 1 line per field, first column is name, second is value
        // This method throws exceptions if unable to write
        public static void SaveObject<T>(T obj, string filename)
        {
            using (var stream = File.Open(filename, FileMode.Create))
            {
                using (var wtr = new StreamWriter(stream, System.Text.Encoding.UTF8))
                {
                    SaveObject<T>(obj, wtr);
                }
            }
        }

        // Save a single object to a CSV stream
        // Will write 1 line per field, first column is name, second is value
        // This method throws exceptions if unable to write
        public static void SaveObject<T>(T obj, TextWriter w)
        {
            FieldInfo[] fi = typeof(T).GetFields();
            bool firstLine = true;
            foreach (FieldInfo f in fi)
            {
                // Good CSV files don't have a trailing newline so only add here
                if (firstLine)
                    firstLine = false;
                else
                    w.Write(Environment.NewLine);

                w.Write(f.Name);
                w.Write(",");
                string val = f.GetValue(obj).ToString();
                // Quote if necessary
                if (val.IndexOfAny(quotedChars) != -1)
                {
                    val = string.Format("\"{0}\"", val);
                }

                w.Write(val);
            }
        }

        // Save multiple objects to a CSV file
        // Writes a header line with field names, followed by one line per
        // object with each field value in each column
        // This method throws exceptions if unable to write
        public static void SaveObjects<T>(IEnumerable<T> objs, string filename)
        {
            using (var stream = File.Open(filename, FileMode.Create))
            {
                using (var wtr = new StreamWriter(stream, System.Text.Encoding.UTF8))
                {
                    SaveObjects<T>(objs, wtr);
                }
            }
        }

        // Save multiple objects to a CSV stream
        // Writes a header line with field names, followed by one line per
        // object with each field value in each column
        // This method throws exceptions if unable to write
        public static void SaveObjects<T>(IEnumerable<T> objs, TextWriter w)
        {
            FieldInfo[] fi = typeof(T).GetFields();
            WriteHeader<T>(fi, w);

            bool firstLine = true;
            foreach (T obj in objs)
            {
                // Good CSV files don't have a trailing newline so only add here
                if (firstLine)
                    firstLine = false;
                else
                    w.Write(Environment.NewLine);

                WriteObjectToLine(obj, fi, w);
            }
        }

        private static void WriteHeader<T>(FieldInfo[] fi, TextWriter w)
        {
            bool firstCol = true;
            foreach (FieldInfo f in fi)
            {
                // Good CSV files don't have a trailing comma so only add here
                if (firstCol)
                    firstCol = false;
                else
                    w.Write(",");

                w.Write(f.Name);
            }

            w.Write(Environment.NewLine);
        }

        private static void WriteObjectToLine<T>(T obj, FieldInfo[] fi, TextWriter w)
        {
            bool firstCol = true;
            foreach (FieldInfo f in fi)
            {
                // Good CSV files don't have a trailing comma so only add here
                if (firstCol)
                    firstCol = false;
                else
                    w.Write(",");

                string val = f.GetValue(obj).ToString();
                // Quote if necessary
                if (val.IndexOfAny(quotedChars) != -1)
                {
                    val = string.Format("\"{0}\"", val);
                }

                w.Write(val);
            }
        }

        // Parse the header line and return a mapping of field names to column
        // indexes. Columns which have a '#' prefix are ignored.
        private static Dictionary<string, int> ParseHeader(string header)
        {
            var headers = new Dictionary<string, int>();
            int n = 0;
            foreach (string field in EnumerateCsvLine(header))
            {
                var trimmed = field.Trim();
                if (!trimmed.StartsWith("#"))
                {
                    trimmed = RemoveSpaces(trimmed);
                    headers[trimmed] = n;
                }

                ++n;
            }

            return headers;
        }

        // Parse an object line based on the header, return true if any fields matched
        private static bool ParseLineToObject(string line, Dictionary<string, int> fieldDefs, FieldInfo[] fi, PropertyInfo[] pi, object destObject, bool strict)
        {
            string[] values = EnumerateCsvLine(line).ToArray();
            bool setAny = false;
            foreach (string field in fieldDefs.Keys)
            {
                int index = fieldDefs[field];
                if (index < values.Length)
                {
                    string val = values[index];
                    setAny = SetField(field, val, fi, pi, destObject) || setAny;
                }
                else if (strict)
                {
                    Debug.LogWarning(string.Format("CsvUtil: error parsing line '{0}': not enough fields", line));
                }
            }

            return setAny;
        }

        private static bool SetField(string fieldName, string val, FieldInfo[] fi, PropertyInfo[] pi, object destObject)
        {
            bool result = false;
            foreach (PropertyInfo p in pi)
            {
                // Case insensitive comparison
                if (string.Compare(fieldName, p.Name, true) == 0)
                {
                    // Might need to parse the string into the property type
                    object typedVal = p.PropertyType == typeof(string) ? val : ParseString(val, p.PropertyType);
                    p.SetValue(destObject, typedVal, null);
                    result = true;
                    break;
                }
            }

            foreach (FieldInfo f in fi)
            {
                // Case insensitive comparison
                if (string.Compare(fieldName, f.Name, true) == 0)
                {
                    // Might need to parse the string into the field type
                    object typedVal = f.FieldType == typeof(string) ? val : ParseString(val, f.FieldType);
                    f.SetValue(destObject, typedVal);
                    result = true;
                    break;
                }
            }

            return result;
        }

        private static object ParseString(string strValue, Type t)
        {
            var cv = TypeDescriptor.GetConverter(t);
            return cv.ConvertFromInvariantString(strValue);
        }

        private static IEnumerable<string> EnumerateCsvLine(string line)
        {
            // Regex taken from http://wiki.unity3d.com/index.php?title=CSVReader
            foreach (Match m in Regex.Matches(line,
                @"(((?<x>(?=[,\r\n]+))|""(?<x>([^""]|"""")+)""|(?<x>[^,\r\n]+)),?)",
                RegexOptions.ExplicitCapture))
            {
                yield return m.Groups[1].Value;
            }
        }

        private static string RemoveSpaces(string strValue)
        {
            return Regex.Replace(strValue, @"\s", string.Empty);
        }
    }
}
