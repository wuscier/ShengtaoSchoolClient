using System;
using St.Common;

namespace St.Meeting
{
    public class MeetingService : IMeeting
    {

        public MeetingService()
        {
            
        }

        public event Action<bool, string> StartMeetingCallbackEvent;
        public event Action<bool, string> ExitMeetingCallbackEvent;


        public void StartMeeting()
        {
            MeetingView mv = new MeetingView(StartMeetingCallbackEvent, ExitMeetingCallbackEvent);
            //MeetingView mv =
            //    DependencyResolver.Current.Container.Resolve<MeetingView>(
            //        new TypedParameter(typeof(Action<bool, string>), StartMeetingCallbackEvent),
            //        new TypedParameter(typeof(Action<bool, string>), ExitMeetingCallbackEvent));
            mv.Show();
        }
    }
}
