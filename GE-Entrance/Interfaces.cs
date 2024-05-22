using System.Collections.Generic;

namespace GE_Entrance
{
    public interface Interfaces
    {
        string CardSeqNumber { get; set; }
        string CompanyName { get; set; }
        bool ConfiscateCard { get; set; }
        List<Form1.ServiceUsage> Entries { get; set; }
        string FirstUse { get; set; }
        bool HasEntryInLastPeriod { get; set; }
        bool IsExpired { get; set; }
        bool IsInactive { get; set; }
        bool IsSingleUse { get; set; }
        string OwnerName { get; set; }
        string ServiceName { get; set; }
        int TotalEntries { get; set; }
        bool Valid { get; set; }
        string ValidTo { get; set; }
    }
}