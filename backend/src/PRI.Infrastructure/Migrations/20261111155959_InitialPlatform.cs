using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "revenue");

            migrationBuilder.EnsureSchema(
                name: "gateway");

            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "ai_insights",
                schema: "revenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetricsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Commentary = table.Column<string>(type: "text", nullable: false),
                    CommentaryLanguage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CommentarySource = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    GatewayRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_insights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "budgets",
                schema: "gateway",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlyLimitUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    WarnAtPercent = table.Column<int>(type: "integer", nullable: false),
                    EnforceLimit = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                schema: "gateway",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsFirstParty = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ApiKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ApiKeyPrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "revenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSource = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_models",
                schema: "gateway",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ModelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tier = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    InputPricePerMillionUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CachedInputPricePerMillionUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    OutputPricePerMillionUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsBaseline = table.Column<bool>(type: "boolean", nullable: false),
                    PricingSourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PricingCheckedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_models", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sync_runs",
                schema: "revenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RangeFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RangeToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImportedPaymentCount = table.Column<int>(type: "integer", nullable: false),
                    ImportedInvoiceCount = table.Column<int>(type: "integer", nullable: false),
                    SkippedCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_events",
                schema: "revenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cache_entries",
                schema: "gateway",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QueryHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NumbersFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Embedding = table.Column<float[]>(type: "real[]", nullable: true),
                    ResponseContent = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ModelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FinishReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OriginalCostUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    HitCount = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHitAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cache_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cache_entries_clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "gateway",
                        principalTable: "clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cache_lookups",
                schema: "gateway",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Similarity = table.Column<double>(type: "double precision", nullable: true),
                    CacheEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmbeddingTokens = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingCostUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    SavedCostUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cache_lookups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cache_lookups_clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "gateway",
                        principalTable: "clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "revenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSource = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ProviderStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoices_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "revenue",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "routing_rules",
                schema: "gateway",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrimaryModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    FallbackModelIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_routing_rules_provider_models_PrimaryModelId",
                        column: x => x.PrimaryModelId,
                        principalSchema: "gateway",
                        principalTable: "provider_models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usage_records",
                schema: "gateway",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ModelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TaskType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsAutoRouted = table.Column<bool>(type: "boolean", nullable: false),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    CachedInputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    CostUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    BaselineModelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BaselineCostUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    InputPricePerMillionUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CachedInputPricePerMillionUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    OutputPricePerMillionUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    LatencyMs = table.Column<int>(type: "integer", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderRequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PromptCharsSaved = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usage_records_clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "gateway",
                        principalTable: "clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usage_records_provider_models_ProviderModelId",
                        column: x => x.ProviderModelId,
                        principalSchema: "gateway",
                        principalTable: "provider_models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "revenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataSource = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InitiatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "revenue",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "revenue",
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "issues",
                schema: "revenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DataSource = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ProblemSinceUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecoveredAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Evidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_issues_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "revenue",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "revenue",
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "revenue",
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_events",
                schema: "revenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_events_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "revenue",
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recovery_actions",
                schema: "revenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RationaleLanguage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    RationaleSource = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    GatewayRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedRecovery = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Executable = table.Column<bool>(type: "boolean", nullable: false),
                    DataSource = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutionError = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recovery_actions_issues_IssueId",
                        column: x => x.IssueId,
                        principalSchema: "revenue",
                        principalTable: "issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_insights_CreatedAtUtc",
                schema: "revenue",
                table: "ai_insights",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_cache_entries_ClientId_ContextHash_ExpiresAtUtc",
                schema: "gateway",
                table: "cache_entries",
                columns: new[] { "ClientId", "ContextHash", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cache_entries_ClientId_ContextHash_QueryHash",
                schema: "gateway",
                table: "cache_entries",
                columns: new[] { "ClientId", "ContextHash", "QueryHash" });

            migrationBuilder.CreateIndex(
                name: "IX_cache_lookups_ClientId_RequestedAtUtc",
                schema: "gateway",
                table: "cache_lookups",
                columns: new[] { "ClientId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cache_lookups_RequestedAtUtc",
                schema: "gateway",
                table: "cache_lookups",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_clients_ApiKeyHash",
                schema: "gateway",
                table: "clients",
                column: "ApiKeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clients_Name",
                schema: "gateway",
                table: "clients",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_DataSource_ExternalId",
                schema: "revenue",
                table: "customers",
                columns: new[] { "DataSource", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_Email",
                schema: "revenue",
                table: "customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_CustomerId",
                schema: "revenue",
                table: "invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_DataSource_ExternalId",
                schema: "revenue",
                table: "invoices",
                columns: new[] { "DataSource", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_InvoiceNumber",
                schema: "revenue",
                table: "invoices",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_Status_DueDate",
                schema: "revenue",
                table: "invoices",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_issues_CustomerId",
                schema: "revenue",
                table: "issues",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_issues_InvoiceId",
                schema: "revenue",
                table: "issues",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_issues_PaymentId",
                schema: "revenue",
                table: "issues",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_issues_ResolvedAtUtc",
                schema: "revenue",
                table: "issues",
                column: "ResolvedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_issues_Status_ProblemSinceUtc",
                schema: "revenue",
                table: "issues",
                columns: new[] { "Status", "ProblemSinceUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_issues_Type_InvoiceId",
                schema: "revenue",
                table: "issues",
                columns: new[] { "Type", "InvoiceId" },
                unique: true,
                filter: "\"PaymentId\" IS NULL AND \"InvoiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_issues_Type_PaymentId",
                schema: "revenue",
                table: "issues",
                columns: new[] { "Type", "PaymentId" },
                unique: true,
                filter: "\"PaymentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_events_PaymentId_OccurredAtUtc",
                schema: "revenue",
                table: "payment_events",
                columns: new[] { "PaymentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_CustomerId",
                schema: "revenue",
                table: "payments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_DataSource_ExternalId",
                schema: "revenue",
                table: "payments",
                columns: new[] { "DataSource", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_InvoiceId",
                schema: "revenue",
                table: "payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Status_InitiatedAtUtc",
                schema: "revenue",
                table: "payments",
                columns: new[] { "Status", "InitiatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_models_Provider_ModelId",
                schema: "gateway",
                table: "provider_models",
                columns: new[] { "Provider", "ModelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recovery_actions_IssueId",
                schema: "revenue",
                table: "recovery_actions",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_actions_PaymentId",
                schema: "revenue",
                table: "recovery_actions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_actions_Status_CreatedAtUtc",
                schema: "revenue",
                table: "recovery_actions",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_routing_rules_PrimaryModelId",
                schema: "gateway",
                table: "routing_rules",
                column: "PrimaryModelId");

            migrationBuilder.CreateIndex(
                name: "IX_routing_rules_TaskType",
                schema: "gateway",
                table: "routing_rules",
                column: "TaskType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_runs_StartedAtUtc",
                schema: "revenue",
                table: "sync_runs",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_usage_records_ClientId_RequestedAtUtc",
                schema: "gateway",
                table: "usage_records",
                columns: new[] { "ClientId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_usage_records_ProviderModelId_RequestedAtUtc",
                schema: "gateway",
                table: "usage_records",
                columns: new[] { "ProviderModelId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_usage_records_RequestedAtUtc",
                schema: "gateway",
                table: "usage_records",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "platform",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_webhook_events_ExternalEventId",
                schema: "revenue",
                table: "webhook_events",
                column: "ExternalEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_insights",
                schema: "revenue");

            migrationBuilder.DropTable(
                name: "budgets",
                schema: "gateway");

            migrationBuilder.DropTable(
                name: "cache_entries",
                schema: "gateway");

            migrationBuilder.DropTable(
                name: "cache_lookups",
                schema: "gateway");

            migrationBuilder.DropTable(
                name: "payment_events",
                schema: "revenue");

            migrationBuilder.DropTable(
                name: "recovery_actions",
                schema: "revenue");

            migrationBuilder.DropTable(
                name: "routing_rules",
                schema: "gateway");

            migrationBuilder.DropTable(
                name: "sync_runs",
                schema: "revenue");

            migrationBuilder.DropTable(
                name: "usage_records",
                schema: "gateway");

            migrationBuilder.DropTable(
                name: "users",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "webhook_events",
                schema: "revenue");

            migrationBuilder.DropTable(
                name: "issues",
                schema: "revenue");

            migrationBuilder.DropTable(
                name: "clients",
                schema: "gateway");

            migrationBuilder.DropTable(
                name: "provider_models",
                schema: "gateway");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "revenue");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "revenue");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "revenue");
        }
    }
}
