# API Sözleşmesi

**Sürüm:** 0.3 · **Güncelleme:** 2026-11-07 · **Sahip:** Backend

Frontend ve backend için tek doğruluk kaynağı. `API_NEEDS.md` ile bu dosya çeliştiğinde bu dosya geçerlidir. Bir endpoint yalnızca **Hazır** bölümündeyse gerçek olarak kullanılır; **Planlı** bölümündekiler için frontend mock adapter kullanır. Her API değişikliği aşağıdaki değişiklik kaydına işlenir.

## Değişiklik kaydı (en yeni üstte)

| Sürüm | Tarih | Değişiklik |
|---|---|---|
| 0.3 | 2026-11-07 | `API_NEEDS.md` yanıtlandı (bölüm 6). **Kırıcı:** para `amount` artık string; sayfalama alanı `totalItems`; sıralama `sortBy`/`sortDir`; enum değerleri snake_case (`user.role`: `admin` / `member`); tüm zaman damgaları `Z` ile biter (`/system/ping` dahil). Gün 2-4 endpoint şemaları "Planlı (şema kabul edildi)" olarak eklendi. |
| 0.2 | 2026-11-07 | Kimlik doğrulama: `POST /auth/login`, `GET /auth/me`, JWT kuralları. `/health` artık veritabanını da kontrol eder. |
| 0.1 | 2026-11-07 | İlk sürüm: genel kurallar, hata formatı, `/health`, `/system/ping`. |

## 1. Ortam

| Ad | Değer |
|---|---|
| API Base URL | `http://localhost:5000/api/v1` |
| Swagger (yalnızca Development) | `http://localhost:5000/swagger` |
| Health check | `http://localhost:5000/health` (düz metin: `Healthy`, veritabanı erişilemezse `503 Unhealthy`) |
| Frontend origin (CORS) | `http://localhost:5173`, cookie/credentials yok |

Frontend `.env`: `VITE_API_BASE_URL=http://localhost:5000/api/v1`

Production adresleri henüz belirlenmedi; environment variable ile tanımlanacak.

## 2. Genel kurallar

1. **İçerik:** `application/json`, property isimleri `camelCase`.
2. **Header'lar:** İstekte `Accept: application/json`, gövdeli isteklerde `Content-Type: application/json`, korumalı uçlarda `Authorization: Bearer <accessToken>`. AI üretimli metin isteyen uçlarda isteğe bağlı `Accept-Language` (madde 12). Başka zorunlu header yoktur (API sürümü, tenant, correlation id yok; hata gövdesindeki `traceId` yeterlidir).
3. **Enum'lar:** küçük harfli snake_case string (`"status": "failed"`, `"role": "admin"`).
4. **Id'ler:** opak string. Şu an Guid biçimindedir, frontend içeriğine güvenmemeli.
5. **Zaman:** ISO-8601, her zaman UTC ve `Z` ile biter (`2026-11-07T10:30:00Z`, milisaniye olabilir). Gün bucket'ları UTC gününe göredir.
6. **Para:** `{ "amount": "1240.00", "currency": "USD" }`. `amount` ondalık **string**, ondalık ayırıcı `.`. Ödeme tutarlarında 2, AI maliyetlerinde 6 basamağa kadar. Frontend basamak sayısına güvenmez, yalnızca ayrıştırıp biçimlendirir; float ve minor unit yok.
7. **Para birimi kapsamı (MVP kararı):** Tek para birimi USD. Özet, seri ve AI maliyet rakamları USD'dir. USD dışı ödemeler listelerde kendi `currency` değeriyle görünür ama özet rakamlara girmez; sayıları özet yanıtındaki `excludedPaymentCount` alanında bildirilir.
8. **Sayfalama:** `?page=1&pageSize=20` (`page` 1'den başlar, varsayılan `pageSize` 20, en fazla 100). Yanıt: `{ "items": [], "page": 1, "pageSize": 20, "totalItems": 0 }`.
9. **Sıralama ve filtre:** `sortBy`, `sortDir=asc|desc` (varsayılan `createdAt` / `desc`; geçerli `sortBy` değerleri endpoint'te yazılıdır), `status` (tekrarlanabilir: `status=failed&status=pending`), `search`, `from`, `to`. `from` dahil, `to` **hariç**.
10. **Tarih aralığı:** Özet, seri ve dağılım endpoint'lerinde `from` ve `to` **zorunludur**; liste endpoint'lerinde isteğe bağlıdır. `from < to` olmalı ve aralık en fazla 366 gün olabilir; aksi halde `400 validation_failed`.
11. **Hesaplamalar:** Tüm toplam, oran ve tasarruf değerlerini backend hesaplar. Ne LLM ne frontend hesaplar; frontend yalnızca biçimlendirir.
12. **AI metin dili:** AI'ın yazdığı metinler (`rationale`, insight, workspace cevapları) isteğin `Accept-Language` başlığındaki dilde üretilir. Desteklenen diller `en`, `de`, `tr`, `es`; başka değerde veya başlık yoksa `en`. Üretilen metin saklanır ve okurken çevrilmez; kaydın üretildiği dil `rationaleLanguage` alanında döner.
13. **Zaman aşımı:** Normal uçlar birkaç saniyede yanıt verir. AI üretimi yapan uçlar (`POST /revenue/suggestions/generate`, `POST /gateway/completions`) 60 saniyeye kadar sürebilir; frontend bu uçlar için 60 sn timeout kullanmalıdır. MVP'de istek hız limiti yoktur.
14. **Demo verisi:** PayPal'dan gelmeyen kayıtlar `dataSource: "demo"` ile döner (gerçek olanlar `"paypal"`). Arayüz demo kayıtları açıkça belirtmelidir.

## 3. Hata formatı

Tüm hatalar RFC 9457 ProblemDetails (`application/problem+json`) olarak döner:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "code": "validation_failed",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
  "errors": { "email": ["Email is required."] }
}
```

- `code` makine okunur sabit koddur; arayüz metinleri bu koda göre üretilmelidir. `detail` geliştiriciye yöneliktir (İngilizce), kullanıcıya gösterilmez.
- `errors` (`{ alan: string[] }`) yalnızca `validation_failed` hatasında bulunur. Alan adları JSON property isimleriyle aynıdır.

| Status | `code` |
|---|---|
| 400 | `validation_failed`, `bad_request` |
| 401 | `unauthorized` |
| 403 | `forbidden` |
| 404 | `not_found` |
| 405 | `method_not_allowed` |
| 409 | `conflict` veya endpoint'e özel kod (planlı: `suggestion_already_decided`) |
| 422 | İş kuralı ihlali, koda göre özel (planlı: `budget_exceeded`) |
| 500 | `internal_error` |

Planlı özel kodlar (`provider_unavailable` gibi) ilgili endpoint hazır olduğunda buraya eklenir.

## 4. Kimlik doğrulama

- JWT Bearer. Frontend `POST /auth/login` çağırır, dönen `accessToken`'ı korumalı her istekte `Authorization: Bearer <accessToken>` ile gönderir (frontend'deki `setAuthHeaderProvider` kancası buna uygundur).
- Token süresi 60 dakikadır (`expiresAtUtc`). Refresh token yoktur; `401` alınca yeniden login gerekir.
- Korumalı olmayan uçlar: `/health`, `/api/v1/system/ping`, `POST /api/v1/auth/login`, `POST /api/v1/webhooks/paypal` (imza ile doğrulanır). Diğer tüm uçlar token ister.
- Roller: `admin`, `member` (`user.role`). Hackathon için tek demo kullanıcı yeterlidir, ama giriş gerçektir (token şart).

## 5. Endpoint durumları

### Hazır

| Metot | Yol | Auth | Açıklama |
|---|---|---|---|
| GET | `/health` | Yok | Servis ve veritabanı sağlığı (düz metin) |
| GET | `/api/v1/system/ping` | Yok | Bağlantı ve CORS testi |
| POST | `/api/v1/auth/login` | Yok | E-posta ve şifre ile giriş |
| GET | `/api/v1/auth/me` | Bearer | Token sahibi kullanıcı |

**GET `/api/v1/system/ping`** yanıtı:

```json
{
  "name": "PayPal Revenue Intelligence API",
  "version": "1.0.0.0",
  "environment": "Development",
  "serverTimeUtc": "2026-11-07T09:30:00.123Z"
}
```

**POST `/api/v1/auth/login`** isteği `{ "email": "admin@pri.local", "password": "Admin123!" }`. `email` zorunlu, geçerli e-posta, en fazla 254 karakter; `password` zorunlu, en fazla 128 karakter. Hatalar: `400 validation_failed`, `401 unauthorized` (hangisinin yanlış olduğu belirtilmez).

Yanıt `200`:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-11-07T10:30:00Z",
  "user": {
    "id": "3f2b8a52-6d0e-4a0b-9c57-2b8f5b1c7e11",
    "email": "admin@pri.local",
    "displayName": "Demo Admin",
    "role": "admin"
  }
}
```

**GET `/api/v1/auth/me`** yanıtı `200`: yukarıdaki `user` nesnesi.

Yerel geliştirme demo kullanıcısı: `admin@pri.local` / `Admin123!`.

### Planlı (şema kabul edildi, henüz uygulanmadı)

Aşağıdaki şemalar `API_NEEDS.md` önerilerine dayanır; farklar yanıtlarda belirtilmiştir. Hedef günler tahminidir. Hazır olana kadar frontend mock adapter kullanır. Gösterimde `Money = { amount: string; currency: string }` ve `Range = { from: string; to: string }`.

**Gün 2: `/system/ping` eki.** Yanıta `paypalEnvironment: "sandbox" | "live"` eklenir (arayüzde "Sandbox" etiketi için).

**Gün 2: Payments**

```ts
// GET /api/v1/revenue/payments
// sorgu: page, pageSize, status (tekrarlanabilir), search (müşteri adı/e-posta/ödeme id),
//        customerId, from, to (createdAt'e göre), sortBy = createdAt | amount, sortDir
// yanıt: { items: Payment[]; page; pageSize; totalItems }
type Payment = {
  id: string;
  customerId: string;
  customerName: string;
  amount: Money;
  status: "completed" | "pending" | "failed" | "refunded";
  failureCode: string | null;        // yalnızca failed için dolu
  paypalTransactionId: string | null;
  dataSource: "paypal" | "demo";
  createdAt: string;
  updatedAt: string;
};

// GET /api/v1/revenue/payments/{id}  -> Payment & { statusHistory: { status: string; occurredAt: string }[] }
```

`failureCode` değerleri Gün 2'de PayPal Sandbox verisine göre kesinleşir. Başlangıç adayları: `card_declined`, `insufficient_funds`, `expired_card`, `account_restricted`, `other`. Frontend bilinmeyen bir değeri genel bir metinle göstermelidir. `pending` ve `refunded` PayPal durumlarından türetilir; eşleme kesinleşince bu dosyaya yazılır.

**Gün 2: Overview özeti**

```ts
// GET /api/v1/revenue/summary?from&to        (from/to zorunlu, to hariç)
type RevenueSummary = {
  range: Range;
  totalRevenue: Money;        // aralıktaki completed ödemelerin toplamı
  failedAmount: Money;
  pendingAmount: Money;
  recoveredRevenue: Money;    // Recovery'deki recoveredAmount ile aynı tanım (aşağıda)
  paymentCount: number;
  failedCount: number;
  pendingCount: number;
  excludedPaymentCount: number;   // USD dışı olduğu için özete girmeyen ödemeler
  series: { date: string; revenue: Money; recovered: Money }[];   // aralıktaki her UTC günü için bir kayıt, boş günler sıfır
};
```

Kurtarılmış gelir henüz kaydedilmediyse `recoveredRevenue` gerçek değer olan `"0.00"` döner.

**Gün 3: AI Cost** (JWT'li panel uçları)

```ts
// GET /api/v1/gateway/usage/summary?from&to
type UsageSummary = {
  range: Range;
  totalCost: Money;
  totalRequests: number;      // aralıkta tamamlanan istekler (sağlayıcıdan veya cache'ten yanıtlananlar)
  inputTokens: number;        // yalnızca sağlayıcıya giden istekler; cache hit'ler sayılmaz
  outputTokens: number;
  cacheHitRate: number;       // 0..1, cache hit sayısı / totalRequests
  savings: Money;             // aşağıdaki tanım
  budget: { limit: Money; spent: Money; period: "monthly" } | null;   // spent: içinde bulunulan UTC takvim ayı, aralıktan bağımsız
};

// GET /api/v1/gateway/usage/series?from&to
//   -> { range: Range; items: { date: string; cost: Money; requests: number }[] }

// GET /api/v1/gateway/usage/breakdown?from&to
//   -> { range: Range; byModel: BreakdownItem[]; byProvider: BreakdownItem[] }
type BreakdownItem = {
  key: string; label: string; cost: Money;
  requests: number; inputTokens: number; outputTokens: number;
};
```

`savings` tanımı: yalnızca gateway'in ölçtüğü kalemler toplanır. Her istek için `baselineCost - actualCost` hesaplanır. `baselineCost`, isteğin özgün (optimize edilmemiş) prompt token'ları ve gerçek çıktı token'larıyla baz modelin resmi fiyatı üzerinden bulunur; `actualCost` gerçekte ödenen tutardır (cache hit'te 0). Baz model ayardan gelir ve bu uçlarda yanıtla birlikte bildirilir. Ölçülmemiş hiçbir tasarruf gösterilmez. Başlangıçta yalnızca `openai` sağlayıcısı vardır; frontend sağlayıcı listesini sabit varsaymamalıdır.

**Gün 4: Recovery**

```ts
// GET /api/v1/revenue/recovery/summary?from&to
type RecoverySummary = {
  range: Range;
  lostAmount: Money;        // aralıkta tespit edilen gelir kaybı kayıtlarının toplamı
  recoveredAmount: Money;   // aynı kayıtlardan sonradan tahsil edilenler
  recoveryRate: number;     // 0..1 = recoveredAmount / lostAmount (lostAmount 0 ise 0)
  openSuggestionCount: number;
};

// GET /api/v1/revenue/suggestions
// sorgu: page, pageSize, status (tekrarlanabilir), paymentId, sortBy = createdAt | expectedRecovery, sortDir
type Suggestion = {
  id: string;
  paymentId: string;
  customerName: string;
  type: "send_reminder" | "retry_payment" | "offer_alternative_method";
  rationale: string;
  rationaleLanguage: "en" | "de" | "tr" | "es";
  expectedRecovery: Money;
  status: "proposed" | "approved" | "rejected" | "executed" | "failed";
  executable: boolean;      // false ise yalnızca tavsiyedir; onaylansa bile otomatik aksiyon çalışmaz
  dataSource: "paypal" | "demo";
  createdAt: string;
  updatedAt: string;
};

// POST /api/v1/revenue/suggestions/{id}/decision   { "decision": "approve" | "reject" }  -> 200 Suggestion
// POST /api/v1/revenue/suggestions/generate        gövdesiz -> 200 { "createdCount": number }
```

- Öneriler kullanıcı tetiklemesiyle üretilir (`generate`, `Accept-Language` dikkate alınır); arka planda otomatik üretim yoktur (MVP kararı).
- Kullanıcı onayı olmadan hiçbir dış aksiyon çalışmaz. `decision` yanıtı güncel öneriyi hemen döner (`approved` veya `rejected`). `executable: true` olan bir öneri onaylanınca backend aksiyonu arka planda çalıştırır ve durum birkaç saniye içinde `executed` veya `failed` olur; frontend öneriyi yeniden sorgular (2-3 sn aralıkla, en fazla 30 sn). `executable: false` öneriler `approved` olarak kalır.
- MVP'de yalnızca PayPal faturasına bağlı kayıtlar için `send_reminder` çalıştırılabilirdir (PayPal Sandbox hatırlatması); `retry_payment` ve `offer_alternative_method` tavsiyedir.
- Karara bağlanmış öneriye tekrar karar verilirse `409` ve `code: "suggestion_already_decided"` döner.

**Gün 4: Ayarlar**

```
GET /api/v1/gateway/budgets · PUT /api/v1/gateway/budgets      aylık bütçe limiti (tek kayıt, MVP)
GET /api/v1/gateway/routing-rules · PUT /api/v1/gateway/routing-rules
GET /api/v1/gateway/models                                      model kataloğu ve sağlayıcılar (salt okunur)
GET /api/v1/gateway/cache/stats · DELETE /api/v1/gateway/cache
```

Şemaları ilgili gün hazırlanırken eklenecek. Backend'in saklayamadığı hiçbir ayar arayüzde gösterilmemelidir.

**AI Workspace (Gün 3 sonrası, sözleşme 0.4'te netleşecek).** Yön: sunucu tarafında konuşma saklanmaz (MVP); frontend mesaj geçmişini her istekte gönderir. Yanıt POST üzerinden SSE (`text/event-stream`) olarak akar; olay türleri `meta`, `token`, `suggestion` (AI'ın önerdiği aksiyon, yukarıdaki `Suggestion` şekliyle), `usage` (model, sağlayıcı, token, maliyet, cache hit), `done`, `error`. Kesin şema 0.4'e kadar kullanılmamalıdır.

**Diğer planlı uçlar:**

```
Gün 2  GET /revenue/invoices · GET /revenue/customers
       POST /revenue/sync · GET /revenue/sync/status
       POST /webhooks/paypal
Gün 3  POST /gateway/completions
```

**Operasyon eşlemesi (frontend adapter):**

| Adapter metodu | Endpoint | Hedef |
|---|---|---|
| `revenue.getSummary(range)` | `GET /revenue/summary` | Gün 2 |
| `revenue.listPayments(query)` | `GET /revenue/payments` | Gün 2 |
| `revenue.getPayment(id)` | `GET /revenue/payments/{id}` | Gün 2 |
| `aiCost.getUsageSummary(range)` | `GET /gateway/usage/summary` | Gün 3 |
| `aiCost.getUsageSeries(range)` | `GET /gateway/usage/series` | Gün 3 |
| `aiCost.getBreakdown(range)` | `GET /gateway/usage/breakdown` | Gün 3 |
| `revenue.getRecoverySummary(range)` | `GET /revenue/recovery/summary` | Gün 4 |
| `revenue.listSuggestions(query)` | `GET /revenue/suggestions` | Gün 4 |
| `revenue.decideSuggestion(id, decision)` | `POST /revenue/suggestions/{id}/decision` | Gün 4 |
| Settings okuma/yazma | `/gateway/budgets`, `/gateway/routing-rules` | Gün 4 |
| AI Workspace sohbeti | 0.4'te netleşecek | Gün 4+ |

## 6. `API_NEEDS.md` yanıtları

| # | Konu | Karar |
|---|---|---|
| 1 | Auth | JWT Bearer, `POST /auth/login`, refresh yok (bölüm 4). Tek demo kullanıcı var ama token şart. |
| 2 | Header'lar | Bölüm 2, madde 2. Zorunlu ek header yok. |
| 3 | CORS | `http://localhost:5173` izinli, credentials yok. |
| 4 | Hata formatı | Önerildiği gibi kabul: ProblemDetails + `code` + `errors` (bölüm 3). |
| 5 | Para | String `amount` + ISO 4217 `currency` (madde 6). MVP'de çoklu para birimi kapsam dışıdır, yalnızca USD (madde 7). |
| 6 | Tarih | ISO-8601 UTC, `Z` ile (madde 5). |
| 7 | Id | Opak string (madde 4). |
| 8 | Sayfalama | `page` + `pageSize`, yanıt `totalItems` (madde 8). Varsayılan 20, en fazla 100. |
| 9 | Sıralama/filtre | `sortBy`, `sortDir`, `status` (tekrarlanabilir), `search`, `from`, `to` hariç (madde 9). |
| 10 | Tarih aralıkları | Özet, seri ve dağılım uçlarında zorunlu, listelerde isteğe bağlı (madde 10). |
| 11 | Enum'lar | Bölüm 5'te. `failureCode` listesi Gün 2'de kesinleşir; frontend bilinmeyen değere dayanıklı olmalı. |
| 12 | Ortam göstergesi | `/system/ping` yanıtına `paypalEnvironment` eklenecek (Gün 2). |
| 13 | AI metin dili | `Accept-Language` (`en`, `de`, `tr`, `es`) dikkate alınır, yedek `en` (madde 12). Metin üretildiği dilde saklanır. |
| 14 | Hesaplamalar | Evet, tamamı backend'de (madde 11). |
| 15 | Limitler/timeout | Hız limiti yok. AI üretimi uçları için 60 sn timeout kullanın (madde 13). |
