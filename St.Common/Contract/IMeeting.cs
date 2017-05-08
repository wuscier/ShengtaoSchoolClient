using System;

namespace St.Common
{
    public interface IMeeting
    {
        void StartMeeting();
        event Action<bool, string> StartMeetingCallbackEvent;
        event Action<bool, string> ExitMeetingCallbackEvent;
    }
}
