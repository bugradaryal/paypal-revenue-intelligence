using PRI.Domain.AiGateway.Entities;

namespace PRI.Domain.AiGateway.Interfaces;

public interface IUsageRecordRepository
{
    void Add(UsageRecord record);
}
