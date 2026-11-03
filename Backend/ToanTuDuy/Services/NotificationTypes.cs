namespace ToanTuDuy.Services
{
    public static class NotificationTypes
    {
        public const string System = "SYSTEM";
        public const string Tuition = "TUITION";
        public const string Academic = "ACADEMIC";
        public const string Attendance = "ATTENDANCE";
        public const string Announcement = "ANNOUNCEMENT";
    }

    public static class NotificationSettingTypes
    {
        public const string InApp = "IN_APP_NOTIFICATION";
        public const string Email = "EMAIL_NOTIFICATION";
        public const string Push = "PUSH_NOTIFICATION";
        public const string Sms = "SMS_ALERT";
        public const string WeeklyReport = "REPORT_WEEKLY";

        public static readonly string[] All =
        {
            InApp,
            Email,
            Push,
            Sms,
            WeeklyReport,
        };
    }
}
