using System;

namespace St.Common.Contract
{
    public class RtSignOutEventArgs : EventArgs
    {
        public string OpenId { get; set; }
        public dynamic Data { get; set; }
    }
}
