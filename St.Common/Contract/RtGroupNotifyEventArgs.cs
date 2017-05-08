using System.Collections.Generic;

namespace St.Common.Contract
{
    public class RtGroupNotifyEventArgs
    {
        public RtGroupNotifyEventArgs()
        {
            this.Users = new HashSet<string>();
        }

        public string Code { get; set; }
        public HashSet<string> Users { get; set; }
    }
}
