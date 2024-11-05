using Microsoft.AspNetCore.Mvc;
using MMDK.Util;
using MMDK.Mods;
using MeowMiraiLib;
using static MeowMiraiLib.Client;
using MeowMiraiLib.Msg;
using MeowMiraiLib.Msg.Sender;
using MeowMiraiLib.Msg.Type;
using MeowMiraiLib.Event;

namespace KuguaAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        public bool isInit = false;
        DateTime beginTime;
        public static MeowMiraiLib.Client? ClientX;
        public List<Mod> Mods = new List<Mod>();


        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<ChatController> _logger;



        public ChatController(ILogger<ChatController> logger)
        {
            _logger = logger;


            if (!isInit) Init();
        }



        [HttpGet(Name = "GetChat")]
        public string Chat(string input)
        {
            
            return $"GET:{input}";
        }
        //public IEnumerable<Chat> Get()
        //{
        //    if (!isInit) Init();
        //    return Enumerable.Range(1, 5).Select(index => new Chat
        //    {
        //        Summary = Summaries[MyRandom.Next(Summaries.Length)]
        //    })
        //    .ToArray();
        //}



       // [HttpGet(Name = "GetInit")]
        bool Init()
        {
            try
            {
                Logger.Instance.OnBroadcastLogEvent += HandleShowLog;
                Logger.Instance.Log("开始初始化配置文件。");

                Config.Instance.Load();
                bool isValid = checkAndSetConfigValid();
                if (!isValid)
                {
                    Logger.Instance.Log("配置文件读取失败，中止运行");
                    return false;
                }

                Logger.Instance.Log("配置文件读取完毕。");


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
                        if (mod is ModWithMirai modWithMirai)
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

                isInit = true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            return isInit;
        }





        /// <summary>
        /// 检查并初始化配置
        /// </summary>
        /// <returns></returns>
        private bool checkAndSetConfigValid()
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

        private  void HandleShowLog(LogInfo info)
        {
            int maxlen = 100000;
            try
            {
                _logger.LogInformation(info.ToDescription());
                //Invoke((Action)(() =>
                //{
                //    tbLog.AppendText($"{info.ToDescription()}\r\n");
                //    if (tbLog.TextLength > maxlen)
                //    {
                //        tbLog.Text = tbLog.Text.Substring(tbLog.TextLength - maxlen);
                //    }
                //    tbLog.ScrollToCaret();
                //}));
            }
            catch (Exception ex)
            {
                //Logger.Instance.Log(ex);
            }

        }


        private void OnFriendMessageReceive(FriendMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
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
                    new MeowMiraiLib.Msg.FriendMessage(s.id, output).Send(ClientX);
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



        private void OnGroupMessageReceive(GroupMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
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


                new MeowMiraiLib.Msg.GroupMessage(s.group.id, output.ToArray()).Send(ClientX);


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

        private void ServiceConnected(string e)
        {
            Logger.Instance.Log($"***连接成功：{e}", LogType.Mirai);





            //Logger.Instance.Log($"更新好友列表和群列表...");
            //RefreshFriendList();
            //Logger.Instance.Log($"更新完毕，找到{Config.Instance.friends.Count}个好友，{Config.Instance.groups.Count}个群...");



        }

        private void OnServeiceError(Exception e)
        {
            Logger.Instance.Log($"***连接出错：{e.Message}\r\n{e.StackTrace}", LogType.Mirai);
        }

        private void OnServiceDropped(string e)
        {
            Logger.Instance.Log($"***连接中断：{e}", LogType.Mirai);
        }

        private void OnClientOnlineEvent(MeowMiraiLib.Event.OtherClientOnlineEvent e)
        {
            Logger.Instance.Log($"***其他平台登录（标识：{e.id}，平台：{e.platform}", LogType.Mirai);
        }

        private void OnEventBotInvitedJoinGroupRequestEvent(MeowMiraiLib.Event.BotInvitedJoinGroupRequestEvent e)
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

        private void OnEventNewFriendRequestEvent(MeowMiraiLib.Event.NewFriendRequestEvent e)
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
                e.Deny(ClientX, "密码错误");
            }
        }

        private void OnEventFriendNickChangedEvent(MeowMiraiLib.Event.FriendNickChangedEvent e)
        {
            Logger.Instance.Log($"好友改昵称（{e.friend.id}，{e.from}->{e.to}", LogType.Mirai);
            var user = Config.Instance.UserInfo(e.friend.id);
            user.Name = e.to;

        }


    }
}
