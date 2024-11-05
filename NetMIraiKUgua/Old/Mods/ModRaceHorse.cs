using MeowMiraiLib;
using MeowMiraiLib.Msg;
using MeowMiraiLib.Msg.Sender;
using MeowMiraiLib.Msg.Type;
using Microsoft.VisualBasic.ApplicationServices;
using MMDK.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Documents;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace MMDK.Mods
{

    /// <summary>
    /// 赛马模块
    /// </summary>
    class ModRaceHorse : Mod, ModWithMirai
    {
        private static readonly Lazy<ModRaceHorse> instance = new Lazy<ModRaceHorse>(() => new ModRaceHorse());
        public static ModRaceHorse Instance => instance.Value;
        private ModRaceHorse()
        {


        }




        //string matchPath = "DataRacehorse/match/";
        object matchMutex = new object();

        Client client = null;


        public Dictionary<long, RHUser> users = new Dictionary<long, RHUser>();
        public Dictionary<string, RHHorse> horses = new Dictionary<string, RHHorse>();
        public Dictionary<long, RHMatch> matches = new Dictionary<long, RHMatch>();

        public TimeSpan raceBegin = new TimeSpan(21, 0, 0);
        public TimeSpan raceEnd = new TimeSpan(23, 0, 0);


        public bool Init(string[] args)
        {
            lock (matchMutex)
            {
                try
                {
                    var lines = FileManager.ReadResourceLines("RaceUser");
                    foreach (var line in lines)
                    {
                        RHUser user = new RHUser();
                        user.parse(line);
                        users[user.id] = user;
                    }
                    lines = FileManager.ReadResourceLines("RaceHorse");
                    foreach (var line in lines)
                    {
                        RHHorse horse = new RHHorse(line);
                        horses[horse.name] = horse;
                    }



                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(ex);
                }
            }
            return true;
        }

        public void Exit()
        {
            try
            {
                foreach (var match in matches)
                {
                    match.Value.StopRaceLoop();
                }
                save();
            }
            catch { }

        }

        public bool HandleText(long userId, long groupId, string cmd, List<string> results)
        {
            try
            {   
                if (string.IsNullOrWhiteSpace(cmd)) return false;

                if (!users.ContainsKey(userId)) users[userId] = new RHUser(userId);
                RHUser user = users[userId];

                //BOT.log("赛马 "+cmd);
                cmd = cmd.Trim();
                var cmdFilter = Regex.Match(cmd, @"^赛马(介绍|玩法)", RegexOptions.Singleline);
                if (cmdFilter.Success)
                {
                    results.Add(getIntroduction());
                    return true;
                }

                cmdFilter = Regex.Match(cmd, @"^个人信息", RegexOptions.Singleline);
                if (cmdFilter.Success)
                {
                    results.Add($"{ModBank.Instance.getUserInfo(userId)}\r\n{getRHInfo(userId)}");
                    return true;
                }


                cmdFilter = Regex.Match(cmd, @"^赛马", RegexOptions.Singleline);
                if (cmdFilter.Success)
                {
                    int num = 5;
                    int len = 100;
                    if (!matches.ContainsKey(groupId)) matches[groupId] = new RHMatch(groupId);
                    matches[groupId].ReStart(num, len);
                    return true;
                }

                cmdFilter = Regex.Match(cmd, @"^胜率榜", RegexOptions.Singleline);
                if (cmdFilter.Success)
                {
                    results.Add($"{showBigWinner()}");
                    return true;
                }

                cmdFilter = Regex.Match(cmd, @"^败率榜", RegexOptions.Singleline);
                if (cmdFilter.Success)
                {
                    results.Add($"{showBigLoser()}");
                    return true;
                }

                cmdFilter = Regex.Match(cmd, @"^赌狗榜", RegexOptions.Singleline);
                if (cmdFilter.Success)
                {
                    results.Add($"{showMostPlayTime()}");
                    return true;
                }

                cmdFilter = Regex.Match(cmd, @"^(\d+)号(\d+)", RegexOptions.Singleline);
                if (cmdFilter.Success)
                {
                    try
                    {
                        int roadnum = 0;
                        int money = 0;
                        if (int.TryParse(cmdFilter.Groups[1].Value, out roadnum)
                         && int.TryParse(cmdFilter.Groups[2].Value, out money))
                        {
                            if (matches.TryGetValue(groupId, out var matchInfo))
                            {
                                string result = matchInfo.bet(user, roadnum, money);
                                if (string.IsNullOrWhiteSpace(result)) return false;
                                results.Add(result);
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Log(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }

            return false;
        }

        public void InitMiraiClient(Client _client)
        {
            client  = _client;
        }

        public bool OnFriendMessageReceive(FriendMessageSender s, Message[] e)
        {
            return false;
        }

        public bool OnGroupMessageReceive(GroupMessageSender s, Message[] e)
        {
            return false;
        }

        public void save()
        {
            lock (matchMutex)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var user in users.Values)
                    {
                        sb.Append($"{user.ToString()}\r\n");
                    }
                    FileManager.writeText(Config.Instance.ResourceFullPath("RaceUser"), sb.ToString());
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(ex);
                }
            }
        }






        public string isAllow(long group)
        {
            DateTime time = DateTime.Now;
            DateTime nightRaceBegin = new DateTime(time.Year, time.Month, time.Day) + raceBegin;
            DateTime nightRaceEnd = new DateTime(time.Year, time.Month, time.Day) + raceEnd;

            if (time >= nightRaceBegin && time <= nightRaceEnd)
            {
                return "";
            }
            string res = $"夜间赛事起止时间为{nightRaceBegin.ToString("HH:mm")}-{nightRaceEnd.ToString("HH:mm")}";
            return res;
        }



        public string showBigWinner()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                var users = this.users.Values.ToList();
                users.Sort((left, right) =>
                {
                    if (left.getWinPercent() < right.getWinPercent())
                        return 1;
                    else if (left.getWinPercent() == right.getWinPercent())
                    {
                        if (left.getPlayTime() < right.getPlayTime()) return 1;
                        else if (left.getPlayTime() > right.getPlayTime()) return -1;
                        else return 0;
                    }
                    else
                        return -1;
                });
                sb.Append("赛 🐎 胜 率 榜 \r\n");
                int showtime = 0;
                int index = 0;
                int maxnum = 10;
                ulong mintime = 5;
                while (showtime < maxnum && index < users.Count)
                {
                    ulong playtime = users[index].wintime + users[index].losetime;
                    if (playtime > mintime)
                    {
                        sb.Append($"{showtime + 1}:{Config.Instance.UserInfo(users[index].id).Name},{Math.Round(users[index].getWinPercent(), 2)}%({users[index].wintime}/{playtime})\r\n");
                        showtime += 1;
                    }
                    index += 1;
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            return "";
            //save();
        }

        public string showBigLoser()
        {
            try
            {
                var users = this.users.Values.ToList();
                users.Sort((left, right) =>
                {
                    if (left.getLosePercent() < right.getLosePercent())
                        return 1;
                    else if (left.getWinPercent() == right.getWinPercent())
                    {
                        if (left.getPlayTime() < right.getPlayTime()) return 1;
                        else if (left.getPlayTime() > right.getPlayTime()) return -1;
                        else return 0;
                    }
                    else
                        return -1;
                });

                StringBuilder sb = new StringBuilder();
                sb.Append("赛 🐎 败 率 榜 \r\n");
                int showtime = 0;
                int index = 0;
                int maxnum = 10;
                ulong mintime = 5;
                while (showtime < maxnum && index < users.Count)
                {
                    ulong playtime = users[index].wintime + users[index].losetime;
                    if (playtime > mintime)
                    {
                        sb.Append($"{showtime + 1}:{Config.Instance.UserInfo(users[index].id).Name},{Math.Round(users[index].getLosePercent(), 2)}%({users[index].losetime}/{playtime})\r\n");
                        showtime += 1;
                    }
                    index += 1;
                }

                //for (int i = 0; i < Math.Min(users.Count, 10); i++)
                //{
                //    sb.Append($"{i + 1}:{getQQNick(users[i].qq)},{Math.Round(users[i].getLosePercent(), 2)}%({users[i].losetime}/{users[i].wintime + users[i].losetime})\r\n");
                //}
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            return "";

        }


        /// <summary>
        /// 赌狗榜
        /// </summary>
        public string showMostPlayTime()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("赛 🐎 赌 狗 榜 \r\n");

                int maxnum = 10;
                var users = this.users.Values.ToList();
                users.Sort((left, right) =>
                {
                    if (left.getPlayTime() < right.getPlayTime())
                        return 1;
                    else if (left.getPlayTime() == right.getPlayTime())
                        return 0;
                    else
                        return -1;
                });
                for (int i = 0; i < Math.Min(users.Count, maxnum); i++)
                {
                    sb.Append($"{i + 1}:{Config.Instance.UserInfo(users[i].id).Name},赌了{users[i].wintime + users[i].losetime}次\r\n");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            return "";
        }

        /// <summary>
        /// 个人赌马记录
        /// </summary>
        /// <param name="userqq"></param>
        /// <returns></returns>
        public string getRHInfo(long userqq)
        {
            if (!users.ContainsKey(userqq)) users[userqq] = new RHUser(userqq);
            var u = users[userqq];
            return $"您在赌马上消费过{u.hrmoney}枚{ModBank.unitName}，共下注{u.losetime + u.wintime}场，赢{u.wintime}场，胜率{Math.Round(u.getWinPercent(), 2)}%";
            //outputMessage(group, userqq, $"您在赌马上消费过{u.hrmoney}枚{BTCActor.unitName}，共下注{u.losetime+u.wintime}场，赢{u.wintime}场，胜率{Math.Round(u.getWinPercent(), 2)}%");
        }

        public string getIntroduction()
        {
            return $"赛🐎游戏介绍：\r\n" +
                $"输入“赛马”开始一局比赛\r\n" +
                $"在比赛开始时会有下注时间，输入x号y可以向x号马下注y元\r\n" +
                $"比赛开始后自动演算，比赛期间不接收指令\r\n" +
                $"其他指令包括“签到”“个人信息”“富豪榜”“穷人榜”“胜率榜”“败率榜”“赌狗榜”";
        }

        //public RHUser getUser(long id)
        //{
        //    return users[id];
        //}



        public List<RHHorse> getHorseInfos()
        {
            return horses.Values.ToList();
        }

        


        internal void showMessage(long groupId, int userId, string s)
        {
            new GroupMessage(groupId, [
                new Plain(s)
                ]).Send(client);
        }
    }



    class RHUser
    {
        public long id;
        //public BTCUser user;
        public ulong hrmoney = 0;
        public ulong wintime = 0;
        public ulong losetime = 0;

        public RHUser(long _id = -1)
        {
            id = _id;
            //user = _user;
            //hrmoney = _hrmoney;
            //wintime = _wintime;
            //losetime = _losetime;
        }


        public void parse(string line)
        {
            try
            {
                var items = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length >= 4)
                {
                    id = long.Parse(items[0].Trim());
                    hrmoney = ulong.Parse(items[1]);
                    wintime = ulong.Parse(items[2]);
                    losetime = ulong.Parse(items[3]);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }

        }

        public override string ToString()
        {
            return $"{id}\t{hrmoney}\t{wintime}\t{losetime}";
        }

        public double getWinPercent()
        {
            if (wintime + losetime <= 0) return 0;
            return (double)100 * wintime / (double)(wintime + losetime);
        }

        public double getLosePercent()
        {
            if (wintime + losetime <= 0) return 0;
            return (double)100 * losetime / (double)(wintime + losetime);
        }

        public ulong getPlayTime()
        {
            return wintime + losetime;
        }
    }

    class RHHorse
    {
        /// <summary>
        /// 🐎的样子
        /// </summary>
        public string emoji = "";       

        /// <summary>
        /// 🐎显示的名称
        /// </summary>
        public string name = "";

        /// <summary>
        /// 最小速度
        /// </summary>
        public int minspeed = 0;

        /// <summary>
        /// 最大速度
        /// </summary>
        public int maxspeed = 0;

        /// <summary>
        /// 技能类型
        /// </summary>
        public int triggerType = 0;

        /// <summary>
        /// 技能参数
        /// </summary>
        public int triggerParam = 0;

        /// <summary>
        /// 技能特效
        /// </summary>
        public string triggerEmoji = "";

        public RHHorse()
        {
            //name = _name;
            //emoji = _emoji;
            //minspeed = _minspeed;
            //maxspeed = _maxspeed;
            //triggerType = _triggerType;
            //triggerParam = _triggerParam;
            //triggerEmoji = _triggerEmoji;
        }

        public RHHorse(string str)
        {
            parse(str);
        }

        public void parse(string line)
        {
            try
            {
                var items = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length >= 7)
                {
                    emoji = items[0];
                    name = items[1];
                    minspeed = int.Parse(items[2]);
                    maxspeed = int.Parse(items[3]);
                    triggerType = int.Parse(items[4]);
                    triggerParam = int.Parse(items[5]);
                    triggerEmoji = items[6];
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
        }

        public override string ToString()
        {
            return $"{name}\t{emoji}\t{minspeed}\t{maxspeed}\t{triggerType}\t{triggerParam}\t{triggerEmoji}";
        }

        public int getNextStep()
        {
            return MyRandom.Next(minspeed, maxspeed);
        }
    }

    class RHBuff
    {
        public string emoji;
        public int type;
        public int para;
        public int lefttime;
        public int speedAdd = 0;

        public RHBuff(string _emoji, int _type, int _para, int _lefttime)
        {
            emoji = _emoji;
            type = _type;
            para = _para;
            lefttime = _lefttime;
        }
    }

    class RHRoad
    {
        public RHHorse horse;
        public int num;
        public int nowlen;
        public RHBuff buff;

        public RHRoad(int _num, RHHorse _horse)
        {
            num = _num;
            horse = _horse;
            nowlen = 0;
        }
    }

    enum RHStatus
    {
        Idling,   // 未开始
        Betting,    // 下注时间
        Playing,    // 比赛时间
        Finishing     // 结果通报
    }

    class RHMatch
    {
        // getQQNickHandler getQQNick;
        //  public sendQQGroupMsgHandler showScene;
        
        // 单个用户可用下多个赛道的赌注
        Dictionary<RHUser, Dictionary<int, long>> bets = new Dictionary<RHUser, Dictionary<int, long>>();
        Dictionary<int, RHRoad> roads = new Dictionary<int, RHRoad>();

        long id = -1;  //用qq群号作为比赛唯一标识，避免同一个群同时多局
        int roadnum = 0;
        int roadlen = 0;

        int MaxBetTime = 2;
        // public int maxTurn;
        //public int turn;

        RHStatus currentState;

        const int betWaitTime = 30;    // 单位是秒
        const int turnWaitTime = 3;
        const int GameoverTime = 1;
        int nowF = 0;
        int winnerRoad = 0;
        string skillDescription = "";

        Timer raceLoopTimer = null;
        static readonly int loopSpanMs = 1000;

        public RHMatch(long _id)
        {
            id = _id;
            currentState = RHStatus.Idling;

            
            raceLoopTimer = new Timer(loopSpanMs);
            raceLoopTimer.Elapsed += OnTimedEvent;
            raceLoopTimer.AutoReset = false; // 设置定时器自动重置
            raceLoopTimer.Enabled = true; // 启动定时器

            //raceLoopTimer.Start();
        }

        /// <summary>
        /// 启动game，可以指定赛道数量和跑道长度
        /// </summary>
        /// <param name="_roadnum"></param>
        /// <param name="_roadlen"></param>
        /// <returns></returns>
        public bool ReStart(int _roadnum, int _roadlen)
        {
            try
            {
                if (currentState != RHStatus.Idling)
                {
                    // 尚未完赛
                    return false;
                }
                roadnum = _roadnum;
                roadlen = _roadlen;
                winnerRoad = 0;
                nowF = 0;
                roads.Clear();
                bets.Clear();
                InitHorses(ModRaceHorse.Instance.getHorseInfos());
                currentState = RHStatus.Betting;
                skillDescription = "";
                
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }

            return true;
        }



        /// <summary>
        /// 仅在要清理全部比赛信息时才调用
        /// 将清空所有动态数据和赌注，也不会返还已下的资金
        /// </summary>
        public void StopRaceLoop()
        {
            try
            {
                //status = RHStatus.Idling;
                if (raceLoopTimer != null)
                {
                    raceLoopTimer.Stop();
                    raceLoopTimer.Dispose(); // 清理定时器
                }
                roads.Clear();
                bets.Clear();
                nowF = 0;
                skillDescription = "";
            }
            catch { }


        }

        /// <summary>
        /// 给赛道分配🐎
        /// </summary>
        /// <param name="_horses"></param>
        public void InitHorses(List<RHHorse> _horses)
        {
            if (_horses != null && _horses.Count > 0)
            {
                for (int i = 1; i <= roadnum; i++)
                {
                    roads[i] = new RHRoad(i, _horses[MyRandom.Next(_horses.Count)]);
                }
            }
        }

        /// <summary>
        /// 下注
        /// </summary>
        /// <param name="betUser"></param>
        /// <param name="roadnum"></param>
        /// <param name="betMoney"></param>
        /// <returns></returns>
        public string bet(RHUser betUser, int roadnum, long betMoney)
        {
            try
            {
                
                if (currentState != RHStatus.Betting || betMoney <= 0 || betUser==null) return "";

                if (roadnum <= 0 || roadnum > this.roadnum) return $"没有第{roadnum}条赛道";

                long userHadMoney = ModBank.Instance.GetMoney(betUser.id);
                if (userHadMoney <= 0) return $"一分钱都没有，下你🐎的注呢？";


                if (!bets.ContainsKey(betUser)) bets[betUser] = new Dictionary<int, long>();

                if (bets[betUser].Keys.Count >= MaxBetTime && !bets[betUser].ContainsKey(roadnum))
                {
                    return $"最多押{MaxBetTime}匹，你已经押了{string.Join("、", bets[betUser].Keys)}。";
                }


                string res = "";
                if(userHadMoney <= betMoney)
                {
                    betMoney = userHadMoney;
                    res = $"All in!把手上的{userHadMoney}枚{ModBank.unitName}都押了{roadnum}号马";
                }
                else
                {
                    res = $"成功在{roadnum}号马下注{betMoney}枚{ModBank.unitName}"; 
                }
                string outMsg = "";
                long tranResult = ModBank.Instance.TransMoney(betUser.id, Config.Instance.BotQQ, betMoney, out outMsg);
                if (tranResult == betMoney)
                {
                    // 转账成功
                    betUser.hrmoney += (ulong)betMoney;
                    if (!bets[betUser].ContainsKey(roadnum)) bets[betUser][roadnum] = 0;
                    bets[betUser][roadnum] += betMoney;

                    res += $"，账户余额{ModBank.Instance.GetMoney(betUser.id)}";
                }
                else
                {
                    // 转账失败
                    res = $"下注失败：{outMsg}";
                }
                return res;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
                return $"ERROR:{ex.Message}";
            }

        }

        /// <summary>
        /// 计算当前帧的比赛进度
        /// </summary>
        private void nextLoop()
        {
            winnerRoad = 0;
            int winnerlen = -1;

            // clear old buffs
            foreach (var road in roads)
            {
                if (road.Value.buff == null) continue;
                road.Value.buff.lefttime -= 1;
                if (road.Value.buff.lefttime <= 0)
                {
                    road.Value.buff = null;
                }
            }
            // skill test
            if (MyRandom.Next(100) < 20)
            {
                // play a skill!
                int skillNum = MyRandom.Next(1, roadnum + 1);
                if (roads.ContainsKey(skillNum))
                {
                    var road = roads[skillNum];
                    if (road.horse.triggerType != 0)
                    {
                        switch (road.horse.triggerType)
                        {
                            case 1:
                                // 自身加速
                                skillDescription = $"{road.num}号马突然开始加速！";
                                road.buff = new RHBuff(road.horse.triggerEmoji, road.horse.triggerType, road.horse.triggerParam, 1);
                                road.buff.speedAdd = road.horse.triggerParam;
                                break;
                            case 2:
                                // 第一减速
                                skillDescription = $"{road.num}号马累了！";
                                road.buff = new RHBuff(road.horse.triggerEmoji, road.horse.triggerType, road.horse.triggerParam, 1);
                                road.buff.speedAdd = road.horse.triggerParam;
                                break;
                            default: break;
                        }
                    }
                }


            }

            for (int i = 1; i <= roadnum; i++)
            {
                var road = roads[i];
                int oristep = road.horse.getNextStep();
                int addstep = road.buff == null ? 0 : road.buff.speedAdd;
                int realstep;
                realstep = oristep + addstep;
                road.nowlen += realstep;
                if (road.nowlen > roadlen && road.nowlen > winnerlen)
                {
                    winnerRoad = i;
                    winnerlen = road.nowlen;
                }
            }
        }

        /// <summary>
        /// 结算
        /// 分账规则：总奖金=所有人下注金额+庄下注金额（苦瓜对所有押单匹的1赔4，押两匹的1赔2）
        /// 胜利者拿到（其对应倍率的赔付+其他人下注总额分成）*（1-抽水比例%）
        /// 多个胜利者，则每人的分成是均分失败者下注总额
        /// </summary>
        /// <param name="winnerroad"></param>
        /// <returns></returns>
        public string calBetResult(int winnerroad)
        {
            StringBuilder sb = new StringBuilder();


            //foreach (var bet in bets.Values) foreach (var money in bet.Values) allmoney += money;
            List<(RHUser user, double multi, long betMoney)> winners = new List<(RHUser user, double multi, long betMoney)>();

            long loserMoneys = 0;
            foreach (var bet in bets)
            {
                var betUser = bet.Key;
                var betList = bet.Value;
                double multi = -1;
                long winBetMoney = 0;
                long loseBetMoney = 0;
                foreach (var betpair in betList)
                {
                    
                    if (betpair.Key == winnerroad)
                    {
                        // 猜中了
                        winBetMoney += betpair.Value; // 猜中项的本金
                        if (bet.Value.Count == 1)
                        {
                            // 只押了一匹，倍率
                            multi = 5.0;
                        }
                        else if (bet.Value.Count >= 2)
                        {
                            //两匹 
                            multi = 3.0;
                        }
                    }
                    else
                    {
                        loseBetMoney += betpair.Value; // 一去不回的钱
                    }
                }
                if (winBetMoney > 0)
                {
                    // 赢家
                    winners.Add((betUser, multi, winBetMoney));
                    betUser.wintime += 1;
                }
                else
                {
                    // 输家
                    betUser.losetime += 1;
                }
                loserMoneys += loseBetMoney;
            }


            if (winners.Count <= 0)
            {
                sb.Append($"很遗憾，本场无人猜中！本场入账{loserMoneys}。");
                // 已经预先转账了，这里不需要再入账 ModBank.Instance.TransMoney()
                // 钱入苦瓜账上
                
            }
            else
            {
                // 分账
                double rakeP = 0.05;    // 抽水5%

                // 这里判断如果我苦账上钱不够了，则只把现有的钱有多少分多少瓜分给用户
                long allNeed = 0;
                foreach (var winner in winners)
                {
                    allNeed  += (long)((winner.multi * winner.betMoney + loserMoneys / winners.Count) * (1 - rakeP));
                    Logger.Instance.Log($"[{winner.user.id}]{allNeed}--{winner.multi}*{winner.betMoney} + {loserMoneys}/{winners.Count}");
                }
                
                if(ModBank.Instance.GetMoney(Config.Instance.BotQQ) < allNeed)
                {
                    // 账上钱不够了
                    sb.Append($"{Config.Instance.BotName}账上钱不够了，这次先欠着!!!!!!!!!!");
                }
                else
                {
                    foreach (var winner in winners)
                    {
                        var money = (long)((winner.multi * winner.betMoney + loserMoneys / winners.Count) * (1 - rakeP));
                        string msg;
                        long res = ModBank.Instance.TransMoney(Config.Instance.BotQQ, winner.user.id, money, out msg);
                        sb.Append($"{Config.Instance.UserInfo(winner.user.id).Name}赢了{money}枚{ModBank.unitName}！恭喜\n");
                        if (res == 0)
                        {
                            // failed
                            sb.Append($"{res}");

                        }
                    }
                }
            }
            sb.Append($"目前总奖池{ModBank.Instance.GetMoney(Config.Instance.BotQQ)}");
            return sb.ToString();
        }
       
        
        
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                switch (currentState)
                {
                    case RHStatus.Idling:
                        // 未开始
                        nowF = -1;
                        break;

                    case RHStatus.Betting:
                        HandleBetting();
                        break;

                    case RHStatus.Playing:
                        HandlePlaying();
                        break;

                    case RHStatus.Finishing:
                        HandleFinishing();
                        break;

                    default:
                        break;
                }
                nowF += 1;

            }
            catch (Exception ex)
            {
                //Logger.Instance.Log(ex, LogType.Debug);
            }
            raceLoopTimer.Start();
        }


        private void HandleBetting()
        {
            if (nowF == 0)
            {
                string message = $"现在是赛🐎比赛下注时间，请下注您看好的马（输入赛道对应数字）。比赛将于{betWaitTime}秒后自动开始\r\n";
                foreach (var road in roads.Values)
                {
                    message += $"{road.num}号：{road.horse.emoji} {road.horse.name}\r\n";
                }
                ModRaceHorse.Instance.showMessage(id, -1, message);
            }
            else
            {
                if (nowF >= betWaitTime)
                {
                    nowF = -1;
                    currentState = RHStatus.Playing;
                }
            }
            
        }


        private void HandlePlaying()
        {
            if (nowF == 0)
            {
                ModRaceHorse.Instance.showMessage(id, -1, "赛🐎比赛正式开始！！");
                ModRaceHorse.Instance.showMessage(id, -1, getMatchScene());
                nowF = 1;
                return;
            }
            else if (nowF >= turnWaitTime)
            {
                nextLoop();
                ModRaceHorse.Instance.showMessage(id, -1, getMatchScene());

                if (winnerRoad > 0)
                {
                    currentState = RHStatus.Finishing;
                    nowF = -1;
                } 
            }
        }


        private void HandleFinishing()
        {
            ModRaceHorse.Instance.showMessage(id, -1, $"比赛结束！{winnerRoad}号马赢了！");
            ModRaceHorse.Instance.showMessage(id, -1, calBetResult(winnerRoad));

            // Reset for the next race
            
            winnerRoad = -1;
            nowF = -1;
            currentState = RHStatus.Idling;
            ModRaceHorse.Instance.save(); 
        }




        /// <summary>
        /// 当前赛场画面
        /// </summary>
        /// <returns></returns>
        public string getMatchScene()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("🏁\r\n");
            int len = 40;
            for (int i = 1; i <= roadnum; i++)
            {
                sb.Append(i);
                if (i != winnerRoad) sb.Append("|");
                int space = (int)(len * (1 - (double)roads[i].nowlen / roadlen));
                if (space > 0) sb.Append(' ', space);
                sb.Append(roads[i].horse.emoji);
                if (roads[i].buff != null) sb.Append(roads[i].buff.emoji);
                sb.Append("\r\n");
            }
            if (!string.IsNullOrWhiteSpace(skillDescription)) sb.Append(skillDescription + "\r\n");
            skillDescription = "";

            return sb.ToString();
        }


    }

}
