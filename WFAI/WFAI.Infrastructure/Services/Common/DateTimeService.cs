using WFAI.Application.Interfaces.Common;

namespace WFAI.Infrastructure.Services.Common
{
    public class DateTimeService : IDateTimeService
    {
        public DateTime NowUtc => DateTime.UtcNow;
    }
}