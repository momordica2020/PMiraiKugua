using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatGPT.Net;
using MeowMiraiLib;
using MMDK.Util;

namespace MMDK.Mods
{
    /// <summary>
    /// 银行
    /// </summary>
    public class ModBank : Mod
    {
        private static readonly Lazy<ModBank> instance = new Lazy<ModBank>(() => new ModBank());
        public static ModBank Instance => instance.Value;
        private ModBank()
        {


        }

        public static string unitName = "马币";





        public bool Init(string[] args)
        {

            return true;
        }

        public void Exit()
        {
            
        }

        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {
            bool isGroup = groupId > 0;
            if (string.IsNullOrWhiteSpace(message)) return false;
            message = message.Trim();
            // 货币系统
            if (message == "签到")
            {
                message = DailyAttendance(groupId, userId);
                //racehorse.dailyAttendance(group, user);
                results.Add(message);
                return true;
            }
            var match = new Regex("^(富人榜|富豪榜)").Match(message);
            if (match.Success)
            {
                string res = $"{showRichest()}";
                results.Add(res);
                return true;
            }

            match = new Regex("^(穷人榜)").Match(message);
            if (match.Success)
            {
                string res = $"{showPoorest()}";
                results.Add(res);
                return true;
            }


            match = new Regex(@"给(.+)转(\d+)").Match(message);
            if (match.Success)
            {
                try
                {
                    string target = match.Groups[1].ToString().Trim();
                    long targetqq = -1;
                    if (!long.TryParse(target, out targetqq))
                    {
                        // nick name -> qq
                        //targetqq = bank.getID(target, msg.fromGroup);
                        // targetqq = getQQNumFromGroup(group, target.Trim());
                    }
                    string res = "";
                    if (targetqq <= 0)
                    {
                        res = $"系统里找不到昵称 {target} ，转账失败。可以输入qq号码直接转";
                    }
                    else
                    {
                        long money = long.Parse(match.Groups[2].ToString());
                        long succeedMoney = TransMoney(userId, targetqq, money, out res);
                    }

                    if (!string.IsNullOrWhiteSpace(res))
                    {
                        results.Add(res);
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }














        /// <summary>
        /// 每日签到，领取低保
        /// </summary>
        /// <param name="group"></param>
        /// <param name="userqq"></param>
        public string DailyAttendance(long group, long userqq)
        {
            var u = Config.Instance.UserInfo(userqq);
            if (u.LastSignTime < DateTime.Today)
            {
                int maxmoney = 114;
                int minmoney = 30;
                // success
                long money = MyRandom.Next(minmoney, maxmoney);
                u.Money += money;
                u.LastSignTime = DateTime.Now;
                u.SignTimes += 1;
                return $"您今日领取失业补助{money}枚{unitName}，现在账上一共{u.Money}枚";
            }
            else
            {
                return $"嗨嗨嗨，今天领过了";
            }
        }



        public string getUserInfo(long userqq)
        {
            var u = Config.Instance.UserInfo(userqq);
            return $"您的账上共有{u.Money}枚{unitName}。共领取失业补助{u.SignTimes}次，今日失业补助{(u.LastSignTime >= DateTime.Today ? "已领取" : "还未领取")}";
        }

        /// <summary>
        /// 查看账户余额
        /// </summary>
        /// <param name="userqq"></param>
        /// <returns></returns>
        public long GetMoney(long userqq)
        {
            var user = Config.Instance.UserInfo(userqq);
            return user.Money;
        }

        /// <summary>
        /// 转账
        /// </summary>
        /// <param name="fromqq">发起转账的用户QQ</param>
        /// <param name="targetqq">接收转账的用户QQ</param>
        /// <param name="money">转账金额</param>
        /// <param name="message">转账结果信息</param>
        /// <returns>成功钱数，失败为0</returns>
        public long TransMoney(long fromqq, long targetqq, long money, out string message)
        {
            message = "";
            if (money <= 0)
            {
                message = "只允许正向转账";
            }

            var user1 = Config.Instance.UserInfo(fromqq);
            var user2 = Config.Instance.UserInfo(targetqq);

            if (user1.Money < money)
            {
                message = $"您的余额不足。当前余额{user1.Money}{unitName}";
                return 0;
            }

            message = $"您向{targetqq}转了{money}枚{unitName}，";
            long user1OldMoney = user1.Money;
            long user2OldMoney = user2.Money;
            try
            {
                
                checked
                {
                    user1.Money -= money;
                    user2.Money += money;
                }
            }
            catch (OverflowException)
            {
                message += $"转账失败：{user1}或{user2}的{unitName}溢出，所转数额{money}，发起者余额{user1.Money}，接收者余额{user2.Money}。";
                Logger.Instance.Log(message);
                user1.Money = user1OldMoney; // 恢复余额
                user2.Money = user2OldMoney; // 恢复余额
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
                message += $"银行被橄榄了，你钱没了！请带截图联系bot管理者{Config.Instance.App.Avatar.adminQQ}";
                return 0;
            }
            message += $"转账成功，您的{unitName}余额{user1.Money}，对方余额{user2.Money}";

            return money;
        }

        public bool ProcessTransfer(Player user1, Player user2, long money)
        {
            


           

            // 可以记录转账记录
            // WriteRecord(new BankRecord(user1.QQ, user2.QQ, money, "转账", "成功"));
            return true;


        }





        /// <summary>
        /// 富人榜
        /// </summary>
        /// <returns></returns>
        public string showRichest()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                int maxnum = 10;
                var users = Config.Instance.players.Values.ToList();
                users.Sort((left, right) =>
                {
                    return -1 * left.Money.CompareTo(right.Money);
                });

                sb.Append("富 豪 榜 \r\n");
                for (int i = 0; i < Math.Min(users.Count, maxnum); i++)
                {
                    sb.Append($"{i + 1}:{users[i].Name},{users[i].Money}枚\r\n");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
                return "";
            }
        }



        /// <summary>
        /// 穷人榜
        /// </summary>
        /// <returns></returns>
        public string showPoorest()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                int maxnum = 10;
                var users = Config.Instance.players.Values.ToList();
                users.Sort((left, right) =>
                {
                    return left.Money.CompareTo(right.Money);
                });

                sb.Append("穷 人 榜 \r\n");
                for (int i = 0; i < Math.Min(users.Count, maxnum); i++)
                {
                    sb.Append($"{i + 1}:{users[i].Name},{users[i].Money}枚\r\n");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
                return "";
            }
        }



    }


    #region 遗弃



    /// <summary>
    /// 转账记录
    ///  * 暂时不用
    /// </summary>
    class BankRecord
    {
        public long src;
        public long tar;
        public DateTime time;
        public long money;
        public string reason;
        public string result;

        public BankRecord()
        {
            src = -1;
            tar = -1;
            time = DateTime.Now;
            money = 0;
            reason = "";
            result = "";
        }

        public BankRecord(long _src, long _tar, long _money, string _reason, string _result)
        {
            src = _src;
            tar = _tar;
            money = _money;
            time = DateTime.Now;
            reason = _reason;
            result = _result;
        }

        public BankRecord(string line)
        {
            parse(line);
        }

        public void parse(string line)
        {
            try
            {
                var items = line.Trim().Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (items.Length >= 6)
                {
                    src = long.Parse(items[0]);
                    tar = long.Parse(items[1]);
                    time = DateTime.ParseExact(items[2], "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
                    money = long.Parse(items[3]);
                    reason = items[4];
                    result = items[5];
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
        }

        public override string ToString()
        {
            return $"{src}\t{tar}\t{time.ToString("yyyy-MM-dd HH:mm:ss")}\t{money}\t{reason}\t{result}";
        }
    }
    
    
    
    #endregion


}
