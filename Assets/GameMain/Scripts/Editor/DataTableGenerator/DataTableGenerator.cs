﻿using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityGameFramework.Editor.DataTableTools;

namespace StarForce.Editor.DataTableTools
{
    public sealed class DataTableGenerator
    {
        private const string DataTablePath = "Assets/GameMain/DataTables";
        private const string CSharpCodePath = "Assets/GameMain/Scripts/DataTable";
        private const string CSharpCodeTemplateFileName = "Assets/GameMain/Configs/DataTableCodeTemplate.txt";
        private static readonly Regex EndWithNumberRegex = new Regex(@"\d+$");

        public static DataTableProcessor CreateDataTableProcessor(string dataTableName)
        {
            return new DataTableProcessor(Utility.Path.GetCombinePath(DataTablePath, dataTableName + ".txt"), Encoding.Default, 1, 2, null, 3, 4, 1);
        }

        public static void GenerateDataFile(DataTableProcessor dataTableProcessor, string dataTableName)
        {
            string binaryDataFileName = Utility.Path.GetCombinePath(DataTablePath, dataTableName + ".bytes");
            if (!dataTableProcessor.GenerateDataFile(binaryDataFileName, Encoding.UTF8) && File.Exists(binaryDataFileName))
            {
                File.Delete(binaryDataFileName);
            }
        }

        public static void GenerateCodeFile(DataTableProcessor dataTableProcessor, string dataTableName)
        {
            dataTableProcessor.SetCodeTemplate(CSharpCodeTemplateFileName, Encoding.UTF8);
            dataTableProcessor.SetCodeGenerator(DataTableCodeGenerator);

            string csharpCodeFileName = Utility.Path.GetCombinePath(CSharpCodePath, "DR" + dataTableName + ".cs");
            if (!dataTableProcessor.GenerateCodeFile(csharpCodeFileName, Encoding.UTF8, dataTableName) && File.Exists(csharpCodeFileName))
            {
                File.Delete(csharpCodeFileName);
            }
        }

        private static void DataTableCodeGenerator(DataTableProcessor dataTableProcessor, StringBuilder codeContent, object userData)
        {
            string dataTableName = (string)userData;

            codeContent.Replace("__DATA_TABLE_CREATE_TIME__", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            codeContent.Replace("__DATA_TABLE_NAME_SPACE__", "StarForce");
            codeContent.Replace("__DATA_TABLE_CLASS_NAME__", "DR" + dataTableName);
            codeContent.Replace("__DATA_TABLE_COMMENT__", dataTableProcessor.GetValue(0, 1) + "。");
            codeContent.Replace("__DATA_TABLE_ID_COMMENT__", "获取" + dataTableProcessor.GetComment(dataTableProcessor.IdColumn) + "。");
            codeContent.Replace("__DATA_TABLE_PROPERTIES__", GenerateDataTableProperties(dataTableProcessor));
            codeContent.Replace("__DATA_TABLE_STRING_PARSER__", GenerateDataTableStringParser(dataTableProcessor));
            codeContent.Replace("__DATA_TABLE_BYTES_PARSER__", GenerateDataTableBytesParser(dataTableProcessor));
            codeContent.Replace("__DATA_TABLE_STREAM_PARSER__", GenerateDataTableStreamParser(dataTableProcessor));
            codeContent.Replace("__DATA_TABLE_PROPERTY_ARRAY__", GenerateDataTablePropertyArray(dataTableProcessor));
        }

        private static string GenerateDataTableProperties(DataTableProcessor dataTableProcessor)
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool firstProperty = true;
            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    continue;
                }

                if (i == dataTableProcessor.IdColumn)
                {
                    // 编号列
                    continue;
                }

                if (firstProperty)
                {
                    firstProperty = false;
                }
                else
                {
                    stringBuilder.AppendLine().AppendLine();
                }

                stringBuilder
                    .AppendLine("        /// <summary>")
                    .AppendFormat("        /// 获取{0}。", dataTableProcessor.GetComment(i)).AppendLine()
                    .AppendLine("        /// </summary>")
                    .AppendFormat("        public {0} {1}", dataTableProcessor.GetLanguageKeyword(i), dataTableProcessor.GetName(i)).AppendLine()
                    .AppendLine("        {")
                    .AppendLine("            get;")
                    .AppendLine("            private set;")
                    .Append("        }");
            }

            return stringBuilder.ToString();
        }

        private static string GenerateDataTableStringParser(DataTableProcessor dataTableProcessor)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder
                .AppendLine("        public override bool ParseDataRow(GameFrameworkSegment<string> dataRowSegment)")
                .AppendLine("        {")
                .AppendLine("            // Star Force 示例代码，正式项目使用时请修改此处的生成函数，以处理 GCAlloc 问题！")
                .AppendLine("            string[] columnTexts = dataRowSegment.Source.Substring(dataRowSegment.Offset, dataRowSegment.Length).Split('\\t');")
                .AppendLine("            int index = 0;");

            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    stringBuilder.AppendLine("            index++;");
                    continue;
                }

                if (i == dataTableProcessor.IdColumn)
                {
                    // 编号列
                    stringBuilder.AppendLine("            m_Id = int.Parse(columnTexts[index++]);");
                    continue;
                }

                string languageKeyword = dataTableProcessor.GetLanguageKeyword(i);
                switch (languageKeyword)
                {
                    case "string":
                        stringBuilder.AppendFormat("            {0} = columnTexts[index++];", dataTableProcessor.GetName(i)).AppendLine();
                        break;
                    default:
                        stringBuilder.AppendFormat("            {0} = {1}.Parse(columnTexts[index++]);", dataTableProcessor.GetName(i), languageKeyword).AppendLine();
                        break;
                }
            }

            stringBuilder
                .AppendLine()
                .AppendLine("            GeneratePropertyArray();")
                .AppendLine("            return true;")
                .Append("        }");

            return stringBuilder.ToString();
        }

        private static string GenerateDataTableBytesParser(DataTableProcessor dataTableProcessor)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder
                .AppendLine("        public override bool ParseDataRow(GameFrameworkSegment<byte[]> dataRowSegment)")
                .AppendLine("        {")
                .AppendLine("            // Star Force 示例代码，正式项目使用时请修改此处的生成函数，以处理 GCAlloc 问题！")
                .AppendLine("            using (MemoryStream memoryStream = new MemoryStream(dataRowSegment.Source, dataRowSegment.Offset, dataRowSegment.Length, false))")
                .AppendLine("            {")
                .AppendLine("                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))")
                .AppendLine("                {");

            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    continue;
                }

                if (i == dataTableProcessor.IdColumn)
                {
                    // 编号列
                    stringBuilder.AppendLine("                    m_Id = binaryReader.ReadInt32();");
                    continue;
                }

                stringBuilder.AppendFormat("                    {0} = binaryReader.Read{1}();", dataTableProcessor.GetName(i), dataTableProcessor.GetTypeName(i)).AppendLine();
            }

            stringBuilder
                .AppendLine("                }")
                .AppendLine("            }")
                .AppendLine()
                .AppendLine("            GeneratePropertyArray();")
                .AppendLine("            return true;")
                .Append("        }");

            return stringBuilder.ToString();
        }

        private static string GenerateDataTableStreamParser(DataTableProcessor dataTableProcessor)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder
                .AppendLine("        public override bool ParseDataRow(GameFrameworkSegment<Stream> dataRowSegment)")
                .AppendLine("        {")
                .AppendLine("            Log.Warning(\"Not implemented ParseDataRow(GameFrameworkSegment<Stream>)\");")
                .AppendLine("            return false;")
                .Append("        }");

            return stringBuilder.ToString();
        }

        private static string GenerateDataTablePropertyArray(DataTableProcessor dataTableProcessor)
        {
            List<PropertyCollection> propertyCollections = new List<PropertyCollection>();
            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    continue;
                }

                if (i == dataTableProcessor.IdColumn)
                {
                    // 编号列
                    continue;
                }

                string name = dataTableProcessor.GetName(i);
                if (!EndWithNumberRegex.IsMatch(name))
                {
                    continue;
                }

                string propertyCollectionName = EndWithNumberRegex.Replace(name, string.Empty);
                int id = int.Parse(EndWithNumberRegex.Match(name).Value);

                PropertyCollection propertyCollection = null;
                foreach (PropertyCollection pc in propertyCollections)
                {
                    if (pc.Name == propertyCollectionName)
                    {
                        propertyCollection = pc;
                        break;
                    }
                }

                if (propertyCollection == null)
                {
                    propertyCollection = new PropertyCollection(propertyCollectionName, dataTableProcessor.GetLanguageKeyword(i));
                    propertyCollections.Add(propertyCollection);
                }

                propertyCollection.AddItem(id, name);
            }

            StringBuilder stringBuilder = new StringBuilder();
            bool firstProperty = true;
            foreach (PropertyCollection propertyCollection in propertyCollections)
            {
                if (firstProperty)
                {
                    firstProperty = false;
                }
                else
                {
                    stringBuilder.AppendLine().AppendLine();
                }

                stringBuilder
                    .AppendFormat("        private KeyValuePair<int, {1}>[] m_{0} = null;", propertyCollection.Name, propertyCollection.LanguageKeyword).AppendLine()
                    .AppendLine()
                    .AppendFormat("        public int Get{0}(int id)", propertyCollection.Name).AppendLine()
                    .AppendLine("        {")
                    .AppendFormat("            foreach (KeyValuePair<int, {1}> i in m_{0})", propertyCollection.Name, propertyCollection.LanguageKeyword).AppendLine()
                    .AppendLine("            {")
                    .AppendLine("                if (i.Key == id)")
                    .AppendLine("                {")
                    .AppendLine("                    return i.Value;")
                    .AppendLine("                }")
                    .AppendLine("            }")
                    .AppendLine()
                    .AppendFormat("            throw new GameFrameworkException(Utility.Text.Format(\"Get{0} with invalid id '{{0}}'.\", id.ToString()));", propertyCollection.Name).AppendLine()
                    .AppendLine("        }")
                    .AppendLine()
                    .AppendFormat("        public int Get{0}At(int index)", propertyCollection.Name).AppendLine()
                    .AppendLine("        {")
                    .AppendFormat("            if (index < 0 || index >= m_{0}.Length)", propertyCollection.Name).AppendLine()
                    .AppendLine("            {")
                    .AppendFormat("                throw new GameFrameworkException(Utility.Text.Format(\"Get{0}At with invalid index '{{0}}'.\", index.ToString()));", propertyCollection.Name).AppendLine()
                    .AppendLine("            }")
                    .AppendLine()
                    .AppendFormat("            return m_{0}[index].Value;", propertyCollection.Name).AppendLine()
                    .Append("        }");
            }

            if (propertyCollections.Count > 0)
            {
                stringBuilder.AppendLine().AppendLine();
            }

            stringBuilder
                .AppendLine("        private void GeneratePropertyArray()")
                .AppendLine("        {");

            firstProperty = true;
            foreach (PropertyCollection propertyCollection in propertyCollections)
            {
                if (firstProperty)
                {
                    firstProperty = false;
                }
                else
                {
                    stringBuilder.AppendLine().AppendLine();
                }

                stringBuilder
                    .AppendFormat("            m_{0} = new KeyValuePair<int, {1}>[]", propertyCollection.Name, propertyCollection.LanguageKeyword).AppendLine()
                    .AppendLine("            {");

                int itemCount = propertyCollection.ItemCount;
                for (int i = 0; i < itemCount; i++)
                {
                    KeyValuePair<int, string> item = propertyCollection.GetItem(i);
                    stringBuilder.AppendFormat("                new KeyValuePair<int, {0}>({1}, {2}),", propertyCollection.LanguageKeyword, item.Key.ToString(), item.Value).AppendLine();
                }

                stringBuilder.Append("            };");
            }

            stringBuilder
                .AppendLine()
                .Append("        }");

            return stringBuilder.ToString();
        }

        private sealed class PropertyCollection
        {
            private readonly string m_Name;
            private readonly string m_LanguageKeyword;
            private readonly List<KeyValuePair<int, string>> m_Items;

            public PropertyCollection(string name, string languageKeyword)
            {
                m_Name = name;
                m_LanguageKeyword = languageKeyword;
                m_Items = new List<KeyValuePair<int, string>>();
            }

            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            public string LanguageKeyword
            {
                get
                {
                    return m_LanguageKeyword;
                }
            }

            public int ItemCount
            {
                get
                {
                    return m_Items.Count;
                }
            }

            public KeyValuePair<int, string> GetItem(int index)
            {
                if (index < 0 || index >= m_Items.Count)
                {
                    throw new GameFrameworkException(Utility.Text.Format("GetItem with invalid index '{0}'.", index.ToString()));
                }

                return m_Items[index];
            }

            public void AddItem(int id, string propertyName)
            {
                m_Items.Add(new KeyValuePair<int, string>(id, propertyName));
            }
        }
    }
}