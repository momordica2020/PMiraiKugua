using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatGPT.Net;
using ChatGPT.Net.DTO.ChatGPT;
using MMDK.Util;
using SuperSocket.ClientEngine;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MMDK.Mods
{
    /// <summary>
    /// 随机回复，龚诗似的
    /// </summary>
    public class ModRandomChat : Mod
    {
        //private static readonly Lazy<ModRandomChat> instance = new Lazy<ModRandomChat>(() => new ModRandomChat());
        //public static ModRandomChat Instance => instance.Value;


        //private ModRandomChat()
        //{


        //}

        public string replacefile = "replacewords.txt";
        string modeIndexName = "_index.txt";
        //string modePrivateName = "_mode_private.txt";
        //string modeGroupName = "_mode_group.txt";
        string defaultAnswerName = "_defaultanswer.txt";
        string PluginPath;

        Dictionary<string, string> wordReplace = new Dictionary<string, string>();
        Dictionary<string, ModeInfo> modedict = new Dictionary<string, ModeInfo>();
        List<string> defaultAnswers = new List<string>();
        //public Dictionary<long, string> privatemode = new Dictionary<long, string>();
        //public Dictionary<long, string> groupmode = new Dictionary<long, string>();
        MD5 md5 = MD5.Create();

        string chaosv = "混沌-名词.txt";
        string chaosm = "混沌-情绪词.txt";
        string chaosw = "混沌-小万邦部分.txt";
        List<string[]> chaosWord = new List<string[]>();
        List<string> chaosMotion = new List<string>();
        List<string> chaosXwb = new List<string>();

        string yunjief = "云杰说道.txt";
        List<string> yjsd = new List<string>();



        string penName = "pen.txt";
        List<string> penlist = new List<string>();






        string picsave = "picsave.txt";
        string piclingtang = "lingtang.jpg";
        List<string> pics = new List<string>();



        public bool Init(string[] args)
        {
            try
            {

                string PluginPath = Config.Instance.ResourceFullPath("ModePath");

                // pen
                penlist = FileManager.ReadLines($"{PluginPath}/{penName}").ToList();










                //// cangtou
                //lines = FileHelper.readLines(path + pyf, Encoding.UTF8);
                //foreach (var line in lines)
                //{
                //    var items = line.Trim().Split(' ');
                //    if (items.Length >= 2)
                //    {
                //        char ch = items[0][0];
                //        for (int i = 1; i < items.Length; i++)
                //        {
                //            string pyall = items[i];
                //            string pyori = pyall;
                //            if ("12345".Contains(pyori.Last()))
                //            {
                //                pyori = pyori.Substring(0, pyori.Length - 1);
                //            }
                //            if (!py.ContainsKey(ch)) py[ch] = new List<string>();
                //            py[ch].Add(pyall);
                //            //py[ch]
                //        }
                //    }
                //}
                //lines = FileHelper.readLines(path + cangtou5f, Encoding.UTF8);
                //foreach (var line in lines)
                //{
                //    var ttmp = line.Trim();
                //    if (ttmp.Length > 0)
                //    {
                //        char targetch = ttmp[0];
                //        if (!cangtou5.ContainsKey(targetch)) cangtou5[targetch] = new List<string>();
                //        cangtou5[targetch].Add(ttmp);
                //        if (py.ContainsKey(targetch))
                //        {
                //            foreach (var pyi in py[targetch])
                //            {
                //                if (!cangtou5py.ContainsKey(pyi)) cangtou5py[pyi] = new List<string>();
                //                cangtou5py[pyi].Add(ttmp);
                //                string pyiori = pyi.Substring(0, pyi.Length - 1);
                //                if (!cangtou5py.ContainsKey(pyiori)) cangtou5py[pyiori] = new List<string>();
                //                cangtou5py[pyiori].Add(ttmp);
                //            }
                //        }
                //        targetch = ttmp[ttmp.Length - 1];
                //        if (!cangwei5.ContainsKey(targetch)) cangwei5[targetch] = new List<string>();
                //        cangwei5[targetch].Add(ttmp);
                //        if (py.ContainsKey(targetch))
                //        {
                //            foreach (var pyi in py[targetch])
                //            {
                //                if (!cangwei5py.ContainsKey(pyi)) cangwei5py[pyi] = new List<string>();
                //                cangwei5py[pyi].Add(ttmp);
                //                string pyiori = pyi.Substring(0, pyi.Length - 1);
                //                if (!cangwei5py.ContainsKey(pyiori)) cangwei5py[pyiori] = new List<string>();
                //                cangwei5py[pyiori].Add(ttmp);
                //            }
                //        }
                //    }
                //}
                //lines = FileHelper.readLines(path + cangtou7f, Encoding.UTF8);
                //foreach (var line in lines)
                //{
                //    var ttmp = line.Trim();
                //    if (ttmp.Length > 0)
                //    {
                //        char targetch = ttmp[0];
                //        if (!cangtou7.ContainsKey(targetch)) cangtou7[targetch] = new List<string>();
                //        cangtou7[targetch].Add(ttmp);
                //        if (py.ContainsKey(targetch))
                //        {
                //            foreach (var pyi in py[targetch])
                //            {
                //                if (!cangtou7py.ContainsKey(pyi)) cangtou7py[pyi] = new List<string>();
                //                cangtou7py[pyi].Add(ttmp);
                //                string pyiori = pyi.Substring(0, pyi.Length - 1);
                //                if (!cangtou7py.ContainsKey(pyiori)) cangtou7py[pyiori] = new List<string>();
                //                cangtou7py[pyiori].Add(ttmp);
                //            }
                //        }
                //        targetch = ttmp[ttmp.Length - 1];
                //        if (!cangwei7.ContainsKey(targetch)) cangwei7[targetch] = new List<string>();
                //        cangwei7[targetch].Add(ttmp);
                //        if (py.ContainsKey(targetch))
                //        {
                //            foreach (var pyi in py[targetch])
                //            {
                //                if (!cangwei7py.ContainsKey(pyi)) cangwei7py[pyi] = new List<string>();
                //                cangwei7py[pyi].Add(ttmp);
                //                string pyiori = pyi.Substring(0, pyi.Length - 1);
                //                if (!cangwei7py.ContainsKey(pyiori)) cangwei7py[pyiori] = new List<string>();
                //                cangwei7py[pyiori].Add(ttmp);
                //            }
                //        }
                //    }
                //}

                //// szm
                //lines = File.ReadAllLines(path + szmf, Encoding.UTF8);
                //foreach (var line in lines)
                //{
                //    var items = line.Trim().Split('\t');
                //    if (items.Length >= 2)
                //    {
                //        if (!szm.ContainsKey(items[1])) szm[items[1]] = new List<string>();
                //        szm[items[1]].Add(items[0]);
                //    }

                //}
                // load modes
                
                modedict = new Dictionary<string, ModeInfo>();
                List<string> modelines = FileManager.ReadLines($"{PluginPath}/{modeIndexName}").ToList();
                foreach (var line in modelines)
                {
                    var items = line.Split('\t');
                    string modeName = items[0].Trim();
                    try
                    {
                        string[] modeConfigs;
                        if (items.Length >= 2)
                        {
                            modeConfigs = items[1].Trim().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        }
                        else
                        {
                            modeConfigs = new string[1] { "默认" };
                        }
                        modedict[modeName] = new ModeInfo(modeName, modeConfigs, FileManager.ReadLines($"{PluginPath}/{modeName}.txt").ToList());
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Log($"模式行[ {line} ]加载失败，{ex.Message}\r\n{ex.StackTrace}");
                    }
                }
                modedict["测试"] = new ModeInfo { name = "测试", config = { "隐藏" } };
                modedict["AI"] = new ModeInfo { name = "AI", config = { "隐藏" } };
                modedict["喷人"] = new ModeInfo { name = "喷人", config = { "隐藏" } };
                modedict["语音"] = new ModeInfo { name = "语音", config = { "隐藏" } };
                // replace
                wordReplace = new Dictionary<string, string>();
                var lines = FileManager.ReadLines($"{PluginPath}/{replacefile}");
                foreach (var line in lines)
                {
                    var items = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (items.Length >= 2)
                    {
                        wordReplace[items[1]] = items[0];
                    }
                }


                //// group mode config
                //groupmode = new Dictionary<long, string>();
                //List<string> grouplines = FileUtil.readLines($"{PluginPath}/{modeGroupName}").ToList();
                //foreach (var line in grouplines)
                //{
                //    var items = line.Split('\t');
                //    if (items.Length >= 2)
                //    {
                //        groupmode[long.Parse(items[0])] = items[1].Trim();
                //    }
                //}
                //// private mode config
                //privatemode = new Dictionary<long, string>();
                //List<string> privatelines = FileUtil.readLines($"{PluginPath}/{modePrivateName}").ToList();
                //foreach (var line in privatelines)
                //{
                //    var items = line.Split('\t');
                //    if (items.Length >= 2)
                //    {
                //        privatemode[long.Parse(items[0])] = items[1].Trim();
                //    }
                //}

                // motions
                chaosMotion = FileManager.ReadLines($"{PluginPath}/{chaosm}").ToList();
                // verb
                var wordlines = FileManager.ReadLines($"{PluginPath}/{chaosv}").ToList();
                foreach (var line in wordlines)
                {
                    chaosWord.Add(line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
                // xwb
                chaosXwb = FileManager.ReadLines($"{PluginPath}/{chaosw}").ToList();

                // yunjieshuodao
                yjsd = FileManager.ReadLines($"{PluginPath}/{yunjief}").ToList();

                // random


                // default
                defaultAnswers = FileManager.ReadLines($"{PluginPath}/{defaultAnswerName}").ToList();

                // pics
                pics = FileManager.ReadLines($"{PluginPath}/{picsave}").ToList();


                //new Thread(workInitModes).Start();
            }
            catch (Exception e)
            {
                Logger.Instance.Log(e);
            }


            return true;
        }

        public void Exit()
        {
            try
            {
                FileManager.writeLines($"{PluginPath}/{picsave}", pics);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
        }


        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {
            message = message.Trim();

            


            if (message.StartsWith("模式列表"))
            {
                string modeindexs = printModeList();
                modeindexs += "~输入“xx模式on”即可切换模式~";

                results.Add(modeindexs);
                return true;
            }

            bool isGroup = groupId > 0;
            var user = Config.Instance.UserInfo(userId);
            var group = Config.Instance.GroupInfo(groupId);
            Regex modereg = new Regex(@"(\S+)模式\s*(on)", RegexOptions.IgnoreCase);
            var moderes = modereg.Match(message);
            if (moderes.Success)
            {
                try
                {
                    if (moderes.Groups.Count < 2) { return false; } // 好像不可能小于2？
                    string modeName = moderes.Groups[1].ToString().Trim();
                    if (modeName.EndsWith("模式")) modeName = modeName.Replace("模式", "");
                    if (string.IsNullOrWhiteSpace(modeName))
                    {
                        // 输入不合法
                        results.Add(printModeList());
                        return true;
                    }
                    //ModeInfo mode = null;
                    if (modedict.TryGetValue(modeName, out ModeInfo mode))
                    {
                        // 模式存在
                        if (mode.config.Contains("隐藏"))
                        {
                            // 隐藏模式，且没有相应权限就不启动
                            if (
                                (isGroup && !GroupHasAdminAuthority(groupId))
                                
                                //||(!isGroup && !UserHasAdminAuthority(groupId))
                                ) {
                                results.Add(printModeList());
                                return true;
                            }
                            if ((!isGroup))
                            {
                                // allowed
                            }
                        }
                        // 切换模式tag
                        if (isGroup)
                        {
                            // group
                            group.Tags.RemoveWhere(t => t.EndsWith("模式"));
                            group.Tags.Add($"{mode.name}模式");
                        }
                        else
                        {
                            // private
                            user.Tags.RemoveWhere(t => t.EndsWith("模式"));
                            user.Tags.Add($"{mode.name}模式");
                        }

                        results.Add($"~{Config.Instance.App.Avatar.askName}的{mode.name}模式启动~");
                        return true;
                        
                    }
                    else
                    {
                        // 没有这个模式
                        results.Add($"~我还没有{modeName}模式~");
                        results.Add(printModeList());
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(ex);
                }
            }

            // 以下部分无需输入任何内容即可触发！！！！！！！！！！！！！1111111
            
            ModeInfo modeTrigger = null;
            if (isGroup)
            {
                // 群内
                modeTrigger = getGroupMode(group);
            }
            else
            {
                // 私聊发言
                modeTrigger = getUserMode(user);
            }
            if (modeTrigger == null)
            {
                // 没找到模式
                return false;
            }
            else
            {
                if(handleChatResults(modeTrigger, userId, groupId, message, out IEnumerable<string> chatResult))
                {
                    results.AddRange(chatResult);
                    return true;
                }
            }




            return false;
        }







        private bool UserBanned(long userId)
        {
            var user = Config.Instance.UserInfo(userId);
            if (user.Is("黑名单")) return true;
            if (user.Type == PlayerType.Blacklist) return true;
            return false;

        }

        private bool GroupBanned(long groupId)
        {
            var group = Config.Instance.GroupInfo(groupId);
            if (group.Is("黑名单")) return true;
            if (group.Type == PlaygroupType.Blacklist) return true;
            return false;

        }

        /// <summary>
        /// 按模式处理输入并返回结果串
        /// 里面每个模式的返回值都不应为null
        /// </summary>
        /// <param name="mode">模式对象</param>
        /// <param name="userId"></param>
        /// <param name="groupId"></param>
        /// <param name="inputText"></param>
        /// <param name="results">输出结果</param>
        /// <returns>若结果不空，返回true，空则返回false</returns>

        bool handleChatResults(ModeInfo mode, long userId, long groupId, string inputText, out IEnumerable<string> results)
        {
            List<string> answer = new List<string>();
            try
            {
                string modeName = mode.name;
                switch (modeName)
                {
                    case "正常":
                    case "混沌":
                        answer.Add(getAnswerChaos(userId, inputText));
                        break;
                    case "小万邦":
                        answer.Add(getGong());
                        break;



                    case "喷人":
                        answer.AddRange(getPen(groupId, userId));
                        break;
                    case "测试":
                        answer.AddRange(getHistoryReact(groupId, userId));
                        break;


                    case "AI":
                        results = new List<string>();
                        string uName = Config.Instance.UserInfo(userId).Name;
                        if (string.IsNullOrWhiteSpace(uName)) uName = "提问者";
                        GPT.Instance.AIReply(groupId, userId, uName, inputText);
                        return true;   // 不在此处理
                    case "语音":
                        results = new List<string>();
                        // string gong = getGong();
                        var r = getHistoryReact(groupId, userId);
                        string sendString = "";
                        foreach(var rs in r)
                        {
                            sendString += rs + "。";
                            if(sendString.Length>50)
                            {
                                GPT.Instance.AITalk(groupId, userId, sendString);
                                sendString = "";
                            }
                            
                        }
                        if (sendString.Length > 0) GPT.Instance.AITalk(groupId, userId, sendString);
                        return true;   // 不在此处理
                    default:
                        answer.Add(mode.getRandomSentence(inputText));
                        break;
                }

            }catch(Exception ex)
            {
                Logger.Instance.Log(ex);
            }


            results = answer.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            return results.Count() > 0;
        }

        private ModeInfo getUserMode(Player player)
        {
            if (player.Tags == null) return null;
            foreach (var tag in player.Tags)
            {
                if (tag.EndsWith("模式"))
                {
                    string findName = tag.Substring(0, tag.Length - 2);
                    if(modedict.TryGetValue(findName, out ModeInfo mode))
                    {
                        return mode;
                    }
                }
            }
            return null;
        }

        private ModeInfo getGroupMode(Playgroup group)
        {
            if (group.Tags != null)
            {
                foreach (var tag in group.Tags)
                {
                    if (tag.EndsWith("模式"))
                    {
                        string findName = tag.Substring(0, tag.Length - 2);
                        ModeInfo mode = null;
                        if (modedict.TryGetValue(findName, out mode))
                        {
                            return mode;
                        }
                    }
                }

            }
            if (modedict.TryGetValue("正常", out var val)) return val;
            return null;
        }


        //string GetChatModeName(string tagString)
        //{
        //    if(string.IsNullOrWhiteSpace(tagString)) return null;
        //    var tags=tagString.Split(',',StringSplitOptions.RemoveEmptyEntries);
        //    foreach (var tag in tags)
        //    {
        //        if (modedict.TryGetValue(tag.Replace("模式", "").Trim(), out var mode))
        //        {

        //            return mode.name;
        //        }
        //    }

        //    return "";
        //}

        //IEnumerable<string> GetAllChatTags()
        //{
        //    List<string> tags = new List<string>();
        //    foreach (var key in modedict.Keys)
        //    {
        //        tags.Add($"{key}模式");
        //    }

        //    return tags;
        //}


        //void GroupClearAndRefreshChatTag(Playgroup group, string newTag)
        //{
        //    foreach (var tag in GetAllChatTags())
        //    {
        //        group.DeleteTag(tag);
        //    }
        //    string tagName = $"{newTag.Replace("模式", "").Trim()}模式";
        //    group.SetTag(tagName);
        //}

        //void UserClearAndRefreshChatTag(Player user, string newTag)
        //{
        //    foreach (var tag in GetAllChatTags())
        //    {
        //        user.DeleteTag(tag);
        //    }
        //    string tagName = $"{newTag.Replace("模式", "").Trim()}模式";
        //    user.SetTag(tagName);
        //}

        bool UserHasAdminAuthority(long userId)
        {
            if (userId <= 0) return false;
            if (userId == Config.Instance.App.Avatar.adminQQ) return true;
            var user = Config.Instance.UserInfo(userId);
            if (user.Is("管理员")) return true;
            if (user.Type == PlayerType.Admin) return true;
            return false;
        }

        bool GroupHasAdminAuthority(long groupId)
        {
            if (groupId <= 0) return false;
            if (groupId == Config.Instance.App.Avatar.adminGroup) return true;
            var group = Config.Instance.GroupInfo(groupId);
            if (group.Is("测试")) return true;
            if (group.Type == PlaygroupType.Test) return true;
            return false;
        }





        public string printModeList()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var mode in modedict)
            {
                if (!mode.Value.config.Contains("隐藏"))
                { 
                   sb.Append($"{mode.Key}模式\r\n");
                }

            }
            sb.Append($"~输入“xxx模式on”即可切换模式~");
            return sb.ToString();
        }





        /// <summary>
        /// 龚诗 bot 特有的模拟
        /// </summary>
        /// <returns></returns>
        public string getGong()
        {
            StringBuilder sb = new StringBuilder();

            int snum = MyRandom.Next(1, 5);
            for (int i = 0; i < snum; i++)
            {
                int wnum = MyRandom.Next(1, 5);
                int nowlen = 0;
                for (int j = 0; j < wnum; j++)
                {
                    string s = chaosXwb[MyRandom.Next(chaosXwb.Count)];
                    sb.Append(s);
                    nowlen += s.Length;
                    if (nowlen > 15) break;
                }
                if (i < snum - 1) sb.Append("，");
                else sb.Append("。？！"[MyRandom.Next(3)]);
            }

            return sb.ToString();
        }
        /// <summary>
        /// 混沌模式的组句，比其他模式稍复杂些。从2个库中按概率抽取内容，整体上接近小万邦的同时加入新词
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string getChaosRandomSentence(string str)
        {
            string[] sgn = new string[] { "\r\n", "。", "？", "！", "…", "——", "??", "...", "：", "?!", "???", "!!", "！！！" };
            string result = "";
            byte[] md5data = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            int sentences = MyRandom.Next(1, 6);

            for (int i = 0; i < sentences; i++)
            {
                int thislen = MyRandom.Next(0, 11);
                StringBuilder thissentence = new StringBuilder();
                int wordnum = 0;
                while (thissentence.Length < thislen && wordnum < 5)
                {
                    wordnum++;
                    if (MyRandom.Next(0, 100) > 80)
                    {
                        thissentence.Append(chaosWord[MyRandom.Next(0, chaosWord.Count - 1)][0]);
                    }
                    else
                    {
                        thissentence.Append(chaosXwb[MyRandom.Next(0, chaosXwb.Count - 1)]);
                    }
                }
                thissentence.Append(sgn[MyRandom.Next(sgn.Length)]);
                result += thissentence.ToString();
            }

            return result;
        }









        public IEnumerable<string> getPen(long group, long user)
        {
            try
            {
                int num = MyRandom.Next(2, 10);
                List<string> res = new List<string>();
                while (num-- > 0)
                {
                    res.Add(penlist[MyRandom.Next(penlist.Count)].Trim());
                }
                return res;
            }
            catch (Exception e)
            {
                Logger.Instance.Log(e);
                return new List<string>();
            }
        }

        public IEnumerable<string> getHistoryReact(long group, long userqq)
        {
            List<string> result = new List<string>();

            string historyPath = Path.GetFullPath($"{Config.Instance.ResourceFullPath("HistoryPath")}/group");
            var files = Directory.GetFiles(historyPath, "*.txt");
            int maxtime = 10;
            try
            {
                if (files.Length <= 0) return result;
                while (maxtime-- > 0)
                {
                    int findex = MyRandom.Next(files.Length);
                    string[] lines = FileManager.ReadLines(files[findex]).ToArray();
                    if (lines.Length < 100) continue;
                    int begin = MyRandom.Next(lines.Length - 5);
                    int maxnum = MyRandom.Next(1, 5);
                    int num = lines.Length - begin;// MyRandom.Next(10, lines.Length - begin);
                    bool find = false;
                    string targetuser = "";
                    for (int i = 0; i < num; i++)
                    {
                        try
                        {
                            var items = lines[begin + i].Trim().Split('\t');
                            if (items.Length >= 3)
                            {
                                if (targetuser.Length > 0 && targetuser != items[1]) continue;
                                targetuser = items[1];
                                string msg = items[2].Trim();

                                if (!IOFilter.Instance.IsPass(msg, FilterType.Strict)) continue;

                                msg = Regex.Replace(msg, "\\[CQ\\:[^\\]]+\\]", "");
                                if (string.IsNullOrWhiteSpace(msg.Trim())) continue;
                                result.Add(msg);
                                find = true;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Instance.Log(e);
                        }
                        maxnum -= 1;
                        if (maxnum <= 0) break;

                    }
                    if (find)
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Log(e);
            }


            return result;
        }


        public string getAnswerGong(long user, string question)
        {
            string msg = "";
            if (MyRandom.Next(0, 100) < 85)
            {
                msg = getChaosRandomSentence(question) + getMotionString();
            }
            else
            {
                if (msg.Length <= 0 || MyRandom.Next(1, 100) < 40)
                {
                    msg = getSaoHua() + getMotionString();
                }
            }

            return msg;
        }

        /// <summary>
        /// 混沌模式的回复
        /// </summary>
        /// <param name="user"></param>
        /// <param name="question"></param>
        /// <returns></returns>
        public string getAnswerChaos(long user, string question)
        {
            string answer = "";
            string msg = "";
            if (MyRandom.Next(0, 100) < 85)
            {
                msg = getChaosRandomSentence(question) + getMotionString();
            }
            else
            {
                //answer = getZhidaoAnswer(question);
                //if (answer.Length > 0)
                //{
                //    msg = answer + "..." + getMotionString();
                //}
                if (msg.Length <= 0 || MyRandom.Next(1, 100) < 40)
                {
                    msg = getSaoHua() + getMotionString();
                }
            }

            return msg;
        }


        /// <summary>
        /// 获取骚话（情话）
        /// </summary>
        /// <returns></returns>
        public string getSaoHua()
        {
            try
            {
                return defaultAnswers[MyRandom.Next(defaultAnswers.Count)].Trim();
            }
            catch (Exception e)
            {
                Logger.Instance.Log(e);
                return "";
            }
        }

        /// <summary>
        /// 获取随机的括弧情绪文本。例如（悲）（大嘘）这种
        /// </summary>
        /// <returns></returns>
        public string getMotionString()
        {
            string res = "";

            if (chaosMotion.Count <= 0) return res;
            if (MyRandom.Next(0, 100) > 66)
            {
                res = $"({chaosMotion[MyRandom.Next(0, chaosMotion.Count - 1)]})";
            }

            return res;
        }





        class ModeInfo
        {
            public string name;
            public List<string> config;
            List<string> sentences;
            // public int 
            public ModeInfo()
            {
                config = new List<string>();
                sentences = new List<string>();
            }

            public ModeInfo(string _name, ICollection<string> _config, ICollection<string> _sentences)
            {
                name = _name;
                config = _config.ToList();
                sentences = _sentences.ToList();
            }

            public string getRandomSentence(string seed = "")
            {

                int maxsnum = 5;
                int maxslen = 7;
                int maxwordnum = 4;

                if (config.Contains("单句"))
                {
                    maxslen = 1;
                    maxwordnum = 1;
                    maxsnum = 1;
                }
                if (config.Contains("句内不拼接"))
                {
                    maxwordnum = 1;
                    maxslen = 1;
                }


                string result = "";
                //byte[] md5data = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
                string[] sgn1 = new string[] { ",", "，", "；", "、" };
                string[] sgn2 = new string[] { "\r\n", "。", "。", "。", "？", "！", "…", "——" };
                string[] sgn3 = new string[] { "\r\n", "。", "？", "！", "…", "——", "??", "...", "：", "?!", "???", "!!", "！！！" };

                int sn = MyRandom.Next(1, maxsnum);

                for (int i = 0; i < sn; i++)
                {
                    int thislen = MyRandom.Next(1, maxslen);
                    StringBuilder thissentence = new StringBuilder();
                    int wordnum = 0;
                    while (thissentence.Length < thislen && wordnum < maxwordnum)
                    {
                        wordnum++;
                        thissentence.Append(sentences[MyRandom.Next(0, sentences.Count - 1)]);
                    }
                    if (thissentence.Length > 0 && !sgn1.Contains(thissentence.ToString().Last().ToString()) && !sgn2.Contains(thissentence.ToString().Last().ToString()))
                    {
                        if (config.Contains("无标点")) thissentence.Append(" ");
                        else thissentence.Append(sgn1[MyRandom.Next(sgn1.Length)]);
                        result += thissentence.ToString();
                        if (result.Length > 0)
                        {
                            if (config.Contains("无标点")) ;
                            else if (config.Contains("乱打标点")) result = result.Substring(0, result.Length - 1) + sgn3[MyRandom.Next(sgn3.Length)];
                            else result = result.Substring(0, result.Length - 1) + sgn2[MyRandom.Next(sgn2.Length)];
                        }

                    }
                    else
                    {
                        result += thissentence.ToString();
                    }
                }
                if (string.IsNullOrWhiteSpace(result))
                {
                    if (config.Contains("无标点")) result = " ";
                    else if (config.Contains("乱打标点")) result = result.Substring(0, result.Length - 1) + sgn3[MyRandom.Next(sgn3.Length)];
                    else result = result.Substring(0, result.Length - 1) + sgn2[MyRandom.Next(sgn2.Length)];
                }


                return result;
            }
        }



    }
}
