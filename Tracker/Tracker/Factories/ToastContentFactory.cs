using Microsoft.Toolkit.Uwp.Notifications;
using Tracker.Common.Enums;

namespace Tracker.Factories
{
    public static class ToastContentFactory
    {
        public static bool TryGetToastContent(ToastNotificationAction type, out ToastContentBuilder? builder)
        {
            builder = new ToastContentBuilder();
            {
                switch (type)
                {
                    case ToastNotificationAction.SqlLoginFailure:
                        builder.AddText("Sql login failed");
                        builder.AddText(
                            "There was a problem logging into the server. Contact your system administrator");
                        builder.AddArgument("conversationId", 6969);
                        return true;
                    case ToastNotificationAction.SqlLoginSuccess:
                        builder.AddText("Logged in to Tracker");
                        builder.AddText("Successfully logged into the Tracker database");
                        builder.AddArgument("conversationId", 6970);
                        return true;
                }
            }
            builder = null;
            return false;
        }
    }
}
