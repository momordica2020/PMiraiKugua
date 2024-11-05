using MeowMiraiLib.Msg.Type;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Input;
using Timer = System.Timers.Timer;

namespace MMDK.Util
{


    /// <summary>
    /// 单条历史记录
    /// </summary>
    public class MessageHistory
    {
        public long messageId;      // 只存入内存，用于at和回撤等操作。
        public long userid;
        public string message;
        public DateTime date;

        public MessageHistory()
        {
            // groupid = -1;
            userid = -1;
            message = "";
        }

        public MessageHistory(long _messageId, long _uid, string _message)
        {
            date = DateTime.Now;
            messageId = _messageId;
            userid = _uid;
            message = _message;
        }

        public override string ToString()
        {
            return $"{date:yyyy-MM-dd_HH:mm:ss}\t{userid}\t{message}";
        }
    }



    /// <summary>
    /// 某个人或组的全部历史记录
    /// </summary>
    public class MessageHistoryGroup
    {
        public string filePath = "";
        public long uid;
        public bool isGroup;
        public Queue<MessageHistory> history = new Queue<MessageHistory>();

        public MessageHistoryGroup(string _rootpath, long _gid, bool _isGroup)
        {
            isGroup = _isGroup;
            uid = _gid;
            filePath = $"{_rootpath}/{(isGroup ? HistoryManager.pathGroup : HistoryManager.pathPrivate)}/{_gid}.txt";
        }

        public void addMessage(long messageId, long user, string message)
        {
            MessageHistory h = new MessageHistory(messageId, user, message);
            history.Enqueue(h);
        }


        static readonly int maxWriteTime = 100;
        public static DateTime maxWriteDate;
        public void write()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                int nowtime = 0;
                while (history.Count > 0)
                {
                    // 只把日期老于maxWriteDate的历史记录归档
                    var checkpoint = history.Peek();
                    if (checkpoint.date > maxWriteDate) break;

                    sb.AppendLine(history.Dequeue().ToString());

                    // 每次不写入超过maxWriteTime条
                    if (nowtime++ >= maxWriteTime) break;
                }
                if (sb.Length > 0)
                {
                    System.IO.File.AppendAllText(filePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
        }
    }


    /// <summary>
    /// 管理历史记录数据
    /// </summary>
    public class HistoryManager
    {
        private static readonly Lazy<HistoryManager> instance = new Lazy<HistoryManager>(() => new HistoryManager());

        public string path;

        public System.Timers.Timer writeHistoryTask;
        //public object savemsgMutex = new object();

        Dictionary<string, MessageHistoryGroup> history = new Dictionary<string, MessageHistoryGroup>();

        public static string pathGroup = "group";
        public static string pathPrivate = "private";

        private HistoryManager()
        {

        }

        public static HistoryManager Instance => instance.Value;

        public void Init(string _path)
        {
            path = _path;

            try
            {
                string path = Config.Instance.ResourceFullPath("HistoryPath");
                if (!Directory.Exists(path))
                {
                    Logger.Instance.Log($"新建历史记录文件夹，路径是{path}", LogType.Debug);
                    Directory.CreateDirectory(path);
                }
                if (!Directory.Exists($"{path}/{pathGroup}"))Directory.CreateDirectory($"{path}/{pathGroup}");
                if (!Directory.Exists($"{path}/{pathPrivate}")) Directory.CreateDirectory($"{path}/{pathPrivate}");
            }
            catch(Exception e) {

                Logger.Instance.Log(e, LogType.System);
            }


            // 每10秒一归档
            writeHistoryTask = new Timer(1000 * 10);
            writeHistoryTask.Start();
            writeHistoryTask.Elapsed += workDealHistory;
        }

        public void Dispose()
        {
            if (writeHistoryTask != null)
            {
                writeHistoryTask.Stop();     // 停止定时器
                writeHistoryTask.Dispose();


                // 立即把没归档的都归档
                MessageHistoryGroup.maxWriteDate = DateTime.Now.AddHours(1);
                workDealHistory(null, null);
            }
            
        }

        private void workDealHistory(object sender, ElapsedEventArgs e)
        {

            try
            {
                Logger.Instance.Log($"把日期前的聊天记录归档中：{MessageHistoryGroup.maxWriteDate.ToString("F")}", LogType.Debug);
                var items = history.Values.AsParallel().ToArray();
                //var items = history.Values.ToArray();
                for (int i = 0; i < items.Length; i++)
                {
                    items[i].write();
                }

                MessageHistoryGroup.maxWriteDate = DateTime.Now.AddHours(-1);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex, LogType.Debug);
            }

        }



        /// <summary>
        /// 记录群/私人聊天信息到文件中
        /// </summary>
        /// <param name="sourceId">MessageId</param>
        /// <param name="group"></param>
        /// <param name="user"></param>
        /// <param name="msg"></param>
        public void saveMsg(long sourceId, long group, long user, string msg)
        {
            try
            {
                bool isGroup = group > 0;
                long uid = isGroup ? group : user;
                string key = isGroup ? $"G{group}" : $"P{user}";
                //Logger.Instance.Log($"==={key},{sourceId},{user},{msg}");
                if (!history.ContainsKey(key)) history[key] = new MessageHistoryGroup(path, uid, isGroup);
                history[key].addMessage(sourceId, user, msg);


            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex, LogType.Debug);
            }
        }

        public MessageHistory[] findMessage(long userId, long groupId, string keyWord="")
        {
            List<MessageHistory> results = new List<MessageHistory>();

            try
            {
                Logger.Instance.Log($"=?={userId},{groupId},{(history.ContainsKey($"G{groupId}"))}");
                if (history.TryGetValue($"G{groupId}", out MessageHistoryGroup g))
                {
                    var lines = g.history.ToArray();
                    //return lines;

                    foreach(var line in lines)
                    {
                        if (line.userid == userId)
                        {
                            if (line.message.Contains(keyWord)) results.Add(line);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Instance.Log(ex, LogType.Debug);
            }

            return results.ToArray();
        }
    }
}
