using PRI.Domain.AiGateway.Entities;

namespace PRI.Domain.AiGateway.Interfaces;

/// <summary>Benzerlik aramasında yalnızca gereken alanlar yüklenir (yanıt metni yok).</summary>
public sealed record CacheCandidate(Guid Id, float[] Embedding, string NumbersFingerprint);

public interface ISemanticCacheRepository
{
    /// <summary>Süresi dolmamış, aynı istemci ve bağlamdaki tam eşleşen en yeni kayıt (takip edilen).</summary>
    Task<SemanticCacheEntry?> FindExactAsync(
        Guid clientId, string contextHash, string queryHash, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>Embedding'i olan, süresi dolmamış en yeni adaylar (aynı istemci ve bağlam).</summary>
    Task<IReadOnlyList<CacheCandidate>> ListCandidatesAsync(
        Guid clientId, string contextHash, DateTime nowUtc, int maxCandidates, CancellationToken cancellationToken);

    Task<SemanticCacheEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(SemanticCacheEntry entry);

    /// <summary>Tüm kayıtları siler; silinen sayıyı döndürür.</summary>
    Task<int> DeleteAllAsync(CancellationToken cancellationToken);
}

/// <summary>Append-only arama kayıtları.</summary>
public interface ICacheLookupRepository
{
    void Add(CacheLookup lookup);
}

public interface IAiBudgetRepository
{
    /// <summary>Tek bütçe kaydı (takip edilen); tanımlı değilse null.</summary>
    Task<AiBudget?> GetAsync(CancellationToken cancellationToken);

    void Add(AiBudget budget);
}
