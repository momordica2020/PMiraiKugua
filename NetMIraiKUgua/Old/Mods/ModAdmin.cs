using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using MMDK.Util;

namespace MMDK.Mods
{
    /// <summary>
    /// 管理员指令
    /// </summary>
    public class ModAdmin : Mod
    {






        public bool Init(string[] args)
        {

            return true;
        }

        public void Exit()
        {
            
        }

        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {
            if(string.IsNullOrWhiteSpace(message))return false;
            message = message.Trim();

            CommandType cmd = CommandType.None;
            bool isGroup = groupId > 0;
            var user = Config.Instance.UserInfo(userId);
            
            var group = Config.Instance.GroupInfo(groupId);
            if (TryReadCommand(ref message, out cmd))
            {
                
                switch (cmd)
                {
                    case CommandType.Help:
                        results.Add(getWelcomeString());
                        return true;
                    case CommandType.Ban:
                        if (!Config.Instance.UserHasAdminAuthority(userId)) return false;
                        var targetUserId = 0;
                        if (string.IsNullOrWhiteSpace(message) || !int.TryParse(message, out targetUserId))
                        {
                            results.Add($"请在指令后接用户QQ号码");
                            return true;
                        }
                        else
                        {
                            var targetUser = Config.Instance.UserInfo(targetUserId);
                            targetUser.Tags.Add("黑名单"); // 临时性拉黑，没有加type设置
                            results.Add($"已全局屏蔽{targetUser.Name}({targetUserId})");
                            return true;
                        }
                    case CommandType.UnBan:
                        if (!Config.Instance.UserHasAdminAuthority(userId)) return false;
                        var targetUserId2 = 0;
                        if (string.IsNullOrWhiteSpace(message) || !int.TryParse(message, out targetUserId))
                        {
                            results.Add($"请在指令后接用户QQ号码");
                            return true;
                        }
                        else
                        {
                            var targetUser = Config.Instance.UserInfo(targetUserId2);
                            targetUser.Tags.Remove("黑名单"); 
                            results.Add($"已解除屏蔽{targetUser.Name}({targetUserId2})");
                            return true;
                        }
                    case CommandType.TagAdd:
                        if (!Config.Instance.UserHasAdminAuthority(userId)) return false;
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            results.Add($"请在指令后接tag名称");
                            return true;
                        }
                        if (isGroup)
                        {
                            group.Tags.Add(message);
                            results.Add($"本群已添加tag：{message}");
                            return true;
                        }
                        else
                        {
                            user.Tags.Add(message);
                            results.Add($"私聊已添加tag：{message}");
                            return true;
                        }
                    case CommandType.TagRemove:
                        if (!Config.Instance.UserHasAdminAuthority(userId)) return false;
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            results.Add($"请在指令后接tag名称");
                            return true;
                        }
                        if (isGroup)
                        {
                            group.Tags.Remove(message);
                            results.Add($"本群已删掉tag：{message}");
                            return true;
                        }
                        else
                        {
                            user.Tags.Remove(message);
                            results.Add($"私聊已删掉tag：{message}");
                            return true;
                        }
                    case CommandType.TagRemoveAll:
                        if (!Config.Instance.UserHasAdminAuthority(userId)) return false;
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            if (isGroup)
                            {
                                group.Tags.Clear();
                                results.Add($"本群已清空所有tag");
                                return true;
                            }
                            else
                            {
                                user.Tags.Clear();
                                results.Add($"私聊已清空所有tag");
                                return true;
                            }
                        }
                        else
                        {
                            if (isGroup)
                            {
                                group.Tags.RemoveWhere(tag => tag.Contains(message));
                                results.Add($"本群已删除所有带{message}tag");
                                return true;
                            }
                            else
                            {
                                user.Tags.RemoveWhere(tag => tag.Contains(message));
                                results.Add($"私聊已删除所有带{message}tag");
                                return true;
                            }
                        }
                    case CommandType.CheckState:
                        string rmsg = "";
                        if (Config.Instance.GroupHasAdminAuthority(groupId) || Config.Instance.UserHasAdminAuthority(userId) || userId == Config.Instance.App.Avatar.adminQQ ) //临时：只有测试群可查详细信息
                        {
                            DateTime startTime = Config.Instance.App.Log.StartTime;
                            rmsg += $"内核版本 - 苦音未来v{Config.Instance.App.Version}（{Util.StaticUtil.GetBuildDate().ToString("F")}）\n";
                            rmsg += $"启动时间：{startTime.ToString("yyyy-MM-dd HH:mm:ss")}(已运行{(DateTime.Now - startTime).TotalDays.ToString("0.00")}天)\n";
                            rmsg += $"CPU({Config.Instance.systemInfo.CpuLoad.ToString(".0")}%) 内存({(100.0 - ((double)Config.Instance.systemInfo.MemoryAvailable * 100 / Config.Instance.systemInfo.PhysicalMemory)).ToString(".0")}%)\n";
                            rmsg += $"{SystemInfo.GetNvidiaGpuAndMemoryUsage()}\n";
                            rmsg += $"一共重启{Config.Instance.App.Log.beginTimes}次\n";
                            rmsg += $"数据库有{Config.Instance.playgroups.Count}个群和{Config.Instance.players.Count}个账户\n";
                            rmsg += $"在群里被乐{Config.Instance.App.Log.playTimeGroup}次\n";
                            rmsg += $"在私聊被乐{Config.Instance.App.Log.playTimePrivate}次\n";
                            rmsg += $"机主是{Config.Instance.App.Avatar.adminQQ}\n";
                        }
                        if (isGroup)
                        {
                            rmsg += $"在本群的标签是：{(group.Tags.Count == 0 ? "(暂无标签)" : string.Join(", ", group.Tags))}\n";
                        }
                        else
                        {
                            //私聊查状态
                             rmsg += $"在私聊的标签是：{(user.Tags.Count == 0 ? "(暂无标签)" : string.Join(", ", user.Tags))}\r\n";
                        }
                        results.Add(rmsg);
                        return true;
                    case CommandType.SaveConfig:
                        // 存档咯
                        if (Config.Instance.GroupHasAdminAuthority(groupId) || Config.Instance.UserHasAdminAuthority(userId) || userId == Config.Instance.App.Avatar.adminQQ)
                        {
                            Config.Instance.Save();
                            ModRaceHorse.Instance.save();
                            results.Add($"配置文件以存档 {DateTime.Now.ToString("F")}");
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
            Help,
            Ban,
            UnBan,
            TagAdd,
            TagRemove,
            TagRemoveAll,
            CheckState,
            SaveConfig,
        }
        // 存储可匹配的命令
        private static readonly Dictionary<string, CommandType> CommandDict = new Dictionary<string, CommandType>
        {
            { "功能", CommandType.Help},
            { "帮助", CommandType.Help},
            { "菜单", CommandType.Help},
            { "拉黑", CommandType.Ban},
            { "屏蔽", CommandType.Ban},
            { "ban", CommandType.Ban},
            { "封", CommandType.Ban},
            { "解封", CommandType.UnBan},
            { "解开", CommandType.UnBan},
            { "设置+", CommandType.TagAdd},
            //{ "添加", CommandType.TagAdd},
            { "设置-", CommandType.TagRemove},
            { "设置清空", CommandType.TagRemoveAll},
            { "状态", CommandType.CheckState},
            { "存档", CommandType.SaveConfig},
        };
        static bool TryReadCommand(ref string input, out CommandType commandType)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                foreach (var command in CommandDict)
                {
                    if (input.StartsWith(command.Key))
                    {
                        commandType = command.Value; // 找到的命令
                        input = input.Substring(command.Key.Length).Trim(); // 截掉命令部分并去掉前导空格
                        return true;
                    }
                }
            }


            commandType = CommandType.None; // 如果没有匹配的命令
            return false;
        }


        /// <summary>
        /// bot的欢迎文本
        /// </summary>
        /// <returns></returns>
        string getWelcomeString()
        {
            return "" +
                $"想在群里使用，就at我或者打字开头加“{Config.Instance.App.Avatar.askName}”，再加内容。私聊乐我的话直接发内容。\r\n" +
                "以下是群常用功能。私聊可以闲聊。\r\n" +
                "~状态查看：“状态”\r\n" +
                "~模式更换：“模式列表”、“xx模式on”\r\n" +
                "~掷骰子：“rd 成功率”“r3d10 攻击力”\r\n" +
                //"~多语翻译：“汉译法译俄 xxxx”\r\n" +
                //"~天气预报：“北京明天天气”\r\n" +
                //"B站live搜索：“绘画区谁在播”“虚拟区有多少B限”“xxx在播吗”\r\n" +
                "~赛马：“赛马介绍”“签到”“个人信息”\r\n" +
                "~生成攻受文：“A攻B受”\r\n" +
                //"~生成谴责：“A谴责B的C”\r\n" +
                //"~生成笑话：“讽刺 本国=A国，好人=甲，坏人=乙，事件=xx”\r\n" +
                "~生成随机汉字：“随机5*4”\r\n" +
                "~周易占卜：“占卜 xxx”\r\n";
        }


        



    }


}
