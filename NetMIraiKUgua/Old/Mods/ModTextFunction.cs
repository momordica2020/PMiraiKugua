using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using MMDK.Util;
using static System.Windows.Forms.LinkLabel;

namespace MMDK.Mods
{
    /// <summary>
    /// 文本应答的一些功能
    /// </summary>
    public class ModTextFunction : Mod
    {

        #region 语料
        //string duiP2f = "pairc2.txt";
        //string duiP1f = "pairc.txt";
        //Dictionary<string, string[]> cf = new Dictionary<string, string[]>();
        //Dictionary<string, string[]> cf2 = new Dictionary<string, string[]>();

        string randomch = "随机-随机汉字.txt";
        string randomChar = "";

        string gongshouName = "gongshou.txt";
        List<string> gongshou = new List<string>();

        string qianzeName = "gengshuang.txt";
        List<string> qianze1 = new List<string>();
        List<string> qianze2 = new List<string>();

        string jokeName = "jokes.txt";
        List<string> jokes = new List<string>();
        List<string> jokesEvent = new List<string>();
        List<string> jokesOrg = new List<string>();
        List<string> jokesEnemy = new List<string>();

        string junkf = "spam.txt";
        List<List<string>> junks = new List<List<string>>();

        string symbolf = "symboltemplate.txt";
        Dictionary<string, List<string>> symbollist = new Dictionary<string, List<string>>();
        #endregion



        public bool Init(string[] args)
        {

            string PluginPath = Config.Instance.ResourceFullPath("ModePath");
            randomChar = FileManager.Read($"{PluginPath}/{randomch}").Trim();

            // gongshou
            gongshou = new List<string>();
            var res = FileManager.ReadLines($"{PluginPath}/{gongshouName}");
            string thistmp = "";
            foreach (var line in res)
            {
                if (line.Trim() == "$$$$$$$$" && !string.IsNullOrWhiteSpace(thistmp))
                {
                    gongshou.Add(thistmp);
                    thistmp = "";
                }
                else
                {
                    thistmp += line + "\r\n";
                }
            }
            if (!string.IsNullOrWhiteSpace(thistmp)) gongshou.Add(thistmp);

            // qianze
            qianze1 = new List<string>();
            qianze2 = new List<string>();
            int pos = 0;
            res = FileManager.ReadLines($"{PluginPath}/{qianzeName}");
            foreach (var line in res)
            {
                if (line.Trim().StartsWith("#1"))
                {
                    pos = 1;
                    continue;
                }
                else if (line.Trim().StartsWith("#2"))
                {
                    pos = 2;
                    continue;
                }

                if (pos == 1) qianze1.Add(line.Trim());
                else if (pos == 2) qianze2.Add(line.Trim());
            }

            // joke
            jokes = new List<string>();
            jokesOrg = new List<string>();
            jokesEvent = new List<string>();
            jokesEnemy = new List<string>();
            res = FileManager.ReadLines($"{PluginPath}/{jokeName}");
            string tmpline = "";
            foreach (var line in res)
            {
                if (line.Trim().StartsWith("#"))
                {
                    if (!string.IsNullOrEmpty(tmpline))
                    {
                        bool find = false;
                        if (tmpline.Contains("【部门】")) { jokesOrg.Add(tmpline); find = true; }
                        if (tmpline.Contains("【事件】")) { jokesEvent.Add(tmpline); find = true; }
                        if (tmpline.Contains("【敌国】")) { jokesEnemy.Add(tmpline); find = true; }
                        if (!find) jokes.Add(tmpline);
                    }
                    tmpline = "";
                    continue;
                }
                else
                {
                    tmpline += $"{line.Trim()}\r\n";
                }
            }


            string[] lines;
            //// duilian
            //var lines = FileManager.readLines($"{PluginPath}/{duiP1f}");
            //foreach (var line in lines)
            //{
            //    var items = line.Split('\t');
            //    var items2 = items[1].Split(',');
            //    cf[items[0]] = items2;
            //}
            //lines = FileManager.readLines($"{PluginPath}/{duiP2f}");
            //foreach (var line in lines)
            //{
            //    var items = line.Split('\t');
            //    var items2 = items[1].Split(',');
            //    cf2[items[0]] = items2;
            //}


            // junk
            if (File.Exists($"{PluginPath}/{junkf}"))
            {
                lines = File.ReadAllLines($"{PluginPath}/{junkf}", Encoding.UTF8);

                List<string> nowline = new List<string>();
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (nowline.Count > 0)
                        {
                            junks.Add(nowline);
                            nowline = new List<string>();
                        }
                    }
                    else
                    {
                        nowline.Add(line.Trim());
                    }
                }
                if (nowline.Count > 0)
                {
                    junks.Add(nowline);
                }
            }


            // symbols
            lines = FileManager.ReadLines($"{PluginPath}/{symbolf}");
            symbollist = new Dictionary<string, List<string>>();
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("/"))
                {
                    var items = line.Trim().Split('\t');
                    if (items.Length >= 2)
                    {
                        if (!symbollist.ContainsKey(items[0])) symbollist[items[0]] = new List<string>();
                        symbollist[items[0]].Add(items[1]);
                    }
                }
            }

            return true;
        }

        public void Exit()
        {
            
        }

        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            message = message.Trim();

            CommandType cmd = CommandType.None;
            List<string> param = new List<string>();
            bool isGroup = groupId > 0;
            var user = Config.Instance.UserInfo(userId);
            var group = Config.Instance.GroupInfo(groupId);
            if (TryReadCommand(message, out cmd, out param))
            {
                switch (cmd)
                {
                    case CommandType.Reverse:
                        // 翻转字符串
                        if (param.Count > 0)
                        {
                            char[] charArray = param.First().ToCharArray(); 
                            Array.Reverse(charArray);
                            results.Add(new string(charArray));
                            return true;
                        }
                        return false;
                        
                    case CommandType.Shuffle:
                        // 乱序字符串
                        if (param.Count == 1)
                        {
                            // 彻底打乱
                            results.Add(ShuffleString(param.First()));
                            return true;
                        }else if(param.Count == 3)
                        {
                            // K切多次
                            int runTime = 0;
                            int cutNum = 0;
                            if (!int.TryParse(param[1], out cutNum)) return false;
                            if (cutNum < 1) return false;
                            if (!int.TryParse(param[2], out runTime)) return false;
                            if (runTime < 1) return false;
                            runTime = Math.Min(runTime, 5);
                            for(int i = 0; i < runTime; i++)
                            {
                                results.Add(ShuffleString(param[0], cutNum));
                            }
                            
                            return true;
                        }
                        else if (param.Count == 2)
                        {
                            // K切
                            int cutNum = 0;
                            if (!int.TryParse(param[1], out cutNum)) return false;
                            if (cutNum < 1) return false;
                            results.Add(ShuffleString(param[0], cutNum));
                            return true;
                        }
                        return false;
                        
                    //case CommandType.Couplet:
                        // 对对联
                        //if (param.Count > 0)
                        //{
                        //    results.Add(getDui(param.First()));
                        //    return true;
                        //}
                        return false;
                    case CommandType.LoremIpsum:
                        // 乱数假文，目前是随机汉字
                        if (param.Count > 0)
                        {
                            var regex = new Regex(@"(\d+)(?:\*(\d+))?");
                            var match = regex.Match(param.First().Trim());

                            if (match.Success)
                            {
                                // 提取行数和列数
                                int rows = int.Parse(match.Groups[1].Value);
                                int columns = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1; // 如果没有列数，默认为1
                                if(rows<1 || columns<1 || rows > 100 || columns > 100 || rows * columns > 2000)
                                {
                                    results.Add($"输入太多，溢出来了！");
                                    return true;
                                }
                                // 生成随机字符串
                                results.Add(GenerateRandomStringHans(rows, columns));
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        return false;
                    case CommandType.WordSalad:
                        // 营销号
                        if (param.Count > 0 && !string.IsNullOrWhiteSpace(param.First()))
                        {
                            string WordSaladresKey = param.First().TrimEnd('？', '?');                                
                            string WordSaladres = GetWordSalad(WordSaladresKey);
                            if (!string.IsNullOrWhiteSpace(WordSaladres))
                            {
                                WordSaladres = WordSaladres.Replace("【D】", Config.Instance.App.Avatar.myQQ.ToString()).Replace("【C】", Config.Instance.App.Avatar.askName);
                                results.Add(WordSaladres);
                                return true;
                            }
                        }
                        return false;
                    case CommandType.Joke:
                        // 苏联笑话
                        if (param.Count > 0 && !string.IsNullOrWhiteSpace(param.First()))
                        {
                            string Jokeres = "";
                            try
                            {
                                // 笑话输入格式：“事件：A，好人：B，坏人：C，地点：D”
                                var items = param.First().Split(new char[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                if (items.Length >= 1)
                                {
                                    Dictionary<string, string> pairs = new Dictionary<string, string>();
                                    foreach (var item in items)
                                    {
                                        var pair = item.Split(new char[] { ':', '：', '=', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                        if (pair.Length == 2) pairs[pair[0].Trim()] = pair[1].Trim();
                                    }
                                    if (pairs.Count > 0)
                                    {
                                        Jokeres = getJoke(pairs);
                                    }
                                }
                            } catch { }
                            if (!string.IsNullOrWhiteSpace(Jokeres))
                            {
                                results.Add(Jokeres);
                                return true;
                            }
                        }
                        return false;
                    case CommandType.BL:
                        // 攻受文
                        if (param.Count >= 2 && !string.IsNullOrWhiteSpace(param[0]) && !string.IsNullOrWhiteSpace(param[1]))
                        {
                            results.Add(getGongshou(param[0], param[1]));
                            return true;
                        }
                        return false;
                    case CommandType.None:
                    default:
                        return false;
                }
            }
            return false;
        }





        //bool isAskme(string msg)
        //{
        //    if (msg.StartsWith(Config.Instance.App.Avatar.askName))
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        enum CommandType
        {
            None,
            Shuffle,    // 洗牌，随机重排给定文本
            Reverse,    // 倒序输出给定文本
            //Couplet,    // 对联
            WordSalad,  // 生成营销号文章
            LoremIpsum, // 生成指定长度的乱数假文
            BL,         // 生成攻受文
            Joke,       // 生成苏联笑话
            //Symbol,     // 符号类生成器

        }
        // 存储可匹配的命令
        private static readonly Dictionary<string, CommandType> CommandDict = new Dictionary<string, CommandType>
        {
            { "乱序", CommandType.Shuffle},
            { "反转", CommandType.Reverse},
            //{ "上联", CommandType.Couplet},
            { "什么是", CommandType.WordSalad},
            { "随机", CommandType.LoremIpsum},
            { "讽刺", CommandType.Joke},
            // CommandType.BL 攻受文不是前缀，所以单独触发
        };
        static bool TryReadCommand(string input, out CommandType commandType, out List<string> param)
        {
            param = new List<string>();
            // 前缀类型的指令，查找并截断前缀
            foreach (var command in CommandDict)
            {
                if (input.StartsWith(command.Key))
                {
                    commandType = command.Value; // 找到的命令
                    param.Add(input.Substring(command.Key.Length).Trim());// 截掉命令部分并去掉前导空格
                    return true;
                }
            }

            // 这里处理其余特殊匹配
            Regex reg = new Regex("(.+)攻(.+)受");
            var match = reg.Match(input);
            if (match.Success)
            {

                string sA = match.Groups[1].Value.Trim();
                string sB = match.Groups[2].Value.Trim();
                param.Add(sA);
                param.Add(sB);
                commandType = CommandType.BL;
                return true;

            }

            // k切
            reg = new Regex(@"(\d+)切(\d+)次(.+)", RegexOptions.Singleline);
            match = reg.Match(input);
            if (match.Success)
            {
                string sTarget = match.Groups[3].Value.Trim();
                string sTime = match.Groups[2].Value.Trim();
                string sNum = match.Groups[1].Value.Trim();
                param.Add(sTarget);
                param.Add(sNum);
                param.Add(sTime);
                commandType = CommandType.Shuffle;
                return true;
            }
            reg = new Regex(@"(\d+)切(.+)", RegexOptions.Singleline);
            match = reg.Match(input);
            if (match.Success)
            {

                string sTarget = match.Groups[2].Value.Trim();
                string sNum = match.Groups[1].Value.Trim();
                param.Add(sTarget);
                param.Add(sNum);
                commandType = CommandType.Shuffle;
                return true;

            }
            // - 符号类


            // 如果没有匹配的命令
            commandType = CommandType.None; 
            return false;
        }



        //public string getDui(string sin)
        //{

        //    sin = sin.Trim();
        //    string sout = "";

        //    for (int i = 0; i < sin.Length; i++)
        //    {
        //        if (i + 1 < sin.Length && cf2.ContainsKey(sin.Substring(i, 2)))
        //        {
        //            sout += cf2[sin.Substring(i, 2)][rand.Next(cf2[sin.Substring(i, 2)].Length)];
        //            i += 1;
        //        }
        //        else if (cf.ContainsKey(sin[i].ToString()))
        //        {
        //            sout += cf[sin[i].ToString()][rand.Next(cf[sin[i].ToString()].Length)];
        //        }
        //        //else if("３")
        //        else if ("123456789".Contains(sin[i]))
        //        {
        //            sout = $"{sout}{10 - int.Parse(sin[i].ToString())}";
        //        }
        //        else if ("abcdefghijklmnopqrstuvwxyz".Contains(sin[i]))
        //        {
        //            sout += "abcdefghijklmnopqrstuvwxyz"[rand.Next(26)];
        //        }
        //        else if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(sin[i]))
        //        {
        //            sout += "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[rand.Next(26)];
        //        }
        //        else if ("あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽ".Contains(sin[i]))
        //        {
        //            sout += "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽ"[rand.Next(71)];
        //        }
        //        else if ("アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポ".Contains(sin[i]))
        //        {
        //            sout += "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポ"[rand.Next(71)];
        //        }
        //        else
        //        {
        //            sout += sin[i];
        //        }
        //    }
        //    return sout;




        //}




        public void FisherYates(char[] input)
        {
            // Fisher-Yates 洗牌算法，完全打乱
            for (int i = input.Length - 1; i > 0; i--)
            {
                int j = MyRandom.Next(0, i + 1); // 生成随机索引
                                             // 交换
                char temp = input[i];
                input[i] = input[j];
                input[j] = temp;
            }
        }
        public void FisherYates(string[] input)
        {
            // Fisher-Yates 洗牌算法，完全打乱
            for (int i = input.Length - 1; i > 0; i--)
            {
                int j = MyRandom.Next(0, i + 1); // 生成随机索引
                                             // 交换
                string temp = input[i];
                input[i] = input[j];
                input[j] = temp;
            }
        }
        public void FisherYates(bool[] input)
        {
            // Fisher-Yates 洗牌算法，完全打乱
            for (int i = input.Length - 1; i > 0; i--)
            {
                int j = MyRandom.Next(0, i + 1); // 生成随机索引
                                             // 交换
                bool temp = input[i];
                input[i] = input[j];
                input[j] = temp;
            }
        }
        /// <summary>
        /// 洗牌算法
        /// </summary>
        /// <param name="str">需打乱的字符串</param>
        /// <returns>打乱结果</returns>
        public string ShuffleString(string str, int time = 0)
        {
            if (string.IsNullOrWhiteSpace(str)) return str;
            


            if(time < 1)
            {
                char[] array = str.ToCharArray(); // 将字符串转换为字符数组
                FisherYates(array);
                return new string(array); // 返回新的乱序字符串
            }
            else
            {
                // 只随机切牌time轮  算法3 - 不均匀切
                time = Math.Min(time, str.Length - 1);
                bool[] cuts = new bool[str.Length - 1];

                var stringBuilder = new StringBuilder();
                for (int i = 0; i < cuts.Length; i++) cuts[i] = (i < time);
                FisherYates(cuts);
                List<string> parts = new List<string>();

                // 切割字符串
                int startIndex = 0;
                for (int i = 1; i < str.Length; i++)
                {
                    if (cuts[i-1])
                    {
                        parts.Add(str.Substring(startIndex, i - startIndex));
                        startIndex = i;
                    }
                    
                }
                parts.Add(str.Substring(startIndex));
                var pparts = parts.ToArray();
                FisherYates(pparts);
                return string.Concat(pparts);

                //// 只随机切牌time轮  算法2 - 均匀切
                //time = Math.Min(time, str.Length);
                //int partLength = str.Length / time;
                //List<string> parts = new List<string>();

                //// 切割字符串
                //for (int i = 0; i < time; i++)
                //{
                //    // 计算切割的开始和结束索引
                //    int startIndex = i * partLength;
                //    // 处理最后一部分，确保包含所有剩余字符
                //    int length = (i == time - 1) ? str.Length - startIndex : partLength;

                //    // 提取子字符串并添加到列表中
                //    parts.Add(str.Substring(startIndex, length));
                //}

                //// 打乱切割后的部分
                //List<string> shuffledParts = parts.OrderBy(x => rand.Next()).ToList();

                //// 合并打乱后的部分为最终字符串
                //return string.Concat(shuffledParts);

                //// 只随机切牌time轮 算法1 - 切后拼后切
                //for (int i = 0; i < Math.Min(time, str.Length*2); i++)
                //{
                //    int cutPosition = rand.Next(1, str.Length); 
                //    string leftPart = str.Substring(0, cutPosition);
                //    if (rand.Next(0, 2) > 0) leftPart = new string(leftPart.Reverse().ToArray());
                //    string rightPart = str.Substring(cutPosition);
                //    if (rand.Next(0, 2) > 0) rightPart = new string(rightPart.Reverse().ToArray());
                //    str = rightPart + leftPart;
                //}
                //// 合并打乱后的部分为最终字符串
                //return str;
            }
        }


        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="rows">行数</param>
        /// <param name="columns">列数</param>
        /// <returns>生成的字符串</returns>
        string GenerateRandomStringHans(int rows, int columns)
        {
            if (string.IsNullOrWhiteSpace(randomChar)) return "";
            StringBuilder sb = new StringBuilder();

            // 生成随机字符
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    char rc = randomChar[MyRandom.Next(randomChar.Length)]; 
                    sb.Append(rc);
                }
                sb.AppendLine(); 
            }

            return sb.ToString();
        }

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="rows">行数</param>
        /// <param name="columns">列数</param>
        /// <returns>生成的字符串</returns>
        string GenerateRandomString(int rows, int columns)
        {
            StringBuilder sb = new StringBuilder();
            Random random = new Random();

            // 生成随机字符
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    // 生成随机字符（可以根据需求修改字符范围）
                    char randomChar = (char)random.Next('A', 'Z' + 1); // 生成大写字母
                    sb.Append(randomChar);
                }
                sb.AppendLine(); // 每行结束后换行
            }

            return sb.ToString();
        }







        /// <summary>
        /// 营销号文章套模板
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetWordSalad(string key)
        {
            string result = "";

            try
            {
                foreach (var para in junks)
                {
                    if (para.Count > 0)
                    {
                        result += para[MyRandom.Next(para.Count)] + "\r\n";
                    }
                }
                result = result.Replace("【E】", DateTime.Now.Year.ToString());
                result = result.Replace("【B】", new string[] { "朋友", "小伙伴", "网友" }[MyRandom.Next(3)]);
                result = result.Replace("【A】", key);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex.Message + "\r\n" + ex.StackTrace);
            }


            return result;
        }


        /// <summary>
        /// 模板生成攻受文
        /// </summary>
        /// <param name="gong"></param>
        /// <param name="shou"></param>
        /// <returns></returns>
        public string getGongshou(string gong, string shou)
        {
            string result = "";

            try
            {
                if (!string.IsNullOrWhiteSpace(gong) && !string.IsNullOrWhiteSpace(shou) && gongshou.Count > 0)
                {
                    result = gongshou[MyRandom.Next(gongshou.Count)];
                    result = result.Replace("<攻>", gong).Replace("<受>", shou);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex.Message + "\r\n" + ex.StackTrace);
            }



            return result;
        }

        public string getJoke(Dictionary<string, string> pairs)
        {
            string result = "";

            try
            {
                List<string> usingjokes = new List<string>();
                if (pairs.ContainsKey("敌国")) usingjokes.AddRange(jokesEnemy);
                if (pairs.ContainsKey("部门")) usingjokes.AddRange(jokesOrg);
                if (pairs.ContainsKey("事件")) usingjokes.AddRange(jokesEvent);
                if (usingjokes.Count <= 0) usingjokes.AddRange(jokes);
                int find = 100;
                int index = MyRandom.Next(usingjokes.Count);
                do
                {
                    result = usingjokes[index];
                    foreach (var pair in pairs)
                    {
                        result = result.Replace($"【{pair.Key}】", pair.Value);
                    }
                    if (result.Contains("【"))
                    {
                        index = (index + 1) % usingjokes.Count;
                        find -= 1;
                    }
                    else
                    {
                        break;
                    }
                } while (find >= 0);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex.Message + "\r\n" + ex.StackTrace);
            }



            return result;
        }








        public string getSymbolDeal(string str)
        {
            string res = "";

            try
            {
                foreach (var sb in symbollist)
                {
                    if (str.StartsWith(sb.Key))
                    {
                        str = str.Substring(sb.Key.Length);
                        if (string.IsNullOrWhiteSpace(str)) return "";

                        var temp = sb.Value[MyRandom.Next(sb.Value.Count)];
                        if (temp.StartsWith("【W】"))     // num and english char
                        {
                            // total 10 + 26 + 26 = 62
                            temp = temp.Substring(3);
                            int singnum = temp.Length / 62;
                            foreach (var ch in str)
                            {
                                try
                                {
                                    int index = -1;
                                    if (ch >= '0' && ch <= '9') index = ch - '0';// res += temp[(int)(ch - '0')];
                                    else if (ch >= 'a' && ch <= 'z') index = 10 + ch - 'a';
                                    else if (ch >= 'A' && ch <= 'Z') index = 36 + ch - 'A';
                                    if (index < 0 || singnum <= 0)
                                    {
                                        res += ch;
                                    }
                                    else
                                    {
                                        res += temp.Substring(index * singnum, singnum);
                                    }
                                }
                                catch
                                {
                                    res += ch;
                                }
                            }
                        }
                        else if (temp.StartsWith("【N】"))        // just num
                        {
                            temp = temp.Substring(3);
                            int maxnum = temp.Length - 1;
                            int trywholenum = -1;
                            int.TryParse(str, out trywholenum);
                            if (trywholenum >= 0 && trywholenum <= maxnum)
                            {
                                // whole num single sym
                                res = temp[trywholenum].ToString();
                            }
                            else
                            {
                                // each num single char
                                foreach (var ch in str)
                                {
                                    try
                                    {
                                        int index = -1;
                                        if (ch >= '0' && ch <= '9') index = ch - '0';
                                        if (index < 0)
                                        {
                                            res += ch;
                                        }
                                        else
                                        {
                                            res += temp.Substring(index, 1);
                                        }
                                    }
                                    catch
                                    {
                                        res += ch;
                                    }
                                }
                            }
                        }
                        else if (temp.StartsWith("【E】"))        // english char
                        {
                            // total 26 + 26 = 52
                            temp = temp.Substring(3);
                            int singnum = temp.Length / 52;
                            if (sb.Key.Contains("空心字母")) singnum = 4;
                            foreach (var ch in str)
                            {
                                try
                                {
                                    int index = -1;
                                    if (ch >= 'a' && ch <= 'z') index = ch - 'a';
                                    else if (ch >= 'A' && ch <= 'Z') index = 26 + ch - 'A';
                                    if (index < 0 || singnum <= 0)
                                    {
                                        res += ch;
                                    }
                                    else
                                    {

                                        res += temp.Substring(index * singnum, singnum);
                                    }
                                }
                                catch
                                {
                                    res += ch;
                                }
                            }
                        }
                        else if (temp.Contains("阿"))          // single word repeat
                        {
                            foreach (var ch in str)
                            {
                                try
                                {
                                    res += temp.Replace('阿', ch);
                                }
                                catch
                                {
                                    res += ch;
                                }
                            }
                        }
                        else if (temp.Contains("【1】"))
                        {
                            if (temp.Contains("【2】"))
                            {
                                // double content
                                res = temp.Replace("【1】", str.Substring(0, str.Length / 2)).Replace("【2】", str.Substring(str.Length / 2));

                            }
                            else
                            {
                                // single content
                                res = temp.Replace("【1】", str);
                            }
                        }


                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex.Message + "\r\n" + ex.StackTrace);
            }




            return res;
        }







    }
}
