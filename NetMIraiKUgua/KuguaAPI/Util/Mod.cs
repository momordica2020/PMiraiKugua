using MeowMiraiLib.Msg.Sender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDK.Util
{

    #region Mod相关接口
    public interface Mod
    {
        /// <summary>
        /// Mod初始化，只调用一次
        /// </summary>
        /// <param name="args">可选的传入参数</param>
        /// <returns></returns>
        public bool Init(string[] args);


        /// <summary>
        /// Mod退出清理，在bot关闭时调用一次
        /// </summary>
        public void Exit();

        /// <summary>
        /// Mod的文本处理接口
        /// </summary>
        /// <param name="userId">用户QQ</param>
        /// <param name="groupId">群QQ</param>
        /// <param name="message">输入的文本内容</param>
        /// <param name="results">传出返回文本序列</param>
        /// <returns>返回是否已处理，true表示该模块已经对信息进行了处理并截断后续其他模块的处理流程</returns>
        public bool HandleText(long userId, long groupId, string message, List<string> results);


    }

    public interface ModWithMirai
    {

        /// <summary>
        /// 这类模块直接让他跟mirai客户端去连接吧，不管了
        /// </summary>
        /// <param name="client"></param>
        public void InitMiraiClient(MeowMiraiLib.Client _client);

        public bool OnFriendMessageReceive(FriendMessageSender s, MeowMiraiLib.Msg.Type.Message[] e);
        public bool OnGroupMessageReceive(GroupMessageSender s, MeowMiraiLib.Msg.Type.Message[] e);
    }

    #endregion

}
