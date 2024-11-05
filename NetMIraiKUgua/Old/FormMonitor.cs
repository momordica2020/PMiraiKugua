using MeowMiraiLib;
using MeowMiraiLib.Event;
using MeowMiraiLib.Msg;
using MeowMiraiLib.Msg.Sender;
using MeowMiraiLib.Msg.Type;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;


using MMDK.Mods;
using MMDK.Util;
using static MeowMiraiLib.Msg.Type.ForwardMessage;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using Microsoft.Win32;
using System.ComponentModel;
using static System.Windows.Forms.AxHost;
using System.Threading.Tasks;
using System.Windows.Interop;
using MeowMiraiLib.GenericModel;
using System.Reflection;
using System.Text;
using System.Linq;
using ChatGPT.Net;
using ChatGPT.Net.DTO;
using System.Windows.Media.Media3D;
using ChatGPT.Net.DTO.ChatGPT;

namespace MMDK
{

    partial class FormMonitor : Form
    {







        public static MeowMiraiLib.Client ClientX;


        public List<Mod> Mods = new List<Mod>();


        





        #region 窗体相关定义



        DateTime beginTime;
        bool IsEnterAutoSend = true;
        bool IsVirtualGroup = false;

        public delegate void sendString(string msg);

        public enum BotRunningState
        {
            stop,
            mmdkInit,
            ok,
            exit
        }
        public BotRunningState _state;
        BotRunningState State
        {
            get => _state;
            set
            {
                _state = value;

                var stateMessages = new Dictionary<BotRunningState, string>
                    {
                        { BotRunningState.stop, "已停止" },
                        { BotRunningState.mmdkInit, "正在启动Bot" },
                        { BotRunningState.ok, "正在运行" }
                    };

                string text = stateMessages.ContainsKey(value) ? stateMessages[value] : string.Empty;
                //更新显示窗口
                try
                {
                    Invoke((Action)(() =>
                    {
                        lbState.Text = text;
                    }));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating state label: {ex.Message}\r\n{ex.StackTrace}");
                }
            }
        }



        #endregion

        public FormMonitor()
        {
            InitializeComponent();
            this.DoubleBuffered = true; // 设置窗体的双缓冲
        }

        /// <summary>
        /// 设置缓冲阻止频闪。？
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // Turn on WS_EX_COMPOSITED 
                return cp;
            }
        }
        /// <summary>
        /// 检查并初始化配置
        /// </summary>
        /// <returns></returns>
        bool checkAndSetConfigValid()
        {
            try
            {

                if (string.IsNullOrWhiteSpace(Config.Instance.App.Version)) Config.Instance.App.Version = "v0.0.1";

                // qq info
                if (string.IsNullOrWhiteSpace(Config.Instance.App.Avatar.myQQ.ToString())) Config.Instance.App.Avatar.myQQ = 00000;


                beginTime = DateTime.Now;
                Config.Instance.App.Log.StartTime = beginTime;

                Config.Instance.App.Log.beginTimes += 1;

            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
                return false;
            }

            return true;
        }



        public void HandleShowLog(LogInfo info)
        {
            int maxlen = 100000;
            try
            {
                Invoke((Action)(() =>
                {
                    tbLog.AppendText($"{info.ToDescription()}\r\n");
                    if (tbLog.TextLength > maxlen)
                    {
                        tbLog.Text = tbLog.Text.Substring(tbLog.TextLength - maxlen);
                    }
                    tbLog.ScrollToCaret();
                }));
            }
            catch (Exception ex)
            {
                //Logger.Instance.Log(ex);
            }

        }



        void workRunBot()
        {
            try
            {
                Logger.Instance.Log($"读取配置文件...");
                Config.Instance.Load();

                Logger.Instance.Log($"启用过滤器...");
                IOFilter.Instance.Init();

                

                Logger.Instance.Log($"开始启动bot...");
                Mods = new List<Mod>
                {
                    new ModAdmin(),
                    ModBank.Instance,
                    ModRaceHorse.Instance,
                    new ModDice(),
                    new ModProof(),
                    new ModTextFunction(),
                    new ModZhanbu(),
                    //new ModTranslate(),
                    new ModTimerTask(),
                    new ModNLP(),
                    new ModRandomChat(),    // 这个会用闲聊收尾

                };



                //bot = new MainProcess();
                //bot.Init(config);



                if (true)
                {
                    // 打开历史记录，不会是真的吧
                    string HistoryPath = Config.Instance.ResourceFullPath("HistoryPath");
                    if (!Directory.Exists(HistoryPath)) Directory.CreateDirectory(HistoryPath);
                    Logger.Instance.Log($"历史记录保存在 {HistoryPath} 里");
                    HistoryManager.Instance.Init(HistoryPath);
                }
                else
                {
                    Logger.Instance.Log($"历史记录不会有记录");
                }

                foreach (var mod in Mods)
                {
                    try
                    {
                        if (mod.Init(null))
                        {
                            Logger.Instance.Log($"模块{mod.GetType().Name}已初始化");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Log(ex);
                    }
                }

                State = BotRunningState.ok;
                Logger.Instance.Log($"bot启动完成");


                //mirai = new MiraiLink();
                if (Config.Instance.App.IO.MiraiRun)
                {

                    //string verifyKey = "123456";
                    string connectUri = $"{Config.Instance.App.IO.MiraiWS}:{Config.Instance.App.IO.MiraiPort}/all?qq={Config.Instance.App.Avatar.myQQ}";
                    Logger.Instance.Log($"正在连接Mirai...{connectUri}");
                    ClientX = new(connectUri);
                    ClientX._OnServeiceConnected += ServiceConnected;
                    ClientX._OnServeiceError += OnServeiceError;
                    ClientX._OnServiceDropped += OnServiceDropped;


                    ClientX.Connect();

                    foreach (var mod in Mods)
                    {
                        if(mod is ModWithMirai modWithMirai)
                        {
                            modWithMirai.InitMiraiClient(ClientX);
                        }
                    }

                    Logger.Instance.Log($"启用GPT接口...");
                    GPT.Instance.Init(ClientX);

                    ClientX.OnFriendMessageReceive += OnFriendMessageReceive;
                    ClientX.OnGroupMessageReceive += OnGroupMessageReceive;
                    ClientX.OnEventBotInvitedJoinGroupRequestEvent += OnEventBotInvitedJoinGroupRequestEvent;
                    ClientX.OnEventNewFriendRequestEvent += OnEventNewFriendRequestEvent;
                    ClientX.OnEventFriendNickChangedEvent += OnEventFriendNickChangedEvent;


                }
                else
                {
                    Logger.Instance.Log($"不启动Mirai，启动本地应答");

                }

            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex.Message + "\r\n" + ex.StackTrace);
            }
        }







        /* APIs
         * OnEventGroupNameChangeEvent	GroupNameChangeEvent	群名改变信息
OnEventGroupEntranceAnnouncementChangeEvent	GroupEntranceAnnouncementChangeEvent	某个群的入群公告改变
OnEventGroupMuteAllEvent	GroupMuteAllEvent	群全员禁言
OnEventGroupAllowAnonymousChatEvent	GroupAllowAnonymousChatEvent	某个群更改了群匿名聊天状态
OnEventGroupAllowConfessTalkEvent	GroupAllowConfessTalkEvent	某个群更改了坦白说的状态
OnEventGroupAllowMemberInviteEvent	GroupAllowMemberInviteEvent	某个群员邀请好友加群


        OnEventGroupRecallEvent	GroupRecallEvent	某群员撤回信息
OnEventMemberJoinEvent	MemberJoinEvent	某群有新人入群了
OnEventMemberLeaveEventKick	MemberLeaveEventKick	某群把某人踢出了(不是Bot)
OnEventMemberLeaveEventQuit	MemberLeaveEventQuit	某群有成员主动退群了
OnEventCardChangeEvent	MemberCardChangeEvent	某群有人的群名片改动了
OnEventSpecialTitleChangeEvent	MemberSpecialTitleChangeEvent	某群群主改动了某人头衔
OnEventPermissionChangeEvent	MemberPermissionChangeEvent	某群有某个成员权限被改变了(不是Bot)
OnEventMemberMuteEvent	MemberMuteEvent	某群的某个群成员被禁言
OnEventMemberUnmuteEvent	MemberUnmuteEvent	某群的某个群成员被取消禁言
OnEventMemberHonorChangeEvent	MemberHonorChangeEvent	某群的某个成员的群称号改变
OnEventMemberJoinRequestEvent	MemberJoinRequestEvent	接收到用户入群申请

        OnEventNewFriendRequestEvent	NewFriendRequestEvent	接收到新好友请求
OnEventFriendInputStatusChangedEvent	FriendInputStatusChangedEvent	好友的输入状态改变
OnEventFriendNickChangedEvent	FriendNickChangedEvent	好友的昵称改变
OnEventFriendRecallEvent	FriendRecallEvent	好友撤回信息


        OnEventBotGroupPermissionChangeEvent	BotGroupPermissionChangeEvent	Bot在群里的权限被改变了
OnEventBotMuteEvent	BotMuteEvent	Bot被禁言
OnEventBotUnmuteEvent	BotUnmuteEvent	Bot被解除禁言
OnEventBotJoinGroupEvent	BotJoinGroupEvent	Bot加入新群
OnEventBotLeaveEventActive	BotLeaveEventActive	Bot主动退群
OnEventBotLeaveEventKick	BotLeaveEventKick	Bot被群踢出
OnEventNudgeEvent	NudgeEvent	Bot被戳一戳
OnEventBotInvitedJoinGroupRequestEvent	BotInvitedJoinGroupRequestEvent	Bot被邀请入群申请

        OnEventBotOnlineEvent	BotOnlineEvent	Mirai后台证实QQ已上线
OnEventBotOfflineEventActive	BotOfflineEventActive	Mirai后台证实QQ主动离线
OnEventBotOfflineEventForce	BotOfflineEventForce	Mirai后台证实QQ被挤下线
OnEventBotOfflineEventDropped	BotOfflineEventDropped	Mirai后台证实QQ由于网络问题掉线
OnEventBotReloginEvent	BotReloginEvent	Mirai后台证实QQ重新连接完毕

        OnFriendMessageReceive	FriendMessageSender, Message[]	接收到好友私聊信息
OnGroupMessageReceive	GroupMessageSender, Message[]	接收到群消息
OnTempMessageReceive	TempMessageSender, Message[]	接收到临时信息
OnStrangerMessageReceive	StrangerMessageSender, Message[]	接收到陌生人消息
OnOtherMessageReceive	OtherClientMessageSender, Message[]	接收到其他类型消息
OnFriendSyncMessageReceive	FriendSyncMessageSender, Message[]	接收到好友同步消息
OnGroupSyncMessageReceive	GroupSyncMessageSender, Message[]	接收到群同步消息
OnTempSyncMessageReceive	TempSyncMessageSender, Message[]	接收到临时同步消息
OnStrangerSyncMessageReceive	StrangerSyncMessageSender, Message[]	接收到陌生人同步消息

        MGetPlainString 获取消息中的所有字符集合
c.OnFriendMessageReceive += (s, e) =>
{
    if(s.id != qqid) //过滤自己发出的信息
    {
        var str = e.MGetPlainString();
        Console.WriteLine(str);
    }
};
2. MGetPlainString 获取消息中的所有字符集合并且使用(splitor参数)分割
c.OnFriendMessageReceive += (s, e) =>
{
    if(s.id != qqid) //过滤自己发出的信息
    {
        var str = e.MGetPlainStringSplit(); //默认使用空格分隔
        //var str = e.MGetPlainStringSplit(","); //使用逗号分割
        Console.WriteLine(str);
    }
};
3. MGetEachImageUrl 获取消息中的所有图片集合的Url
c.OnFriendMessageReceive += (s, e) =>
{
    if(s.id != qqid) //过滤自己发出的信息
    {
        var sx = e.MGetEachImageUrl();
        Console.WriteLine(sx[1].url);
    }
};
4. SendToFriend 信息类前置发送好友信息
new Message[] { new Plain("...") }.SendToFriend(qqnumber,c);
5. SendToGroup 信息类前置发送群信息
new Message[] { new Plain("...") }.SendToGroup(qqgroupnumber,c);
6. SendToTemp 信息类前置发送临时信息
new Message[] { new Plain("...") }.SendToTemp(qqnumber,qqgroupnumber,c);
7. SendMessage 对于 GenericModel 的群发信息逻辑
注:您也可以使用foreach对每个群/好友/群员发送

var msg = new Message[] { new Plain("...") };//要发送的信息

var fl = new FriendList().Send(c);//获取好友列表

fl[0].SendMessage(msg,c);//朝好友列表的1号好友发送信息(原生写法)
(fl[0], msg).SendMessage(c); //朝好友列表的1号好友发送信息(简单写法)

foreach(var i in fl) //朝好友列表的所有好友发送信息(原生写法)
{
    i.SendMessage(msg,c);
}

var gl = new GroupList().Send(c);//获取群列表
var gml = gl[0].GetMemberList(c);//获取群1的群员列表

gml[0].SendMessage(msg,c);//朝群1的1号群员发送msg信息(原生写法)
(gml[0], msg).SendMessage(c);//朝群1的1号群员发送msg信息(简单写法)

foreach(var i in gml) //朝群1的所有群员发送信息(原生写法)
{
    i.SendMessage(msg,c);
}

foreach(var i in gl) //朝所有群发送群信息(原生写法)
{
    i.SendMessage(msg,c);
}


        Instance	MessageId	SendMsgBack	Message[],
Opt ConClient?	往原处发送信息
Instance	async Task	SendMsgBackAsync	Message[],
Opt ConClient?	往原处发送信息
Instance	MessageId	SendMessageToFriend	Message[],
Opt ConClient?	强行往发送者的私聊发送信息
Instance	async Task	SendMessageToFriendAsync	Message[],
Opt ConClient?	强行往发送者的私聊发送信息
Instance	MessageId	SendMessageToGroup	Message[],
Opt ConClient?	强行往发送者的群发送信息(如果有)
Instance	async Task	SendMessageToGroupAsync	Message[],
Opt ConClient?	强行往发送者的群发送信息(如果有)



        _OnServeiceConnected	string	接收到WS连接成功信息
_OnServeiceError	Exception	接收到WS错误信息
_OnServiceDropped	string	接收到WS断连信息
_OnClientOnlineEvent	OtherClientOnlineEvent	接收到其他客户端上线通知
_OnOtherClientOfflineEvent	OtherClientOfflineEvent	接收到其他客户端下线通知
_OnCommandExecutedEvent	CommandExecutedEvent	接收到后端传送命令执行
_OnUnknownEvent	string	接收到后端传送未知指令
         * 
        var bp = new BotProfile().Send(c); //获取Bot资料
        var fp = new FriendProfile(qqnumber).Send(c);//获取好友资料
        var mp = new MemberProfile(qqgroup, qqnumber).Send(c);//获取群员资料
        var up = new UserProfile(qqnumber).Send(c);//获取用户资料
                                                   //获取群公告&&推送群公告
        var k = new Anno_list(qqgroup).Send(c);
        k[1].Delete(c);//删除群公告1 (快速写法)
        var k1 = new Anno_publish(qqgroup, "Bot 公告推送").Send(c);
        var k2 = new Anno_publish(qqgroup, "Bot 带图公告推送实验", imageUrl: "https://www.baidu.com/img/PCtm_d9c8750bed0b3c7d089fa7d55720d6cf.png").Send(c);
        */





        public void OnFriendMessageReceive(FriendMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
        {
            if (!Config.Instance.AllowPlayer(s.id)) return; // 黑名单
            Logger.Instance.Log($"好友信息 [qq:{s.id},昵称:{s.nickname},备注:{s.remark}] \n内容:{e.MGetPlainString()}", LogType.Mirai);
            var uinfo = Config.Instance.UserInfo(s.id);
            uinfo.Name = s.nickname;
            uinfo.Mark = s.remark;

            var sourceItem = e.First() as Source;
            HistoryManager.Instance.saveMsg(sourceItem.id, 0, s.id, e.MGetPlainString());
            string cmd = e.MGetPlainString();
           // if (string.IsNullOrWhiteSpace(cmd)) return;
            cmd = cmd.Trim();
            bool talked = false;

            //if (cmd.Length > 0)
            {
                List<string> res = new List<string>();
                foreach (var mod in Mods)
                {
                    var succeed = mod.HandleText(s.id, 0, cmd, res);
                    if (succeed)
                    {
                        break;
                    }

                    if (mod is ModWithMirai modWithMirai)
                    {
                        succeed = modWithMirai.OnFriendMessageReceive(s, e);
                        //modWithMirai.OnGroupMessageReceive;
                    }
                    if (succeed)
                    {
                        break;
                    }
                }


                foreach (var msg in res)
                {
                    var msgFilted = IOFilter.Instance.Filting(msg, FilterType.Normal);
                    if (string.IsNullOrWhiteSpace(msgFilted)) continue;

                    var output = new MeowMiraiLib.Msg.Type.Message[]
                    {
                        new Plain(msgFilted)
                    };
                    new FriendMessage(s.id, output).Send(ClientX);
                    talked = true;
                }

            }
            // update player info
            Player p = Config.Instance.UserInfo(s.id);
            p.Name = s.nickname;
            p.Mark = s.remark;



            // 计数统计
            if (talked)
            {
                p.UseTimes += 1;
                Config.Instance.App.Log.playTimePrivate += 1;
            }
        }



        public void OnGroupMessageReceive(GroupMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
        {
            if (!Config.Instance.AllowPlayer(s.id) || !Config.Instance.AllowGroup(s.group.id)) return; // 黑名单
            if (e == null) return;
            var sourceItem = e.First() as Source;
            Logger.Instance.Log($"[{sourceItem.id}]群({s.group.id})信息 [qq:{s.id},昵称:{s.memberName}] \n内容:{e.MGetPlainString()}", LogType.Mirai);
            HistoryManager.Instance.saveMsg(sourceItem.id, s.group.id, s.id, e.MGetPlainString());
            var uinfo = Config.Instance.UserInfo(s.id);
            uinfo.Name = s.memberName;
            

            // 检查群聊是否需要bot回复
            bool isAtMe = false;
            string cmd = e.MGetPlainString().Trim();
            if (cmd.Length > 0)
            {

            }
            foreach (var v in e)
            {
                if (v.type == "At" && ((MeowMiraiLib.Msg.Type.At)v).target == Config.Instance.App.Avatar.myQQ)
                {
                    isAtMe = true;
                    break;
                }
            }
            if (Config.Instance.isAskMe(cmd))
            {
                isAtMe = true;
                cmd = cmd.Substring(Config.Instance.App.Avatar.askName.Length);
            }
            List<string> res = new List<string>();


            // 调用各个mod来处理
            bool succeed = false;
            foreach (var mod in Mods)
            {
                // text模式则要看有at才传递被截断提示词后的cmd
                if (isAtMe)
                {
                    succeed = mod.HandleText(s.id, s.group.id, cmd, res);
                    if (succeed)
                    {
                        break;
                    }
                }

                // mirai-mod则不检查是否at我，直接原文传递即可
                if (mod is ModWithMirai modWithMirai)
                {
                    succeed = modWithMirai.OnGroupMessageReceive(s, e);
                    //modWithMirai.OnGroupMessageReceive;
                }
                
                if (succeed)
                {
                    break;
                }
            }

            // thie part sendout res message array to Mirai server
            var rres = res.ToArray();
            int sendNum = 0;
            foreach (var r in rres)
            {
                var textFilted = IOFilter.Instance.Filting(r, FilterType.Normal);
                if (string.IsNullOrWhiteSpace(textFilted)) continue;
                var output = new List<MeowMiraiLib.Msg.Type.Message>();
                if (sendNum == 0)
                {
                    output.Add(new At(s.id, s.memberName));
                }
                output.Add(new Plain(textFilted));


                new GroupMessage(s.group.id, output.ToArray()).Send(ClientX);


                sendNum++;
            }

            // update player info
            Playgroup p = Config.Instance.GroupInfo(s.group.id);
            p.Name = s.group.name;
            if (sendNum > 0)
            {
                //p.UseTimes += 1;
                Config.Instance.UserInfo(s.id).UseTimes += 1;
                Config.Instance.App.Log.playTimeGroup += 1;
            }
        }

        void ServiceConnected(string e)
        {
            Logger.Instance.Log($"***连接成功：{e}", LogType.Mirai);

            



            //Logger.Instance.Log($"更新好友列表和群列表...");
            //RefreshFriendList();
            //Logger.Instance.Log($"更新完毕，找到{Config.Instance.friends.Count}个好友，{Config.Instance.groups.Count}个群...");



        }

        void OnServeiceError(Exception e)
        {
            Logger.Instance.Log($"***连接出错：{e.Message}\r\n{e.StackTrace}", LogType.Mirai);
        }

        void OnServiceDropped(string e)
        {
            Logger.Instance.Log($"***连接中断：{e}", LogType.Mirai);
        }

        void OnClientOnlineEvent(OtherClientOnlineEvent e)
        {
            Logger.Instance.Log($"***其他平台登录（标识：{e.id}，平台：{e.platform}", LogType.Mirai);
        }
        void OnEventBotInvitedJoinGroupRequestEvent(BotInvitedJoinGroupRequestEvent e)
        {
            Logger.Instance.Log($"受邀进群（用户：{e.fromId}，群：{e.groupName}({e.groupId})消息：{e.message}", LogType.Mirai);
            var g = Config.Instance.GroupInfo(e.groupId);
            var u = Config.Instance.UserInfo(e.fromId);
            if (g.Is("黑名单") || u.Is("黑名单"))
            {
                e.Deny(ClientX, "非好友不接受邀请谢谢");
                return;
            }
            if (Config.Instance.friends.ContainsKey(e.fromId) || u.Is("管理员") || u.Is("好友") || e.fromId == Config.Instance.App.Avatar.adminQQ)
            {
                e.Grant(ClientX);
                return;
            }
        }

        void OnEventNewFriendRequestEvent(NewFriendRequestEvent e)
        {
            Logger.Instance.Log($"好友申请：{e.nick}({e.fromId})(来自{e.groupId})消息：{e.message}", LogType.Mirai);
            if (!string.IsNullOrWhiteSpace(e.message) && e.message.StartsWith(Config.Instance.App.Avatar.askName))
            {
                e.Grant(ClientX, "来了来了");
                var user = Config.Instance.UserInfo(e.fromId);
                user.Name = e.nick;
                //user.Mark = e.nick;
                user.Tags.Add("好友");
                user.Type = PlayerType.Normal;
            }
            else
            {
                e.Deny(ClientX,"密码错误");
            }
        }

        void OnEventFriendNickChangedEvent(FriendNickChangedEvent e)
        {
            Logger.Instance.Log($"好友改昵称（{e.friend.id}，{e.from}->{e.to}", LogType.Mirai);
            var user = Config.Instance.UserInfo(e.friend.id);
            user.Name = e.to;

        }



        private void tbMmdk_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void tbMirai_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void 清空日志ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tbLog.Clear();
        }



        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                foreach (var mod in Mods)
                {
                    try
                    {
                        mod.Exit();
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Log(ex);
                    }

                }
                HistoryManager.Instance.Dispose();
                Config.Instance.Save();

                Logger.Instance.Close();
                //Environment.Exit(0);

                State = BotRunningState.exit;
            }
            catch
            {

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            Logger.Instance.OnBroadcastLogEvent += HandleShowLog;
            Logger.Instance.Log("开始初始化配置文件。");

            Config.Instance.Load();
            bool isValid = checkAndSetConfigValid();
            if (!isValid)
            {
                Logger.Instance.Log("配置文件读取失败，中止运行");
                return;
            }


            Logger.Instance.Log("配置文件读取完毕。");


            Task.Run(() =>
            {
                while (State != BotRunningState.exit)
                {
                    UpdateMonitorInfo(); // 更新状态

                    // 控制更新频率，比如每秒更新一次
                    Thread.Sleep(200);
                }
            });
            //new Thread(workMonitor).Start();
        }

        private void StartBot()
        {
            //button1.Enabled = false;

            Task.Run(() =>
            {
                workRunBot();
            });

            textInputTest.Focus();
        }

        /// <summary>
        /// 模拟bot的输入
        /// </summary>
        /// <param name="message"></param>
        public void virtualInput(string message)
        {
            long userId;
            long groupId;
            bool isAtMe = false;

            if (IsVirtualGroup)
            {

                userId = -1;
                groupId = 1;
                textLocalTestGroup.AppendText($"[me]:{message}\r\n");

                if (Config.Instance.isAskMe(message))
                {
                    isAtMe = true;
                    message = message.Substring(Config.Instance.App.Avatar.askName.Length);
                }
            }
            else
            {

                userId = -1;
                groupId = 0;
                textLocalTest.AppendText($"[me]:{message}\r\n");

                isAtMe = true;
            }



            try
            {

                List<string> res = new List<string>();
                foreach (var mod in Mods)
                {
                    if (!isAtMe) break;
                    var succeed = mod.HandleText(userId, groupId, message, res);
                    if (succeed)
                    {
                        break;
                    }
                }
                foreach (var result in res)
                {
                    virtualOutput(result);
                }
            }
            catch (Exception ex)
            {
                textLocalTest.AppendText($"[error]:{ex.Message}\r\n{ex.StackTrace}\r\n");
            }


        }



        public void virtualOutput(string result)
        {
            if (IsVirtualGroup)
            {
                textLocalTestGroup.AppendText($"[bot]:{result}\r\n");
            }
            else
            {
                textLocalTest.AppendText($"[bot]:{result}\r\n");
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string msg = textInputTest.Text.Trim();
            textInputTest.Clear();
            virtualInput(msg);

        }

        private void textInputTest_TextChanged(object sender, EventArgs e)
        {
            if (IsEnterAutoSend && textInputTest.Text.EndsWith("\n"))
            {
                string msg = textInputTest.Text.Trim();
                textInputTest.Clear();
                virtualInput(msg);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void 启动botToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartBot();
            启动botToolStripMenuItem.Enabled = false;
            启动botToolStripMenuItem.Text = "（正在运行）";
        }

        private void 存档当前配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Instance.Save();
            Logger.Instance.Log($"已储存当前配置");
        }


        /// <summary>
        /// 更新显示界面信息
        /// </summary>
        private void UpdateMonitorInfo()
        {
            if (State != BotRunningState.exit)
            {
                var cpu = Config.Instance.systemInfo.CpuLoad;
                var mem = 100.0 - ((double)Config.Instance.systemInfo.MemoryAvailable * 100 / Config.Instance.systemInfo.PhysicalMemory);
                try
                {
                    Invoke((Action)(() =>
                    {

                        lbCPU.Text = $"CPU\n({cpu.ToString(".0")}%)";
                        lbMem.Text = $"内存\n({mem.ToString(".0")}%)";
                        lbBeginTime.Text = $"{beginTime.ToString("yyyy-MM-dd")}\r\n{beginTime.ToString("HH:mm:ss")}";
                        lbTimeSpan.Text = $"{(DateTime.Now - beginTime).Days}天\r\n{(DateTime.Now - beginTime).Hours}小时{(DateTime.Now - beginTime).Minutes}分{(DateTime.Now - beginTime).Seconds}秒";
                        lbQQ.Text = $"{Config.Instance.App.Avatar.myQQ}";
                        lbPort.Text = $"{Config.Instance.App.IO.MiraiPort}";
                        lbVersion.Text = $"{Config.Instance.App.Version}\n({StaticUtil.GetBuildDate().ToString("F")})";
                        lbUpdateTime.Text = $"{Util.StaticUtil.GetBuildDate().ToString("yyyy-MM-dd")}";


                        lbFriendNum.Text = $"{Config.Instance.players.Count}";
                        lbGroupNum.Text = $"{Config.Instance.playgroups.Count}";
                        lbUseNum.Text = $"{Config.Instance.App.Log.playTimePrivate + Config.Instance.App.Log.playTimeGroup}";

                        pbCPU.Value = (int)(cpu);
                        pbMem.Value = (int)(mem);


                    }));
                }
                catch (Exception ex)
                {

                }



                Thread.Sleep(500);     // 1s
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            IsEnterAutoSend = checkBox1.Checked;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                // private
                IsVirtualGroup = false;
                button2.Text = "发送（私聊）";
            }
            else
            {
                IsVirtualGroup = true;
                button2.Text = "发送（群组）";
            }
        }

        private void 清空私聊窗口ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textLocalTest.Clear();
        }

        private void 清空群聊窗口ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textLocalTestGroup.Clear();
        }


        private void DealFilterFile(string filterSourceName)
        {
            string filterCompressed = Config.Instance.ResourceFullPath(filterSourceName);
            string filterRaw = $"{filterCompressed}.raw";
            try
            {

                bool hasC = System.IO.File.Exists(filterCompressed);
                bool hasR = System.IO.File.Exists(filterRaw);

                if (!hasC && !hasR)
                {
                    System.IO.File.WriteAllText(filterRaw, "example1 \nexapmle2 => fff\n", Encoding.UTF8);
                    Logger.Instance.Log($"初始化过滤器{filterRaw}，请用记事本按规则编辑该过滤器文件，然后再点按钮加载之进bot。重启后生效");
                }
                if (hasC && !hasR)
                {
                    FileManager.DecompressFile(filterCompressed, filterRaw);
                    Logger.Instance.Log($"准备编辑过滤词。{filterRaw}，请用记事本按规则编辑该过滤器文件，然后再点按钮加载之进bot。重启后生效");
                }
                if (!hasC && hasR)
                {
                    FileManager.CompressFile(filterRaw, filterCompressed);
                    Logger.Instance.Log($"已用{filterRaw}刷新了过滤器文件{filterCompressed}。重启后生效");
                }
                if (hasC && hasR)
                {
                    FileManager.CompressFile(filterRaw, filterCompressed);
                    Logger.Instance.Log($"已用{filterRaw}刷新了过滤器文件{filterCompressed}。重启后生效");
                }
            }

            catch (Exception ex)
            {
                Logger.Instance.Log($"刷新过滤器{filterCompressed}出错：{ex.Message}\r\n{ex.StackTrace}");
            }

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DealFilterFile("FilterNormal");
            DealFilterFile("FilterStrict");
        }


        private async void workTest()
        {
            //var opt = new ChatGptUnofficialOptions
            //{
            //    BaseUrl = "http://127.0.0.1:8000/switch-model",
            //    Model = "rwkv"
            //};
            var opt2 = new ChatGptOptions();
            opt2.BaseUrl = "http://127.0.0.1:8000";
            opt2.Model = "rwkv2";
            opt2.Temperature = 0.9; // Default: 0.9;
            opt2.TopP = 1.0; // Default: 1.0;
            opt2.MaxTokens = 100; // Default: 64;
            opt2.Stop = ["User:"]; // Default: null;
            opt2.PresencePenalty = 0.5; // Default: 0.0;
            opt2.FrequencyPenalty = 0.5; // Default: 0.0;

            var bot = new ChatGpt("", opt2);

            // get response
            string ask = $"你好?有没有旅行建议？他说：";
            Logger.Instance.Log($"[AskAI]{ask}");
            var response = await bot.Ask(ask);
            Logger.Instance.Log($"[AI]{response}");
            //Console.WriteLine(response);


            // get response for a specific conversation
            //response = await bot.Ask("今天天气如何", "conversation name");
            //Logger.Instance.Log($"[AI]{response}");
            //Console.WriteLine(response);


        }

        private void 测试gptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            workTest();
        }
    }

}
