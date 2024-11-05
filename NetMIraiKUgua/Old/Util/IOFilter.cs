using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Windows.Forms.LinkLabel;

namespace MMDK.Util
{
    public enum FilterType
    {
        None,   // 无过滤
        Normal, // 一般模式杜绝词语出现
        Strict, // 严格模式杜绝单字出现
    }
    /// <summary>
    /// bot发言的总过滤器
    /// </summary>
    public class IOFilter
    {

        // Trie节点定义
        private class TrieNode
        {
            public Dictionary<char, TrieNode> Children { get; } = new Dictionary<char, TrieNode>();
            public string Replacement { get; set; }
            public bool IsEndOfWord { get; set; }
        }





        private static readonly Lazy<IOFilter> instance = new Lazy<IOFilter>(() => new IOFilter());
        //private static readonly object lockObject = new object(); // 用于线程安全
        private bool isLoaded;

        private readonly TrieNode rootNormal;
        private readonly TrieNode rootStrict;

        public static IOFilter Instance => instance.Value;
        private IOFilter()
        {
            rootNormal = new TrieNode();
            rootStrict = new TrieNode();
            isLoaded = false;
        }

        public bool Init()
        {
            if (isLoaded) return true;
            try
            {
                //string filterFile1 = Config.Instance.ResourceFullPath("FilterNormal");
                //string filterFile2 = Config.Instance.ResourceFullPath("FilterStrict");
                var fileLines = FileManager.ReadResourceLines("FilterNormal", true);
                foreach(var line in fileLines)
                {
                    string[] parts = line.Split("=>", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    string keyword = parts[0];
                    string replacement = parts.Length > 1 ? parts[1] : null;
                    AddRuleNormal(keyword, replacement);
                }



                fileLines = FileManager.ReadResourceLines("FilterStrict", true);
                foreach (var line in fileLines)
                {
                    string[] parts = line.Split("=>", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    string keyword = parts[0];
                    string replacement = parts.Length > 1 ? parts[1] : null;
                    AddRuleStrict(keyword, replacement);
                }
                isLoaded = true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            
            return isLoaded;
        }


        /// <summary>
        /// 添加规则到Trie
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="replacement"></param>
        private void AddRuleNormal(string keyword, string replacement)
        {
            TrieNode node = rootNormal;
            foreach (char c in keyword)
            {
                if (!node.Children.ContainsKey(c))
                {
                    node.Children[c] = new TrieNode();
                }
                node = node.Children[c];
            }
            node.IsEndOfWord = true;
            node.Replacement = replacement;
        }

        /// <summary>
        /// 添加规则到Trie
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="replacement"></param>
        private void AddRuleStrict(string keyword, string replacement)
        {
            TrieNode node = rootStrict;
            foreach (char c in keyword)
            {
                if (!node.Children.ContainsKey(c))
                {
                    node.Children[c] = new TrieNode();
                }
                node = node.Children[c];
            }
            node.IsEndOfWord = true;
            node.Replacement = replacement;
        }

        /// <summary>
        /// 字符串是否通过过滤？
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool IsPass(string input, FilterType type)
        {
            if (!isLoaded || type == FilterType.None || string.IsNullOrWhiteSpace(input)) return true;
            bool result = true;

            switch (type)
            {
                case FilterType.None:
                    result = true;
                    break;
                case FilterType.Normal:
                case FilterType.Strict:
                    string res = Filting(input, type);
                    if (res != input)
                    {
                        // filted!
                        Logger.Instance.Log($"以拦截！{input} => {res}", LogType.Debug);
                    }
                    result = (input == res);
                    break;
                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// 过滤文本，返回过滤后的文本内容
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string Filting(string input, FilterType type)
        {
            if (!isLoaded || type == FilterType.None || string.IsNullOrWhiteSpace(input)) return input;

            List<char> output = new List<char>();
            int index = 0;

            while (index < input.Length)
            {
                
                TrieNode node = null;
                if (FilterType.Normal == type)
                {
                    node = rootNormal;
                }else if(FilterType.Strict == type)
                {
                    node = rootStrict;
                }
                else
                {// error
                    return input;
                }
                int matchLength = 0;
                string replacement = null;

                for (int i = index; i < input.Length; i++)
                {
                    if (!node.Children.ContainsKey(input[i]))
                    {
                        break;
                    }
                    node = node.Children[input[i]];

                    if (node.IsEndOfWord)
                    {
                        matchLength = i - index + 1;
                        replacement = node.Replacement;
                    }
                }

                if (matchLength > 0)
                {
                    // 如果有替换词，则替换；否则直接跳过匹配部分（相当于删除）
                    if (replacement != null)
                    {
                        output.AddRange(replacement);
                    }
                    index += matchLength; // 跳过匹配的部分
                }
                else
                {
                    output.Add(input[index]);
                    index++;
                }
            }
            string res = new string(output.ToArray());
            //if(res != input)
            //{
            //    Logger.Instance.Log($"以过滤！{input} => {res}", LogType.Debug);
            //}
            return res;
        }

    }
}
