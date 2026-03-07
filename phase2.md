# Phase 2 實作規格書 — Diff 取得層重建

---

## 一、Phase 2 完成定義（Definition of Done）

Phase 2 目標是 **建立平台無關的 Diff 取得抽象層**，讓 `CodeReviewService.StartAsync` 能透過統一介面取得 diff 與 raw content，不再依賴舊的 `IGitPlatformService`、不再走 bridge 到 `ProcessPullRequestAsync`。

Phase 2 結束後，系統必須達到以下可驗證狀態：

| # | 驗收條件 |
|---|---------|
| 1 | `IRepoHostClient` 介面存在，定義兩個方法：取 diff 集合（`GetPullRequestDiffFilesAsync`）與取 raw content（`GetFileRawContentAsync`） |
| 2 | `IRepoHostClientFactory` 可依 `GitPlatformType` 回傳對應 client |
| 3 | `GitHubClient`、`GitLabClient`、`BitbucketClient` 三個實作存在 |
| 4 | GitLab diff 透過 GitLab API 取得（Phase 1 中 `DiffUrl = null` 問題解決） |
| 5 | `FileDiffItem` 包含逐檔 diff 文字、副檔名、以及從 diff hunk 解析出的 new-端行號範圍 |
| 6 | 暫時性錯誤（timeout / 5xx / 429）自動 retry 3 次；401 / 403 / 404 不 retry |
| 7 | Platform Access Token 從 `_N1SECRETS` config 讀取，不再從 DB（`PlatformSettings.AccessToken`）取 |
| 8 | `CodeReviewService.StartAsync` 直接執行 Steps 1-4（Title Ignore Gate → resolve client → 取 diff → 取 raw content），移除對 `ProcessPullRequestAsync` 的 bridge 呼叫 |
| 9 | `FileExcludeOptions` 支援排除路徑前綴與副檔名，client 回傳前先做過濾 |

> **Phase 2 不涉及**：Repo 白名單驗證、Rules 查詢、Dify API 呼叫、Post comment 回 Git 平台。這些屬於 Phase 3。

---

## 二、異動檔案清單

```
NineYi.Ai.CodeReview.Application/
├── Abstractions/                              ← 新增
│   ├── IRepoHostClient.cs                    ← 新增
│   └── IRepoHostClientFactory.cs             ← 新增
├── Models/                                    ← 新增
│   └── FileDiffItem.cs                       ← 新增
├── Options/                                   ← 擴充
│   ├── WebhookSecretsOptions.cs              ← 修改：加入 AccessToken 欄位
│   └── FileExcludeOptions.cs                 ← 新增
└── Services/
    └── CodeReviewService.cs                   ← 修改：StartAsync Steps 2-4 重寫，移除 bridge

NineYi.Ai.CodeReview.Infrastructure/
├── Clients/                                   ← 新增
│   ├── GitHubClient.cs                       ← 新增
│   ├── GitLabClient.cs                       ← 新增
│   ├── BitbucketClient.cs                    ← 新增
│   └── RepoHostClientFactory.cs              ← 新增
├── Http/                                      ← 新增
│   └── HttpPolicies.cs                       ← 新增（Polly retry）
└── DependencyInjection.cs                     ← 修改：註冊新 clients + factory + Polly

NineYi.Ai.CodeReview.Api/
└── appsettings.json                           ← 修改：_N1SECRETS 加入 AccessToken 欄位
```

---

## 三、Step-by-Step 實作細節

---

### Step 1：擴充 WebhookSecretsOptions — 加入 AccessToken

`WebhookSecretsOptions` 目前只放 WebhookSecret。Phase 2 把 Access Token 也納入，讓所有平台憑證統一由 secrets config 管理。

```csharp
// Application/Options/WebhookSecretsOptions.cs（修改）
public class WebhookSecretsOptions
{
    public const string SectionName = "_N1SECRETS";

    public GitHubSecretOptions GitHub { get; set; } = new();
    public GitLabSecretOptions GitLab { get; set; } = new();
    public BitbucketSecretOptions Bitbucket { get; set; } = new();

    public class GitHubSecretOptions
    {
        /// <summary>驗證 X-Hub-Signature-256（HMAC-SHA256）。為空時跳過驗證。</summary>
        public string? WebhookSecret { get; set; }

        /// <summary>呼叫 GitHub REST API 用的 Personal Access Token 或 App Token。</summary>
        public string? AccessToken { get; set; }
    }

    public class GitLabSecretOptions
    {
        /// <summary>驗證 X-Gitlab-Token（直接比對）。為空時跳過驗證。</summary>
        public string? WebhookSecret { get; set; }

        /// <summary>呼叫 GitLab API 用的 Personal Access Token。</summary>
        public string? AccessToken { get; set; }

        /// <summary>GitLab 自架站台的 base URL，預設為空（使用 gitlab.com）。</summary>
        public string? ApiBaseUrl { get; set; }
    }

    public class BitbucketSecretOptions
    {
        /// <summary>呼叫 Bitbucket API 用的 App Password 或 Access Token。</summary>
        public string? AccessToken { get; set; }

        /// <summary>Bitbucket App Password 使用者名稱（Basic Auth 搭配 AccessToken 用）。</summary>
        public string? Username { get; set; }
    }
}
```

**appsettings.json 結構（值留空，由外部 secrets config 注入）：**

```json
"_N1SECRETS": {
  "GitHub":    { "WebhookSecret": "", "AccessToken": "" },
  "GitLab":    { "WebhookSecret": "", "AccessToken": "", "ApiBaseUrl": "" },
  "Bitbucket": { "AccessToken": "", "Username": "" }
}
```

---

### Step 2：FileDiffItem 模型

放置位置：`NineYi.Ai.CodeReview.Application/Models/`

```csharp
// Application/Models/FileDiffItem.cs（新增）

/// <summary>單一檔案的 diff 資訊，由 IRepoHostClient 回傳。</summary>
public class FileDiffItem
{
    /// <summary>檔案名稱（含路徑），例如 "src/Foo.cs"。</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>副檔名（含句點），例如 ".cs"。用於依 FileType 篩選 Rules。</summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>該檔案的完整 unified diff 文字。</summary>
    public string Diff { get; set; } = string.Empty;

    /// <summary>
    /// 從 diff hunk 解析出的 new-端行號範圍集合。
    /// 每個 hunk 產生一個 (Start, End) 範圍，對應 new 端新增/修改的行。
    /// </summary>
    public IReadOnlyList<LineRange> ChangedLineRanges { get; set; } = [];
}

/// <summary>行號範圍（inclusive，1-based）。</summary>
public record LineRange(int Start, int End);
```

---

### Step 3：Diff Hunk 解析工具

`ChangedLineRanges` 由一個靜態 helper 從 unified diff 文字解析：

```
@@ -oldStart,oldCount +newStart,newCount @@
         ↑ 只取 new 端（+）
         Start = newStart
         End   = newStart + newCount - 1
```

放置位置：`Application/Utilities/DiffHunkParser.cs`

```csharp
/// <summary>解析 unified diff 的 hunk header，回傳 new-端行號範圍集合。</summary>
public static class DiffHunkParser
{
    // Regex: @@ -old,count +newStart,newCount @@
    private static readonly Regex HunkHeaderRegex =
        new(@"@@\s+-\d+(?:,\d+)?\s+\+(\d+)(?:,(\d+))?\s+@@", RegexOptions.Compiled);

    public static IReadOnlyList<LineRange> Parse(string diff)
    {
        var ranges = new List<LineRange>();
        foreach (Match m in HunkHeaderRegex.Matches(diff))
        {
            int start = int.Parse(m.Groups[1].Value);
            int count = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 1;
            if (count > 0)
                ranges.Add(new LineRange(start, start + count - 1));
        }
        return ranges;
    }
}
```

---

### Step 4：IRepoHostClient + IRepoHostClientFactory 介面

放置位置：`NineYi.Ai.CodeReview.Application/Abstractions/`

```csharp
// Application/Abstractions/IRepoHostClient.cs（新增）

/// <summary>跨平台 Git Repo 操作抽象，負責取得 diff 與 raw content。</summary>
public interface IRepoHostClient
{
    /// <summary>
    /// 取得 PR/MR 的逐檔 diff 集合（已過濾排除規則）。
    /// </summary>
    Task<IReadOnlyList<FileDiffItem>> GetPullRequestDiffFilesAsync(
        string repoFullName,
        int pullRequestNumber,
        PullRequestRef prRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得指定 commitSha 下某檔案的 raw 內容。
    /// </summary>
    Task<string> GetFileRawContentAsync(
        string repoFullName,
        string filePath,
        string commitSha,
        CancellationToken cancellationToken = default);
}
```

```csharp
// Application/Abstractions/IRepoHostClientFactory.cs（新增）

/// <summary>依 GitPlatformType 取得對應的 IRepoHostClient。</summary>
public interface IRepoHostClientFactory
{
    IRepoHostClient GetClient(GitPlatformType platformType);
}
```

---

### Step 5：FileExcludeOptions

放置位置：`NineYi.Ai.CodeReview.Application/Options/`

```csharp
// Application/Options/FileExcludeOptions.cs（新增）
public class FileExcludeOptions
{
    public const string SectionName = "FileExclude";

    /// <summary>排除的路徑前綴，例如 ["Migrations/", "i18n/"]。</summary>
    public List<string> PathPrefixes { get; set; } = new();

    /// <summary>排除的副檔名（含句點），例如 [".min.js", ".generated.cs"]。</summary>
    public List<string> Extensions { get; set; } = new();
}
```

**appsettings.json 範例：**
```json
"FileExclude": {
  "PathPrefixes": ["Migrations/", "i18n/", "AutoGenerated/"],
  "Extensions":   [".min.js", ".designer.cs"]
}
```

---

### Step 6：三個 Infrastructure Client 實作

放置位置：`NineYi.Ai.CodeReview.Infrastructure/Clients/`

#### 6.1 GitHubClient

**取 diff：**
- Endpoint：`GET /repos/{owner}/{repo}/pulls/{pull_number}/files`
- Headers：`Authorization: Bearer {AccessToken}`、`Accept: application/vnd.github+json`
- Response：JSON 陣列，每個物件含 `filename`（完整路徑）、`patch`（unified diff 文字）、`status`（`added/modified/removed/renamed`）
- 解析：取 `patch` 欄位餵給 `DiffHunkParser.Parse()` 取行號範圍；`status == "removed"` 跳過

**取 raw content：**
- Endpoint：`GET /repos/{owner}/{repo}/contents/{filePath}?ref={commitSha}`
- Headers：`Authorization: Bearer {AccessToken}`、**`Accept: application/vnd.github.v3.raw`**
- 帶 `Accept: application/vnd.github.v3.raw` 時 API 直接回傳純文字，不需要 base64 decode

**RepoFullName 解析：** `{owner}/{repo}` 從 `repoFullName` 用 `/` 拆分取得

#### 6.2 GitLabClient

**取 diff（使用新 API）：**
- Endpoint：`GET /api/v4/projects/{projectId}/merge_requests/{iid}/diffs`
- Headers：`PRIVATE-TOKEN: {AccessToken}`（勿用 query param，避免 token 出現在 log）
- Response：JSON 陣列，每個物件含 `new_path`、`old_path`、`diff`（unified diff 文字）、`new_file`、`deleted_file`、`renamed_file`
- 解析：取 `diff` 欄位餵給 `DiffHunkParser.Parse()`；`deleted_file == true` 跳過
- `{projectId}` 從 `StartCodeReviewCommand.PlatformProjectId` 取得
- `{iid}` 為 MR iid（`StartCodeReviewCommand.PullRequestNumber`）
- **ApiBaseUrl**：從 `WebhookSecretsOptions.GitLab.ApiBaseUrl` 取得，支援自架 GitLab

**取 raw content：**
- Endpoint：`GET /api/v4/projects/{projectId}/repository/files/{encodedPath}/raw?ref={commitSha}`
- Headers：`PRIVATE-TOKEN: {AccessToken}`
- **注意：`filePath` 必須先 URL encode**（`Uri.EscapeDataString(filePath)`），路徑中的 `/` 也要 encode 成 `%2F`
- `{commitSha}` 傳入 `HeadCommitSha` 確保取的是 source branch 的版本

> **Phase 1 遺留問題解決**：`StartCodeReviewCommand` 目前沒有帶 `ProjectId`（GitLab 數字 ID）。Phase 2 需要在 `GitLabWebhookRequest → StartCodeReviewCommand` 的組裝中加入 `ProjectId` 欄位，或透過 `RepoFullName` 查詢。

#### 6.3 BitbucketClient

**取 diff：**
- URL 來源：直接使用 `PullRequestRef.DiffUrl`（Bitbucket webhook payload 已提供，不需自行組裝）
- Headers：`Authorization: Bearer {AccessToken}`
- Response：純文字 unified diff（`text/x-diff`），多個檔案串接在一起，用 `diff --git` 分隔
- **特殊處理：302 Redirect** — Bitbucket diff URL 可能重導向到 CDN：
  ```
  1. 打 DiffUrl（帶 Authorization header）
  2. 若收到 302，從 Location header 取新 URL
  3. 用新 URL 重打（不帶 Authorization header，CDN 不需認證）
  ```
- 解析：Regex `@"diff\s--[\s\S]*?(?=diff\s--)|diff\s--[\s\S]*"` 切出逐檔 section；再從 `diff --git a/... b/{path}/{filename}` 取路徑與檔名；最後 `DiffHunkParser.Parse()` 取行號範圍

**取 raw content：**
- Endpoint：`GET /repositories/{workspace}/{repoSlug}/src/{commitSha}/{filePath}`
- Headers：`Authorization: Bearer {AccessToken}`
- `workspace` 與 `repoSlug` 從 `repoFullName`（格式 `{workspace}/{repoSlug}`）用 `/` 拆分取得

---

### Step 7：Polly Retry Policy

放置位置：`NineYi.Ai.CodeReview.Infrastructure/Http/HttpPolicies.cs`

```csharp
public static class HttpPolicies
{
    /// <summary>
    /// 暫時性錯誤 retry 3 次（exponential backoff）。
    /// 暫時性：timeout、5xx、429
    /// 永久性（不 retry）：401、403、404
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> RetryPolicy =>
        HttpPolicyExtensions
            .HandleTransientHttpError()  // 5xx, timeout
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}
```

DI 注冊：透過 `services.AddHttpClient<GitHubClient>().AddPolicyHandler(HttpPolicies.RetryPolicy)`

---

### Step 8：重寫 CodeReviewService.StartAsync（Steps 2-4）

移除 bridge 到 `ProcessPullRequestAsync`，改為：

```
Step 1: PR Title Ignore Gate（維持不變）

Step 2: Resolve Client
        client = _clientFactory.GetClient(command.ProviderType)

Step 3: 取得逐檔 Diff 集合
        files = await client.GetPullRequestDiffFilesAsync(
                    command.RepoFullName,
                    command.PullRequestNumber,
                    command.PullRequestRef,
                    cancellationToken)

Step 4: 取得每個檔案的 Raw Content
        foreach file in files:
            rawContent = await client.GetFileRawContentAsync(
                             command.RepoFullName,
                             file.FilePath,
                             command.PullRequestRef.HeadCommitSha,
                             cancellationToken)

        ← Phase 2 暫停在這裡，diff + rawContent 已備妥
        ← 後續 Step 5~8（Rules、Dify、Post comment）= Phase 3
```

**`ProcessPullRequestAsync` 先保留不動**（Phase 3 移除），確保不破壞現有 DB / rule / Dify 流程。

---

### Step 9：DI 註冊

修改 `Infrastructure/DependencyInjection.cs`：

```csharp
// 新增
services.AddHttpClient<GitHubClient>()
        .AddPolicyHandler(HttpPolicies.RetryPolicy);

services.AddHttpClient<GitLabClient>()
        .AddPolicyHandler(HttpPolicies.RetryPolicy);

services.AddHttpClient<BitbucketClient>()
        .AddPolicyHandler(HttpPolicies.RetryPolicy);

services.AddSingleton<IRepoHostClientFactory, RepoHostClientFactory>();
```

修改 `Application/DependencyInjection.cs`：

```csharp
// 新增
services.Configure<FileExcludeOptions>(
    configuration.GetSection(FileExcludeOptions.SectionName));
```

---

## 四、GitLab ProjectId 傳遞問題

GitLab API 需要 numeric `projectId`（例如 `12345`），而 `StartCodeReviewCommand` 目前只有 `RepoFullName`（`namespace/repo`）。

決定採用**方案 A：Command 加入 `PlatformProjectId`**：

```csharp
// StartCodeReviewCommand 新增欄位
public string? PlatformProjectId { get; init; }
```

由 `WebhookController.GitLab()` 從 payload 填入（`GitLabWebhookRequest.Project.Id`），其他平台填 `null`。

---

## 五、Phase 2 Todo 清單

| # | 工作項目 | 異動檔案 | 依賴 | 狀態 |
|---|---------|---------|------|------|
| 1 | 擴充 `WebhookSecretsOptions`（加 AccessToken） | `Application/Options/WebhookSecretsOptions.cs`、`appsettings.json` | — | ⬜ |
| 2 | 建立 `FileDiffItem` + `LineRange` | `Application/Models/FileDiffItem.cs` | — | ⬜ |
| 3 | 建立 `DiffHunkParser` 工具 | `Application/Utilities/DiffHunkParser.cs` | 2 | ⬜ |
| 4 | 定義 `IRepoHostClient` + `IRepoHostClientFactory` | `Application/Abstractions/*.cs` | 2 | ⬜ |
| 5 | 建立 `FileExcludeOptions` | `Application/Options/FileExcludeOptions.cs` | — | ⬜ |
| 6 | `StartCodeReviewCommand` 加入 `PlatformProjectId` | `Application/Commands/StartCodeReviewCommand.cs`、`WebhookController.cs` | — | ⬜ |
| 7 | 實作 `GitHubClient` | `Infrastructure/Clients/GitHubClient.cs` | 3、4、5 | ⬜ |
| 8 | 實作 `GitLabClient` | `Infrastructure/Clients/GitLabClient.cs` | 3、4、5 | ⬜ |
| 9 | 實作 `BitbucketClient` | `Infrastructure/Clients/BitbucketClient.cs` | 3、4、5 | ⬜ |
| 10 | 實作 `RepoHostClientFactory` | `Infrastructure/Clients/RepoHostClientFactory.cs` | 7、8、9 | ⬜ |
| 11 | Polly retry policy | `Infrastructure/Http/HttpPolicies.cs` | — | ⬜ |
| 12 | 重寫 `CodeReviewService.StartAsync` Steps 2-4 | `Application/Services/CodeReviewService.cs` | 4、10 | ⬜ |
| 13 | DI 註冊清理 | `Infrastructure/DependencyInjection.cs`、`Application/DependencyInjection.cs` | 7~12 | ⬜ |
