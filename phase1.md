# Phase 1 實作規格書

---

## 一、Phase 1 完成定義（Definition of Done）

Phase 1 目標是完成 **Webhook 接收層骨架對齊**，使 Controller → Application 的資料流符合 `Implement_Instruction_V2.md` 的架構規劃。

Phase 1 結束後，系統必須達到以下可驗證狀態：

| # | 驗收條件 |
|---|---------|
| 1 | `WebhookController` 有三個獨立 Action：`POST /api/webhook/github`、`POST /api/webhook/gitlab`、`POST /api/webhook/bitbucket`，各自接受強型別 Request Entity（`[FromBody]`），**不再**依賴 `IWebhookParserService` |
| 2 | 各 Action 內部自行判斷「是否需要處理此事件」，符合條件才往下走，不符合回傳 `200 OK + skipped` |
| 3 | Webhook Signature 驗證邏輯仍留在 Controller 層，不移入 Service；Secret 從 secrets config 讀取，不存 DB |
| 4 | Controller 組裝出 `StartCodeReviewCommand`（含 `PullRequestRef` 與各平台的 `DiffUrl`），呼叫 `ICodeReviewService.StartAsync(command)` |
| 5 | `StartCodeReviewCommand` 成為 Service 唯一入口；舊的 `ProcessPullRequestAsync(WebhookPayload)` 改由 `StartAsync` 橋接呼叫（Phase 2 再徹底移除） |
| 6 | `PullRequestIgnoreOptions` 從 `appsettings.json` 讀取 title ignore keywords；`CodeReviewService.StartAsync` 命中 keyword 即 skip，**只寫 ILogger，不寫 DB** |

> **Phase 1 不涉及**：DiffUrl 實際 HTTP 取得、raw content 抓取、diff hunk 解析、Polly retry、`IRepoHostClient` 重構。這些屬於 Phase 2。

---

## 二、異動檔案清單

```
NineYi.Ai.CodeReview.Api/
├── Models/
│   └── Webhooks/                            ← 新增目錄
│       ├── GitHubWebhookRequest.cs           ← 新增
│       ├── GitLabWebhookRequest.cs           ← 新增
│       └── BitbucketWebhookRequest.cs        ← 新增
└── Controllers/
    └── WebhookController.cs                  ← 改版（移除 Parser 依賴）

NineYi.Ai.CodeReview.Application/
├── Commands/                                 ← 新增目錄
│   ├── StartCodeReviewCommand.cs             ← 新增
│   └── PullRequestRef.cs                    ← 新增
├── Options/                                  ← 新增目錄
│   ├── PullRequestIgnoreOptions.cs           ← 新增
│   └── WebhookSecretsOptions.cs             ← 新增（Webhook Secret 強型別設定）
└── Services/
    ├── ICodeReviewService.cs                 ← 新增 StartAsync 方法簽章
    └── CodeReviewService.cs                  ← 新增 StartAsync + ignore gate

NineYi.Ai.CodeReview.Domain/
└── Entities/
    └── ReviewLog.cs                          ← 新增 ReviewStatus.Skipped enum 值

NineYi.Ai.CodeReview.Api/
└── appsettings.json                          ← 新增 PullRequestIgnore 與 _N1SECRETS 區段結構
```

---

## 三、Step 1 — 各平台 Webhook Request Entity

放置位置：`NineYi.Ai.CodeReview.Api/Models/Webhooks/`

設計原則：
- 只對應 Code Review 流程**真正需要的欄位**，其餘 payload 欄位不對應
- 使用 `[JsonPropertyName]` 對應各平台的 snake_case / 特殊命名
- 子物件拆成獨立 class，命名以 `{Platform}*Payload` 後綴區分

---

### 1-A. `GitHubWebhookRequest.cs`

**關鍵欄位來源對照（based on GitHub_PR_Payload.sample.md）：**

| 欄位 | JSON 路徑 | 用途 |
|------|-----------|------|
| `Action` | `$.action` | ShouldProcess 判斷：opened / synchronize / reopened |
| `Number` | `$.number` | PR 編號（root level） |
| `PullRequest.Title` | `$.pull_request.title` | PR Title Ignore Gate |
| `PullRequest.Draft` | `$.pull_request.draft` | Draft PR 不處理 |
| `PullRequest.DiffUrl` | `$.pull_request.diff_url` | ⭐ DiffUrl → `PullRequestRef.DiffUrl` |
| `PullRequest.Head.Sha` | `$.pull_request.head.sha` | ⭐ HeadCommitSha |
| `PullRequest.Head.Ref` | `$.pull_request.head.ref` | Source branch |
| `PullRequest.Base.Ref` | `$.pull_request.base.ref` | Target branch |
| `PullRequest.User.Login` | `$.pull_request.user.login` | PR 作者 |
| `Repository.Id` | `$.repository.id` | Repo 識別 |
| `Repository.Name` | `$.repository.name` | Repo 短名 |
| `Repository.FullName` | `$.repository.full_name` | ⭐ RepoFullName |
| `Sender.Login` | `$.sender.login` | 觸發者 |

**ShouldProcess 條件（Controller 內判斷）：**
- Header `X-GitHub-Event` == `"pull_request"`
- `action` in `["opened", "synchronize", "reopened"]`

```csharp
// Models/Webhooks/GitHubWebhookRequest.cs
namespace NineYi.Ai.CodeReview.Api.Models.Webhooks;

public class GitHubWebhookRequest
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("pull_request")]
    public GitHubPrPayload PullRequest { get; set; } = new();

    [JsonPropertyName("repository")]
    public GitHubRepoPayload Repository { get; set; } = new();

    [JsonPropertyName("sender")]
    public GitHubUserPayload Sender { get; set; } = new();
}

public class GitHubPrPayload
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("diff_url")]
    public string DiffUrl { get; set; } = string.Empty;

    [JsonPropertyName("head")]
    public GitHubBranchPayload Head { get; set; } = new();

    [JsonPropertyName("base")]
    public GitHubBranchPayload Base { get; set; } = new();

    [JsonPropertyName("user")]
    public GitHubUserPayload User { get; set; } = new();
}

public class GitHubBranchPayload
{
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;

    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
}

public class GitHubRepoPayload
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
}

public class GitHubUserPayload
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
}
```

**Controller 組裝 `StartCodeReviewCommand`：**
```
ProviderType      = GitPlatformType.GitHub
RepoFullName      ← request.Repository.FullName
PullRequestNumber ← request.Number
Title             ← request.PullRequest.Title
PullRequestRef
  .HeadCommitSha  ← request.PullRequest.Head.Sha
  .SourceBranch   ← request.PullRequest.Head.Ref
  .TargetBranch   ← request.PullRequest.Base.Ref
  .DiffUrl        ← request.PullRequest.DiffUrl
```

---

### 1-B. `GitLabWebhookRequest.cs`

**關鍵欄位來源對照（based on GitLab_PR_Payload.sample.md notes + 官方文件）：**

> GitLab Merge Request Hook 官方文件：https://docs.gitlab.com/ee/user/project/integrations/webhook_events.html#merge-request-events

| 欄位 | JSON 路徑 | 用途 |
|------|-----------|------|
| `ObjectKind` | `$.object_kind` | 必須為 `"merge_request"` |
| `User.Username` | `$.user.username` | 觸發者 |
| `Project.Id` | `$.project.id` | ⭐ Project ID（notes 確認） |
| `Project.Name` | `$.project.name` | ⭐ Repo 短名（notes 確認） |
| `Project.PathWithNamespace` | `$.project.path_with_namespace` | ⭐ RepoFullName（"group/repo"） |
| `ObjectAttributes.Iid` | `$.object_attributes.iid` | ⭐ MR 編號（**iid** 非 id，notes 確認） |
| `ObjectAttributes.Title` | `$.object_attributes.title` | PR Title Ignore Gate |
| `ObjectAttributes.State` | `$.object_attributes.state` | opened / closed / merged |
| `ObjectAttributes.Action` | `$.object_attributes.action` | ⭐ open / update / reopen（notes 確認） |
| `ObjectAttributes.SourceBranch` | `$.object_attributes.source_branch` | Source branch |
| `ObjectAttributes.TargetBranch` | `$.object_attributes.target_branch` | ⭐ Target branch（notes 確認） |
| `ObjectAttributes.LastCommit.Id` | `$.object_attributes.last_commit.id` | ⭐ HeadCommitSha（notes 確認） |
| `ObjectAttributes.Draft` | `$.object_attributes.draft` | Draft MR 不處理（GitLab 15.0+） |
| `ObjectAttributes.Url` | `$.object_attributes.url` | MR 連結 |

> ⚠️ GitLab webhook payload **不包含 diff_url**，`PullRequestRef.DiffUrl = null`。  
> Phase 2 由 `GitLabClient` 以 `project.id` + `iid` 呼叫 GitLab API 取得 diff。

**ShouldProcess 條件（Controller 內判斷）：**
- Header `X-Gitlab-Event` == `"Merge Request Hook"`
- `object_kind` == `"merge_request"`
- `object_attributes.action` in `["open", "update", "reopen"]`

```csharp
// Models/Webhooks/GitLabWebhookRequest.cs
namespace NineYi.Ai.CodeReview.Api.Models.Webhooks;

public class GitLabWebhookRequest
{
    [JsonPropertyName("object_kind")]
    public string ObjectKind { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public GitLabUserPayload User { get; set; } = new();

    [JsonPropertyName("project")]
    public GitLabProjectPayload Project { get; set; } = new();

    [JsonPropertyName("object_attributes")]
    public GitLabObjectAttributesPayload ObjectAttributes { get; set; } = new();
}

public class GitLabUserPayload
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}

public class GitLabProjectPayload
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path_with_namespace")]
    public string PathWithNamespace { get; set; } = string.Empty;

    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; } = string.Empty;

    [JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; } = string.Empty;
}

public class GitLabObjectAttributesPayload
{
    [JsonPropertyName("iid")]
    public int Iid { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("source_branch")]
    public string SourceBranch { get; set; } = string.Empty;

    [JsonPropertyName("target_branch")]
    public string TargetBranch { get; set; } = string.Empty;

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("last_commit")]
    public GitLabLastCommitPayload LastCommit { get; set; } = new();
}

public class GitLabLastCommitPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
```

**Controller 組裝 `StartCodeReviewCommand`：**
```
ProviderType      = GitPlatformType.GitLab
RepoFullName      ← request.Project.PathWithNamespace
PullRequestNumber ← request.ObjectAttributes.Iid
Title             ← request.ObjectAttributes.Title
PullRequestRef
  .HeadCommitSha  ← request.ObjectAttributes.LastCommit.Id
  .SourceBranch   ← request.ObjectAttributes.SourceBranch
  .TargetBranch   ← request.ObjectAttributes.TargetBranch
  .DiffUrl        ← null（GitLab 不提供，Phase 2 補）
```

---

### 1-C. `BitbucketWebhookRequest.cs`

**關鍵欄位來源對照（based on Bitbucket_PR_Payload.sample.md）：**

| 欄位 | JSON 路徑 | 用途 |
|------|-----------|------|
| `Repository.FullName` | `$.repository.full_name` | ⭐ RepoFullName（"workspace/slug"） |
| `Repository.Name` | `$.repository.name` | Repo 短名 |
| `Repository.Uuid` | `$.repository.uuid` | Repo UUID |
| `Actor.AccountId` | `$.actor.account_id` | 觸發者 ID |
| `Actor.Nickname` | `$.actor.nickname` | 觸發者名稱 |
| `Pullrequest.Id` | `$.pullrequest.id` | ⭐ PR 編號 |
| `Pullrequest.Title` | `$.pullrequest.title` | ⭐ PR Title Ignore Gate |
| `Pullrequest.State` | `$.pullrequest.state` | OPEN / MERGED / DECLINED |
| `Pullrequest.Draft` | `$.pullrequest.draft` | Draft PR 不處理 |
| `Pullrequest.Source.Branch.Name` | `$.pullrequest.source.branch.name` | Source branch |
| `Pullrequest.Source.Commit.Hash` | `$.pullrequest.source.commit.hash` | ⭐ HeadCommitSha |
| `Pullrequest.Destination.Branch.Name` | `$.pullrequest.destination.branch.name` | ⭐ Target branch |
| `Pullrequest.Author.AccountId` | `$.pullrequest.author.account_id` | PR 作者 |
| `Pullrequest.Links.Diff.Href` | `$.pullrequest.links.diff.href` | ⭐ DiffUrl → `PullRequestRef.DiffUrl` |
| `Pullrequest.Links.Diffstat.Href` | `$.pullrequest.links.diffstat.href` | Diffstat URL（備用） |

**ShouldProcess 條件（Controller 內判斷）：**
- Header `X-Event-Key` in `["pullrequest:created", "pullrequest:updated"]`
- `pullrequest.state` == `"OPEN"`

```csharp
// Models/Webhooks/BitbucketWebhookRequest.cs
namespace NineYi.Ai.CodeReview.Api.Models.Webhooks;

public class BitbucketWebhookRequest
{
    [JsonPropertyName("repository")]
    public BitbucketRepoPayload Repository { get; set; } = new();

    [JsonPropertyName("actor")]
    public BitbucketActorPayload Actor { get; set; } = new();

    [JsonPropertyName("pullrequest")]
    public BitbucketPrPayload Pullrequest { get; set; } = new();
}

public class BitbucketRepoPayload
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;
}

public class BitbucketActorPayload
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}

public class BitbucketPrPayload
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("source")]
    public BitbucketBranchRefPayload Source { get; set; } = new();

    [JsonPropertyName("destination")]
    public BitbucketBranchRefPayload Destination { get; set; } = new();

    [JsonPropertyName("author")]
    public BitbucketActorPayload Author { get; set; } = new();

    [JsonPropertyName("links")]
    public BitbucketPrLinksPayload Links { get; set; } = new();
}

public class BitbucketBranchRefPayload
{
    [JsonPropertyName("branch")]
    public BitbucketBranchPayload Branch { get; set; } = new();

    [JsonPropertyName("commit")]
    public BitbucketCommitPayload Commit { get; set; } = new();
}

public class BitbucketBranchPayload
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class BitbucketCommitPayload
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
}

public class BitbucketPrLinksPayload
{
    [JsonPropertyName("diff")]
    public BitbucketHrefPayload Diff { get; set; } = new();

    [JsonPropertyName("diffstat")]
    public BitbucketHrefPayload Diffstat { get; set; } = new();
}

public class BitbucketHrefPayload
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;
}
```

**Controller 組裝 `StartCodeReviewCommand`：**
```
ProviderType      = GitPlatformType.Bitbucket
RepoFullName      ← request.Repository.FullName
PullRequestNumber ← request.Pullrequest.Id
Title             ← request.Pullrequest.Title
PullRequestRef
  .HeadCommitSha  ← request.Pullrequest.Source.Commit.Hash
  .SourceBranch   ← request.Pullrequest.Source.Branch.Name
  .TargetBranch   ← request.Pullrequest.Destination.Branch.Name
  .DiffUrl        ← request.Pullrequest.Links.Diff.Href
```

---

## 四、Step 2 — StartCodeReviewCommand + PullRequestRef

放置位置：`NineYi.Ai.CodeReview.Application/Commands/`

```csharp
// Commands/StartCodeReviewCommand.cs
public class StartCodeReviewCommand
{
    public GitPlatformType ProviderType { get; set; }   // 沿用現有 Domain 層的 GitPlatformType
    public string RepoFullName { get; set; } = string.Empty;
    public int PullRequestNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public PullRequestRef PullRequestRef { get; set; } = new();
}

// Commands/PullRequestRef.cs
public class PullRequestRef
{
    public string HeadCommitSha { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string? DiffUrl { get; set; }   // GitHub/Bitbucket 有值；GitLab 為 null
}
```

> `GitPlatformType` 沿用現有 Domain 層定義，不新增 `ProviderType` enum。

---

## 五、Step 3 — WebhookController 改版

改版重點：
- 移除 `IEnumerable<IWebhookParserService> _parsers` 依賴
- 移除 `IGitPlatformServiceFactory` 依賴（此層不再需要）
- 三個 Action 各自 `[FromBody]` 強型別 Request Entity
- ShouldProcess 邏輯 inline 在各 Action 內（3～4 行條件，不需要抽介面）
- Signature 驗證保留（GitHub HMAC-SHA256、GitLab Token 比對、Bitbucket 無簽名）
- **直接 `await` 呼叫 `ICodeReviewService.StartAsync(command)`**，不使用 fire-and-forget

> **關於 async/await vs fire-and-forget 的決策：**
> 
> 雖然各平台 Webhook 有 10 秒回應時限，但 Code Review 可能超過此時間。
> Phase 1 先採用直接 `await` 的方式，優點是：
> - 錯誤可直接在 response 中反映
> - 不會有背景 Task 被 App restart 中斷而導致無紀錄的問題
> 
> 如果平台標記 delivery failed 造成困擾，Phase 3 再升級為 Background Service + Queue。

```csharp
// 改版後 WebhookController — GitHub Action 骨架
[HttpPost("github")]
public async Task<IActionResult> GitHub(
    [FromBody] GitHubWebhookRequest request,
    CancellationToken cancellationToken)
{
    var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();

    // ① ShouldProcess — 只處理 PR 事件的特定 action，其餘回 200 skipped
    //    回傳 200（而非 4xx），避免平台誤判 delivery failed 並觸發重試
    if (eventType != "pull_request")
        return Ok(new { message = "Event skipped", reason = "Not a pull_request event" });

    var validActions = new[] { "opened", "synchronize", "reopened" };
    if (!validActions.Contains(request.Action))
        return Ok(new { message = "Event skipped", reason = $"Action '{request.Action}' not handled" });

    if (request.PullRequest.Draft)
        return Ok(new { message = "Event skipped", reason = "Draft PR" });

    // ② Signature 驗證 — 確認 request 來自 GitHub，防偽造
    var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
    if (!string.IsNullOrEmpty(signature))
    {
        var platformSettings = await _platformSettingsRepository
            .GetByPlatformAsync(GitPlatformType.GitHub, cancellationToken);

        if (platformSettings != null && !string.IsNullOrEmpty(platformSettings.WebhookSecret))
        {
            // 驗證失敗 → 401，不往下執行
            // 注意：需要 raw body string 來驗簽，需在 ReadBody 之前保存
            // （現有 ValidateWebhookSignature 邏輯保留不動）
        }
    }

    // ③ 組裝 StartCodeReviewCommand
    var command = new StartCodeReviewCommand
    {
        ProviderType      = GitPlatformType.GitHub,
        RepoFullName      = request.Repository.FullName,
        PullRequestNumber = request.Number,
        Title             = request.PullRequest.Title,
        PullRequestRef    = new PullRequestRef
        {
            HeadCommitSha = request.PullRequest.Head.Sha,
            SourceBranch  = request.PullRequest.Head.Ref,
            TargetBranch  = request.PullRequest.Base.Ref,
            DiffUrl       = request.PullRequest.DiffUrl
        }
    };

    // ④ 直接 await（非 fire-and-forget）
    var result = await _codeReviewService.StartAsync(command, cancellationToken);

    return Ok(new
    {
        message     = "Webhook processed",
        repository  = command.RepoFullName,
        pullRequest = command.PullRequestNumber,
        isSuccess   = result.IsSuccess
    });
}
```

> GitLab、Bitbucket Action 結構相同，差異只在：ShouldProcess 條件的 header/欄位不同、Signature 驗證方式不同、Command 組裝欄位對應不同。

---

## 六、Step 4 — PullRequestIgnoreOptions

放置位置：`NineYi.Ai.CodeReview.Application/Options/`

### 4-A. Options Class

```csharp
// Options/PullRequestIgnoreOptions.cs
namespace NineYi.Ai.CodeReview.Application.Options;

public class PullRequestIgnoreOptions
{
    public const string SectionName = "PullRequestIgnore";

    /// <summary>
    /// PR 標題包含這些關鍵字時，跳過 Code Review。
    /// 比對方式：Contains（不分大小寫）
    /// </summary>
    public List<string> TitleKeywords { get; set; } = new();
}
```

### 4-B. appsettings.json 新增

```json
"PullRequestIgnore": {
  "TitleKeywords": [ "[WIP]", "[SKIP]", "[NO-REVIEW]", "auto-", "chore:" ]
}
```

### 4-C. DI 註冊移至 Application/DependencyInjection.cs

```csharp
// Application/DependencyInjection.cs
using Microsoft.Extensions.Configuration;
using NineYi.Ai.CodeReview.Application.Options;

public static IServiceCollection AddApplication(
    this IServiceCollection services,
    IConfiguration configuration)  // ← 新增參數
{
    services.AddScoped<ICodeReviewService, CodeReviewService>();
    services.AddScoped<IStatisticsService, StatisticsService>();

    // PR Title Ignore Gate 設定
    services.Configure<PullRequestIgnoreOptions>(
        configuration.GetSection(PullRequestIgnoreOptions.SectionName));

    return services;
}
```

### 4-D. Program.cs 對應調整

```csharp
// 原本
builder.Services.AddApplication();

// 改為
builder.Services.AddApplication(builder.Configuration);
```

---

## 六、Step 4-B — WebhookSecretsOptions（Webhook Secret 設定）

> 討論後決定：Webhook Secret 不存 DB，從 secrets config 讀取（`secrets.json` 或 Key Vault 等外部來源）。

放置位置：`NineYi.Ai.CodeReview.Application/Options/`

```csharp
// Options/WebhookSecretsOptions.cs
public class WebhookSecretsOptions
{
    public const string SectionName = "_N1SECRETS";

    public GitHubSecretOptions GitHub { get; set; } = new();
    public GitLabSecretOptions GitLab { get; set; } = new();

    public class GitHubSecretOptions
    {
        /// <summary>用於驗證 X-Hub-Signature-256（HMAC-SHA256）。為空時跳過驗證。</summary>
        public string? WebhookSecret { get; set; }
    }

    public class GitLabSecretOptions
    {
        /// <summary>用於驗證 X-Gitlab-Token（直接比對）。為空時跳過驗證。</summary>
        public string? WebhookSecret { get; set; }
    }
}
```

**appsettings.json 加入區段結構（值為空，實際值由外部 secrets config 注入）：**

```json
"_N1SECRETS": {
  "GitHub": { "WebhookSecret": "" },
  "GitLab": { "WebhookSecret": "" }
}
```

**DI 註冊（同 PullRequestIgnoreOptions 一起在 Application/DependencyInjection.cs）：**

```csharp
services.Configure<WebhookSecretsOptions>(
    configuration.GetSection(WebhookSecretsOptions.SectionName));
```

**Controller 注入：**

```csharp
// 原本（注入 IPlatformSettingsRepository 查 DB）
public WebhookController(
    IEnumerable<IWebhookParserService> parsers,
    ICodeReviewService codeReviewService,
    IPlatformSettingsRepository platformSettingsRepository,
    IGitPlatformServiceFactory gitPlatformServiceFactory,
    ILogger<WebhookController> logger)

// 現在（從 config 讀 secret，不查 DB）
public WebhookController(
    ICodeReviewService codeReviewService,
    IOptions<WebhookSecretsOptions> secrets,
    ILogger<WebhookController> logger)
```

---

### 5-A. Domain 層：ReviewStatus 補充

**`ReviewStatus` 新增 `Skipped`（Domain/Entities/ReviewLog.cs）：**

```csharp
public enum ReviewStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4,
    Skipped = 5    // ← 新增：PR title 命中 ignore keyword，主動跳過
}
```

> **⚠️ 決策記錄：Skip 不寫 DB**
>
> 原始規格曾規劃 skip 時建立 `ReviewLog（Status = Skipped）`並新增 `SkipReason` 欄位，但討論後確認：
> - Skip 是「有意識的略過」，不是業務流程的一部分
> - 不需要追蹤紀錄，只需要 ILogger 紀錄即可
> - 不寫 DB 可避免 ReviewLog 被無意義的 skip 紀錄污染
>
> 因此：
> - `ReviewLog.SkipReason` 欄位**不新增**
> - **不執行 EF Core Migration**
> - `ReviewStatus.Skipped` 保留在 enum（未來可能有其他用途）

---

### 7-B. ICodeReviewService 新增方法

```csharp
public interface ICodeReviewService
{
    // ← 新增：Phase 1 統一入口
    Task<ReviewResultDto> StartAsync(
        StartCodeReviewCommand command,
        CancellationToken cancellationToken = default);

    // ← 保留：Phase 2 移除，目前由 StartAsync 橋接呼叫
    Task<ReviewResultDto> ProcessPullRequestAsync(
        WebhookPayload payload,
        CancellationToken cancellationToken = default);

    Task<ReviewResultDto?> GetReviewStatusAsync(
        Guid reviewLogId,
        CancellationToken cancellationToken = default);
}
```

---

### 7-C. CodeReviewService.StartAsync 完整實作

```csharp
public async Task<ReviewResultDto> StartAsync(
    StartCodeReviewCommand command,
    CancellationToken cancellationToken = default)
{
    // Step 1: PR Title Ignore Gate
    // 命中 keyword → 只寫 ILogger，不寫 DB，直接回傳
    var matchedKeyword = _ignoreOptions.TitleKeywords
        .FirstOrDefault(k => command.Title.Contains(k, StringComparison.OrdinalIgnoreCase));

    if (matchedKeyword is not null)
    {
        _logger.LogInformation(
            "PR #{Number} in {Repo} skipped — title matched ignore keyword '{Keyword}'. Title: {Title}",
            command.PullRequestNumber, command.RepoFullName, matchedKeyword, command.Title);

        return new ReviewResultDto
        {
            IsSuccess = true,
            ErrorMessage = $"Skipped: PR title matched ignore keyword '{matchedKeyword}'"
        };
    }

    // Step 2: 橋接現有邏輯（Phase 2 再重構移入 StartAsync）
    var payload = new WebhookPayload
    {
        Platform    = command.ProviderType,
        Repository  = new WebhookRepository { FullName = command.RepoFullName },
        PullRequest = new WebhookPullRequest
        {
            Number       = command.PullRequestNumber,
            Title        = command.Title,
            HeadSha      = command.PullRequestRef.HeadCommitSha,
            SourceBranch = command.PullRequestRef.SourceBranch,
            TargetBranch = command.PullRequestRef.TargetBranch
        }
    };

    return await ProcessPullRequestAsync(payload, cancellationToken);
}
```

---

## 八、Step 6 — DI 清理

### 8-A. Infrastructure/DependencyInjection.cs — 移除 Parser 註冊

```diff
- using NineYi.Ai.CodeReview.Infrastructure.Services.WebhookParsers;

  // Dify Service
  services.AddScoped<IDifyService, DifyService>();

- // Webhook Parsers
- services.AddScoped<IWebhookParserService, GitHubWebhookParser>();
- services.AddScoped<IWebhookParserService, GitLabWebhookParser>();
- services.AddScoped<IWebhookParserService, BitbucketWebhookParser>();
```

### 8-B. Application/DependencyInjection.cs — 新增 IConfiguration 參數

```diff
+ using Microsoft.Extensions.Configuration;
+ using NineYi.Ai.CodeReview.Application.Options;

- public static IServiceCollection AddApplication(this IServiceCollection services)
+ public static IServiceCollection AddApplication(
+     this IServiceCollection services,
+     IConfiguration configuration)
  {
      services.AddScoped<ICodeReviewService, CodeReviewService>();
      services.AddScoped<IStatisticsService, StatisticsService>();

+     // PR Title Ignore Gate 設定（由 Step 4 引入）
+     services.Configure<PullRequestIgnoreOptions>(
+         configuration.GetSection(PullRequestIgnoreOptions.SectionName));

      return services;
  }
```

### 8-C. Api/Program.cs — AddApplication 傳入 Configuration

```diff
- builder.Services.AddApplication();
+ builder.Services.AddApplication(builder.Configuration);
  builder.Services.AddInfrastructure(builder.Configuration);
```

### Parser 檔案的處置

| 檔案 | Phase 1 動作 | 原因 |
|------|------------|------|
| `WebhookParsers/GitHubWebhookParser.cs` | **保留，不刪** | DI 已移除，不影響編譯；Phase 2 整批清除 |
| `WebhookParsers/GitLabWebhookParser.cs` | **保留，不刪** | 同上 |
| `WebhookParsers/BitbucketWebhookParser.cs` | **保留，不刪** | 同上 |

---

## 九、Todo 清單與依賴關係

| # | 工作項目 | 異動檔案 | 依賴 | 狀態 |
|---|---------|---------|------|------|
| 1 | 建立三個 Webhook Request Entity | `Api/Models/Webhooks/*.cs`（新增） | — | ✅ |
| 2 | 建立 `StartCodeReviewCommand` + `PullRequestRef` | `Application/Commands/*.cs`（新增） | — | ✅ |
| 3 | 改版 `WebhookController` | `Api/Controllers/WebhookController.cs` | 1、2 | ✅ |
| 4 | 新增 `PullRequestIgnoreOptions` + `WebhookSecretsOptions` + appsettings | `Application/Options/*.cs`（新增）、`appsettings.json` | — | ✅ |
| 5 | Domain 補充：`ReviewStatus.Skipped`（不新增 SkipReason，不做 Migration） | `Domain/Entities/ReviewLog.cs` | — | ✅ |
| 6 | `ICodeReviewService.StartAsync` + `CodeReviewService` 實作 | `Application/Services/ICodeReviewService.cs`、`CodeReviewService.cs` | 2、4、5 | ✅ |
| 7 | DI 清理 + Program.cs 調整 | `Infrastructure/DependencyInjection.cs`、`Application/DependencyInjection.cs`、`Program.cs` | 3、6 | ✅ |

**依賴圖：**
```
[1] Entity  ──┐
              ├──→ [3] Controller 改版 ──┐
[2] Command ──┘                         │
                                        ├──→ [7] DI 清理
[4] Options ──┐                         │
              ├──→ [6] StartAsync ───────┘
[5] Domain  ──┘
```
