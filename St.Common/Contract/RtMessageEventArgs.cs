using System;

namespace St.Common.Contract
{
    public class RtMessageEventArgs : EventArgs
    {
        /// <summary>
        /// 消息发送者
        /// </summary>
        public string OpenId { get; set; }
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }
    }
}
