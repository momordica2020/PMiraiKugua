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
                Logger.Instance.Log("��ʼ��ʼ�������ļ���");

                Config.Instance.Load();
                bool isValid = checkAndSetConfigValid();
                if (!isValid)
                {
                    Logger.Instance.Log("�����ļ���ȡʧ�ܣ���ֹ����");
                    return false;
                }

                Logger.Instance.Log("�����ļ���ȡ��ϡ�");


                Logger.Instance.Log($"��ȡ�����ļ�...");
                Config.Instance.Load();

                Logger.Instance.Log($"���ù�����...");
                IOFilter.Instance.Init();



                Logger.Instance.Log($"��ʼ����bot...");
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
                    new ModRandomChat(),    // �������������β

                };



                //bot = new MainProcess();
                //bot.Init(config);



                if (true)
                {
                    // ����ʷ��¼����������İ�
                    string HistoryPath = Config.Instance.ResourceFullPath("HistoryPath");
                    if (!Directory.Exists(HistoryPath)) Directory.CreateDirectory(HistoryPath);
                    Logger.Instance.Log($"��ʷ��¼������ {HistoryPath} ��");
                    HistoryManager.Instance.Init(HistoryPath);
                }
                else
                {
                    Logger.Instance.Log($"��ʷ��¼�����м�¼");
                }

                foreach (var mod in Mods)
                {
                    try
                    {
                        if (mod.Init(null))
                        {
                            Logger.Instance.Log($"ģ��{mod.GetType().Name}�ѳ�ʼ��");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Log(ex);
                    }
                }

                Logger.Instance.Log($"bot�������");


                //mirai = new MiraiLink();
                if (Config.Instance.App.IO.MiraiRun)
                {

                    //string verifyKey = "123456";
                    string connectUri = $"{Config.Instance.App.IO.MiraiWS}:{Config.Instance.App.IO.MiraiPort}/all?qq={Config.Instance.App.Avatar.myQQ}";
                    Logger.Instance.Log($"��������Mirai...{connectUri}");
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

                    Logger.Instance.Log($"����GPT�ӿ�...");
                    GPT.Instance.Init(ClientX);

                    ClientX.OnFriendMessageReceive += OnFriendMessageReceive;
                    ClientX.OnGroupMessageReceive += OnGroupMessageReceive;
                    ClientX.OnEventBotInvitedJoinGroupRequestEvent += OnEventBotInvitedJoinGroupRequestEvent;
                    ClientX.OnEventNewFriendRequestEvent += OnEventNewFriendRequestEvent;
                    ClientX.OnEventFriendNickChangedEvent += OnEventFriendNickChangedEvent;


                }
                else
                {
                    Logger.Instance.Log($"������Mirai����������Ӧ��");

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
        /// ��鲢��ʼ������
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
            if (!Config.Instance.AllowPlayer(s.id)) return; // ������
            Logger.Instance.Log($"������Ϣ [qq:{s.id},�ǳ�:{s.nickname},��ע:{s.remark}] \n����:{e.MGetPlainString()}", LogType.Mirai);
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



            // ����ͳ��
            if (talked)
            {
                p.UseTimes += 1;
                Config.Instance.App.Log.playTimePrivate += 1;
            }
        }



        private void OnGroupMessageReceive(GroupMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
        {
            if (!Config.Instance.AllowPlayer(s.id) || !Config.Instance.AllowGroup(s.group.id)) return; // ������
            if (e == null) return;
            var sourceItem = e.First() as Source;
            Logger.Instance.Log($"[{sourceItem.id}]Ⱥ({s.group.id})��Ϣ [qq:{s.id},�ǳ�:{s.memberName}] \n����:{e.MGetPlainString()}", LogType.Mirai);
            HistoryManager.Instance.saveMsg(sourceItem.id, s.group.id, s.id, e.MGetPlainString());
            var uinfo = Config.Instance.UserInfo(s.id);
            uinfo.Name = s.memberName;


            // ���Ⱥ���Ƿ���Ҫbot�ظ�
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


            // ���ø���mod������
            bool succeed = false;
            foreach (var mod in Mods)
            {
                // textģʽ��Ҫ����at�Ŵ��ݱ��ض���ʾ�ʺ��cmd
                if (isAtMe)
                {
                    succeed = mod.HandleText(s.id, s.group.id, cmd, res);
                    if (succeed)
                    {
                        break;
                    }
                }

                // mirai-mod�򲻼���Ƿ�at�ң�ֱ��ԭ�Ĵ��ݼ���
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
            Logger.Instance.Log($"***���ӳɹ���{e}", LogType.Mirai);





            //Logger.Instance.Log($"���º����б��Ⱥ�б�...");
            //RefreshFriendList();
            //Logger.Instance.Log($"������ϣ��ҵ�{Config.Instance.friends.Count}�����ѣ�{Config.Instance.groups.Count}��Ⱥ...");



        }

        private void OnServeiceError(Exception e)
        {
            Logger.Instance.Log($"***���ӳ���{e.Message}\r\n{e.StackTrace}", LogType.Mirai);
        }

        private void OnServiceDropped(string e)
        {
            Logger.Instance.Log($"***�����жϣ�{e}", LogType.Mirai);
        }

        private void OnClientOnlineEvent(MeowMiraiLib.Event.OtherClientOnlineEvent e)
        {
            Logger.Instance.Log($"***����ƽ̨��¼����ʶ��{e.id}��ƽ̨��{e.platform}", LogType.Mirai);
        }

        private void OnEventBotInvitedJoinGroupRequestEvent(MeowMiraiLib.Event.BotInvitedJoinGroupRequestEvent e)
        {
            Logger.Instance.Log($"������Ⱥ���û���{e.fromId}��Ⱥ��{e.groupName}({e.groupId})��Ϣ��{e.message}", LogType.Mirai);
            var g = Config.Instance.GroupInfo(e.groupId);
            var u = Config.Instance.UserInfo(e.fromId);
            if (g.Is("������") || u.Is("������"))
            {
                e.Deny(ClientX, "�Ǻ��Ѳ���������лл");
                return;
            }
            if (Config.Instance.friends.ContainsKey(e.fromId) || u.Is("����Ա") || u.Is("����") || e.fromId == Config.Instance.App.Avatar.adminQQ)
            {
                e.Grant(ClientX);
                return;
            }
        }

        private void OnEventNewFriendRequestEvent(MeowMiraiLib.Event.NewFriendRequestEvent e)
        {
            Logger.Instance.Log($"�������룺{e.nick}({e.fromId})(����{e.groupId})��Ϣ��{e.message}", LogType.Mirai);
            if (!string.IsNullOrWhiteSpace(e.message) && e.message.StartsWith(Config.Instance.App.Avatar.askName))
            {
                e.Grant(ClientX, "��������");
                var user = Config.Instance.UserInfo(e.fromId);
                user.Name = e.nick;
                //user.Mark = e.nick;
                user.Tags.Add("����");
                user.Type = PlayerType.Normal;
            }
            else
            {
                e.Deny(ClientX, "�������");
            }
        }

        private void OnEventFriendNickChangedEvent(MeowMiraiLib.Event.FriendNickChangedEvent e)
        {
            Logger.Instance.Log($"���Ѹ��ǳƣ�{e.friend.id}��{e.from}->{e.to}", LogType.Mirai);
            var user = Config.Instance.UserInfo(e.friend.id);
            user.Name = e.to;

        }


    }
}
