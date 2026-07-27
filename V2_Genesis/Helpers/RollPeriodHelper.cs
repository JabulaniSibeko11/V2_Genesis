using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Helpers
{
    public class RollPeriodHelper
    {
        public static string GetPeriodStatus(RollDateEntry? d)
        {
            if (d is null) return "unknown";

            var now = DateTime.Now;

            if (now < d.OpenDate) return "upcoming";
            if (now <= d.VisibleUntil) return "active";

            return "closed";
        }
    }
}
