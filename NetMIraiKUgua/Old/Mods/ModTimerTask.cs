using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatGPT.Net.DTO.ChatGPT;
using ChatGPT.Net;
using MeowMiraiLib;
using MeowMiraiLib.GenericModel;
using MeowMiraiLib.Msg;
using MeowMiraiLib.Msg.Sender;
using MeowMiraiLib.Msg.Type;
using MMDK.Util;
using MeowMiraiLib.Event;
using System.Reflection.PortableExecutable;
using System.IO;
using Microsoft.VisualBasic.Devices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MMDK.Mods
{
    internal class ModTimerTask : Mod, ModWithMirai
    {
        class MyTask
        {
            public long UserId;
            public long GroupId;
            public DateTime Time;
            public string Message;
            //public System.Timers.Timer TaskTimer;
        }

        System.Timers.Timer TaskTimer;

        MeowMiraiLib.Client client;


        List<MyTask> tasks = new List<MyTask>();







        public void Exit()
        {
        }
        public bool Init(string[] args)
        {
            return true;
        }
        public void InitMiraiClient(Client _client)
        {
            client = _client;


            TaskTimer = new(1000 * 10);
            TaskTimer.AutoReset = true;
            TaskTimer.Start();
            TaskTimer.Elapsed += TaskTimer_Elapsed;


            

        }

        private void TaskTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            for(int i = tasks.Count - 1; i >= 0; i--)
            {
                try
                {
                    var t = tasks[i];
                    if (DateTime.Now >= t.Time)
                    {
                        new MeowMiraiLib.Msg.GroupMessage(t.GroupId, [
                            new At(t.UserId, ""),
                                    new Plain($"{DateTime.Now.ToString("HH:mm")}到了{(string.IsNullOrEmpty(t.Message)?"":$"，{t.Message}，请")}")
                            ]).Send(client);
                        //new MeowMiraiLib.Msg.FriendMessage(userId, new Message[] { new Plain($"{DateTime.Now.Hour}点 啦!") }).Send(c);

                        tasks.Remove(t);
                    }
                }
                catch { }
            }

        }
        /// <summary>
        /// 刷新好友列表并更新配置文件
        /// </summary>
        public void RefreshFriendList()
        {
            try
            {
                if (client != null)
                {
                    var fp = new FriendList().Send(client);
                    Config.Instance.friends.Clear();
                    if (fp == null)
                    {
                        Logger.Instance.Log($"不会吧不会吧不会没有好友吧");

                    }
                    else
                    {
                        foreach (var f in fp)
                        {
                            var friend = Config.Instance.UserInfo(f.id);
                            friend.Name = f.nickname;
                            //friend.Mark = f.remark;
                            friend.Tags.Add("好友");
                            //friend.Type = PlayerType.Normal;
                            Config.Instance.friends.Add(f.id, f);
                        }
                    }




                    var gp = new GroupList().Send(client);
                    Config.Instance.groups.Clear();
                    Config.Instance.groupMembers.Clear();
                    if (gp == null)
                    {
                        Logger.Instance.Log($"不会吧不会吧不会没有群吧");

                    }
                    else
                    {
                        foreach (var g in gp)
                        {
                            var group = Config.Instance.GroupInfo(g.id);
                            group.Name = g.name;
                            var groupMembers = g.GetMemberList(client);
                            if (groupMembers == null)
                            {
                                Logger.Instance.Log($"不会吧不会吧不会{g.id}是鬼群吧");
                                continue;
                            }
                            Config.Instance.groups.Add(g.id, g);
                            Config.Instance.groupMembers.Add(g.id, groupMembers);
                            foreach (var gf in groupMembers)
                            {
                                var member = Config.Instance.UserInfo(gf.id);
                                member.Mark = gf.memberName;    //群昵称？
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }

        }
        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {

            try
            {
                bool isGroup = groupId > 0;

                var regex = new Regex(@"^帮我撤回(\d{1,2})?条?");
                var match = regex.Match(message);
                if (isGroup && Config.Instance.UserHasAdminAuthority(userId) && match.Success)
                {
                    int quantity = 1;
                    if (match.Groups[1].Success)
                    {
                        quantity = int.Parse(match.Groups[1].Value);
                    }
                    var historys = HistoryManager.Instance.findMessage(userId, groupId);
                    for (int i = 0; i < Math.Min(historys.Length, quantity); i++)
                    {
                        Logger.Instance.Log($"?{historys[i].messageId}");

                        new GroupMessage(groupId, [
                            new Quote(historys[i].messageId,groupId,userId,groupId,
                                [new Plain(historys[i].message)]),
                                ]).Send(client);
                        new Recall(historys[i].messageId).Send(client);
                    }
                    return true;
                    //new MeowMiraiLib.Msg.GroupMessage(groupId, [
                    //new At(userId, ""),
                    //    new Plain($"?")
                    //]).Send(client);
                    //return true;
                }

                regex = new Regex(@"^刷新列表");
                match = regex.Match(message);
                if (isGroup && Config.Instance.UserHasAdminAuthority(userId) && match.Success)
                {
                    Logger.Instance.Log($"更新好友列表和群列表...");
                    RefreshFriendList();
                    Logger.Instance.Log($"更新完毕，找到{Config.Instance.friends.Count}个好友，{Config.Instance.groups.Count}个群...");
                    new MeowMiraiLib.Msg.GroupMessage(groupId, [
                        new Plain($"更新完毕，找到{Config.Instance.friends.Count}个好友，{Config.Instance.groups.Count}个群...")
                        ]).Send(client);
                    return true;
                }

                regex = new Regex(@"^来点狐狸");
                match = regex.Match(message);
                if (match.Success)
                {
                    var files = Directory.GetFiles(@"D:\Projects\TestFunctions\bin\Debug\net8.0-windows\gifs", "*.gif");
                    if (files != null)
                    {
                        string fname = files[MyRandom.Next(files.Length)];

                        if (isGroup)
                        {
                            new MeowMiraiLib.Msg.GroupMessage(groupId, [
                                                        new Image(null,null,fname)
                                                    ]).Send(client);
                        }
                        else
                        {
                                // friend
                                new MeowMiraiLib.Msg.FriendMessage(userId, [
                                new Image(null,null,fname)
                            ]).Send(client);
                        }

                        
                    }
                    
                    return true;
                }

                regex = new Regex(@"^说(.+)", RegexOptions.Singleline);
                match = regex.Match(message);
                if (match.Success)
                {
                    string speakSentence = match.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(speakSentence)) return false;

                        GPT.Instance.AITalk(groupId, userId, $"{speakSentence}");
                    

                    return true;
                }


                regex = new Regex(@"^你什么情况？");
                match = regex.Match(message);
                if (match.Success)
                {
                    var res = new BotProfile().Send(client);
                    string data = "";
                    data += $"我是{res.nickname}，{(res.sex == "FEMALE" ? "女" : "男")}，QQ等级{res.level}，年龄{res.age}，邮箱是{res.email}，个性签名是\"{res.sign}\"。你们别骂我了！\n";

                    GPT.Instance.AITalk(groupId, userId, $"你是谁啊？");


                    //foreach (var msg in e)
                    //{
                    //    data += $"{msg.type}/";

                    //}



                    //new GroupMessage(groupId, [
                    //        //new At(userId, ""),
                    //        new Plain($"{data}"),
                    //        new Image(null, "https://s3.bmp.ovh/imgs/2024/10/31/ce9c165d2d4c274a.gif"),
                            
                    //        ]).Send(client);

                    
                    //var ress = new Anno_publish(groupId, "Bot 公告推送").Send(client);
                    //var res2 = new Anno_list(groupId).Send(client);
                    //foreach (var ano in res2)
                    //{
                    //    data += $"{ano.content}\n望周知！\n";
                    //}

                    //new GroupMessage(groupId, [
                    //    //new At(userId, ""),
                    //    new Plain($"{data}")
                    //    ]).Send(client);
                    return true;
                }

                regex = new Regex(@"^(\d{1,2})[:：点]((\d{1,2})分?)?[叫喊]我(.*)");
                match = regex.Match(message);
                if (match.Success)
                {
                    string hourString = match.Groups[1].Value;
                    string minuteString = match.Groups[3].Value;
                    string alertMsg = match.Groups[4].Value;
                    if (string.IsNullOrWhiteSpace(alertMsg)) alertMsg = "";
                    int hour;int minute;
                    if (!int.TryParse(hourString, out hour)) return false;
                    if (!int.TryParse(minuteString, out minute)) minute = 0;
                    DateTime alertTime = DateTime.Today.AddHours(hour).AddMinutes(minute);
                    if (alertTime < DateTime.Now) alertTime = alertTime.AddDays(1);

                    MyTask newTask = new MyTask
                    {
                        UserId = userId,
                        GroupId = groupId,
                        Time = alertTime,
                        Message = alertMsg,
                    };
                    tasks.Add(newTask);
                    if (isGroup)
                    {
new MeowMiraiLib.Msg.GroupMessage(groupId, [
                        new At(userId, ""),
                            new Plain($"帮你设了{alertTime.ToString("d号H点m分")}{alertMsg}的闹钟")
                        ]).Send(client);
                    }
                    else
                    {
                        new MeowMiraiLib.Msg.FriendMessage(userId, [
                            new Plain($"帮你设了{alertTime.ToString("d号H点m分")}{alertMsg}的闹钟")
                        ]).Send(client);
                    }



                    return true;
                }

                regex = new Regex(@"^闹钟(列表|信息|状态)\b+");
                match = regex.Match(message);
                if (match.Success)
                {
                    string res = "";
                    int no = 1;
                    for(int i = tasks.Count - 1; i >=0; i--)
                    {
                        try
                        {
                            var task = tasks[i];
                            if(task.UserId==userId && task.GroupId == groupId)
                            {
                                res += $"- {task.Time.ToString("MM月dd日HH时mm分")} {task.Message}\n";
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Log(ex);
                        }
                    }
                    if (isGroup)
                    {
                        if (no > 1)
                        {

                            new MeowMiraiLib.Msg.GroupMessage(groupId, [
                                    new At(userId, ""),
                                new Plain($"你在本群订了{no-1}个闹钟：\n{res}")
                                ]).Send(client);
                            return true;
                        }
                        else
                        {
                            new MeowMiraiLib.Msg.GroupMessage(groupId, [
                                    new At(userId, ""),
                                new Plain($"你没设过闹钟")
                                ]).Send(client);
                            return true;
                        }
                    }
                    else
                    {
                        if (no > 1)
                        {

                            new MeowMiraiLib.Msg.FriendMessage(userId, [
                                new Plain($"你订了{no-1}个闹钟：\n{res}")
                                ]).Send(client);
                            return true;
                        }
                        else
                        {
                            new MeowMiraiLib.Msg.FriendMessage(userId, [
                                new Plain($"你没设过闹钟")
                                ]).Send(client);
                            return true;
                        }
                    }
                }

                regex = new Regex(@"^(删除闹钟|别[叫喊][了我]?)");
                match = regex.Match(message);
                if (match.Success)
                {
                    int haveTask = TaskRemove(userId, groupId);
                    if (haveTask > 0)
                    {
                        if (isGroup)
                        {
                            new MeowMiraiLib.Msg.GroupMessage(groupId, [
                            new At(userId, ""),
                                new Plain($"彳亍!{haveTask}个闹钟以取消")
                            ]).Send(client);
                            return true;
                        }
                        else
                        {
                            new MeowMiraiLib.Msg.FriendMessage(groupId, [
                                new Plain($"彳亍!{haveTask}个闹钟以取消")
                            ]).Send(client);
                            return true;
                        }
                        
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            return false;

        }


        int TaskRemove(long userId, long groupId)
        {
            int haveTask = 0;
            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                try
                {
                    var task = tasks[i];
                    if (task.UserId == userId && task.GroupId == groupId)
                    {
                        haveTask++;
                        tasks.RemoveAt(i); // 倒序删除元素不会影响遍历
                    }
                }
                catch { }
            }

            return haveTask;
        }


        bool ModWithMirai.OnFriendMessageReceive(FriendMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
        {
            if (e == null || e.Length < 2) return false;

            var source = e[0];
            foreach(var msg in e)
            {
                if(msg is Plain plain)
                {
                    var str = plain.text;
                    if (str == "测试")
                    {
                        //new FriendMessage(s.id, [
                        //new Voice(null,voice.url)
                        //]).Send(client);
                    }
                    //string msg2 = StaticUtil.RemoveEmojis(plain.text);
                    //if (string.IsNullOrWhiteSpace(msg2)) return false;
                    //new FriendMessage(s.id, [
                    //    new Plain(msg2)
                    //    ]).Send(client);
                    return false;
                }
                if(msg is Voice voice)
                {
                    new FriendMessage(s.id, [
                        new Voice(null,voice.url)
                        ]).Send(client);
                    return true;
                }
            }

            //throw new NotImplementedException();
            return false;
        }

        bool ModWithMirai.OnGroupMessageReceive(GroupMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
        {
            if (s == null || e == null) return false;
            var message = e.MGetPlainString();
            long groupId = s.group.id;
            long userId = s.id;
            var group = Config.Instance.GroupInfo(groupId);
            var user = Config.Instance.UserInfo(userId);
            var source = e.First() as Source;

            var ask = isAskMe(e);
            if (ask)
            {
                // 
                foreach(var msg in e)
                {
                    
                    if(msg is Plain plain)
                    {
                        if (plain.text == "测试下")
                        {
                            new GroupMessage(s.group.id, [
                                new Voice(null,null,@"D:\Videos\20241103-124142-2.amr")
                                ]).Send(client);

                            //return true;
                        }
                    }
                }
            }


            string logstr = "";
            foreach (var msg in e)
            {
                logstr += $",{msg.GetType()}";
            }
            Logger.Instance.Log(logstr);
                
            foreach(var msg in e)
            {
                if (msg is Voice voice)
                {
                    new GroupMessage(s.group.id, [
                            new Voice(voice.voiceId)
                            ]).Send(client);

                    return true;
                }

                if (msg is Image itemImg)
                {
                    string userImgDict = $"{Config.Instance.ResourceFullPath("HistoryImagePath")}/{userId}";
                    if (!Directory.Exists(userImgDict)) Directory.CreateDirectory(userImgDict);
                    WebLinker.DownloadImageAsync(itemImg.url, $"{userImgDict}/{itemImg.imageId}");
                }
                //ForwardMessage fm = new ForwardMessage([new ForwardMessage.Node(Config.Instance.App.Avatar.myQQ, DateTime.Now.Ticks, Config.Instance.App.Avatar.myName, e.Skip(1).ToArray(), source.id)]);

                //if (userId == Config.Instance.App.Avatar.adminQQ)
                //{
                //    //if (item is ForwardMessage gmsg)
                //    {
                //        // new MeowMiraiLib.Msg.GroupMessage(groupId, [
                //        //  new At(userId, ""),
                //        //new ForwardMessage()
                //        //new Voice(null,null,@"D:\Projects\SummerTTS_VS-main\x64\Debug\out.wav")
                //        //new ForwardMessage.Node()
                //        //]).Send(client);
                //        //return true;
                //    }
                //}
            }

            return false;
        }



        /// <summary>
        /// 查看并截断掉Message里的提示词
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        bool isAskMe(MeowMiraiLib.Msg.Type.Message[] e)
        {
            foreach(var item in e)
            {
                if (item is AtAll) return true;
                if(item is At itemat)
                {
                    if(itemat.target == Config.Instance.App.Avatar.myQQ)  return true;
                    
                }
                if(item is Plain plain)
                {
                    if (plain.text.TrimStart().StartsWith(Config.Instance.App.Avatar.askName))
                    {
                        plain.text = plain.text.TrimStart().Substring(Config.Instance.App.Avatar.askName.Length);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
