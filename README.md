# NineYi.Ai.Code.Review.V2

AI 驅動的 Code Review 服務，支援 GitHub、GitLab 和 Bitbucket 的 Webhook，整合 Dify API 進行智慧審查。

## 功能特色

- **多平台 Webhook 支援**：接收並處理 GitHub、GitLab、Bitbucket 的 Webhook
- **Dify API 整合**：運用多個 Dify AI 規則進行全面的程式碼審查
- **Repository 規則對應**：為不同的 Repository 設定不同的審查規則，儲存於資料庫
- **敏感關鍵字偵測**：識別敏感關鍵字（密碼、金鑰等）並提醒開發人員
- **費用追蹤**：監控 Dify API 使用量與費用
- **規則統計**：追蹤規則觸發頻率，用於優化 Prompt 精準度
- **執行紀錄**：完整追蹤審查執行過程，方便除錯與稽核

## 架構

專案採用 Clean Architecture 架構：

```
src/
├── NineYi.Ai.CodeReview.Api/           # WebAPI 層
│   ├── Controllers/
│   │   ├── WebhookController.cs        # GitHub/GitLab/Bitbucket webhooks
│   │   ├── RepositoriesController.cs   # Repository 管理
│   │   ├── RulesController.cs          # 規則管理
│   │   ├── HotKeywordsController.cs    # 關鍵字管理
│   │   └── StatisticsController.cs     # 使用量與費用統計
│   └── Program.cs
│
├── NineYi.Ai.CodeReview.Application/   # 應用層
│   ├── DTOs/
│   └── Services/
│       ├── CodeReviewService.cs        # 核心審查邏輯
│       └── StatisticsService.cs        # 統計彙整
│
├── NineYi.Ai.CodeReview.Domain/        # 領域層
│   ├── Entities/
│   │   ├── Repository.cs
│   │   ├── Rule.cs
│   │   ├── RepositoryRuleMapping.cs
│   │   ├── ReviewLog.cs
│   │   ├── HotKeyword.cs
│   │   ├── RuleStatistics.cs
│   │   └── DifyUsageLog.cs
│   └── Interfaces/
│
└── NineYi.Ai.CodeReview.Infrastructure/ # 基礎設施層
    ├── Data/
    │   └── CodeReviewDbContext.cs
    ├── Repositories/
    └── Services/
        ├── GitHubService.cs
        ├── GitLabService.cs
        ├── BitbucketService.cs
        └── DifyService.cs
```

## 快速開始

### 環境需求

- .NET 8.0 SDK
- Dify API 存取權限

### 設定

應用程式使用 SQLite 資料庫，無需額外安裝。

1. `appsettings.json` 中的資料庫連線字串（預設值即可）：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=CodeReview.db"
  }
}
```

2. 執行資料庫 Migration（可選 - 開發環境會自動建立）：

```bash
cd src/NineYi.Ai.CodeReview.Api
dotnet ef migrations add InitialCreate --project ../NineYi.Ai.CodeReview.Infrastructure
dotnet ef database update --project ../NineYi.Ai.CodeReview.Infrastructure
```

3. 啟動應用程式：

```bash
dotnet run --project src/NineYi.Ai.CodeReview.Api
```

SQLite 資料庫檔案（`CodeReview.db` 或開發環境的 `CodeReview_Dev.db`）會自動建立在應用程式目錄下。

### API 端點

#### Webhooks
- `POST /api/webhook/github` - GitHub webhook 端點
- `POST /api/webhook/gitlab` - GitLab webhook 端點
- `POST /api/webhook/bitbucket` - Bitbucket webhook 端點

#### Repository 管理
- `GET /api/repositories` - 列出所有 repositories
- `POST /api/repositories` - 新增 repository
- `PUT /api/repositories/{id}` - 更新 repository
- `DELETE /api/repositories/{id}` - 刪除 repository

#### 規則管理
- `GET /api/rules` - 列出所有規則
- `POST /api/rules` - 建立規則
- `POST /api/rules/{ruleId}/repositories/{repositoryId}` - 將規則對應到 repository
- `DELETE /api/rules/{ruleId}/repositories/{repositoryId}` - 取消規則對應

#### 敏感關鍵字
- `GET /api/hotkeywords` - 列出所有敏感關鍵字
- `POST /api/hotkeywords` - 建立敏感關鍵字

#### 統計
- `GET /api/statistics/usage` - 取得使用量摘要（包含 Dify 費用）
- `GET /api/statistics/top-triggered-rules` - 取得最常觸發的規則
- `GET /api/statistics/cost-by-rule` - 取得各規則的費用統計

## 設定 Webhooks

### GitHub
1. 前往 Repository Settings > Webhooks > Add webhook
2. Payload URL：`https://your-domain/api/webhook/github`
3. Content type：`application/json`
4. Secret：您的 webhook secret（建議設定）
5. Events：選擇 "Pull requests"

### GitLab
1. 前往 Repository Settings > Webhooks
2. URL：`https://your-domain/api/webhook/gitlab`
3. Secret token：您的 webhook secret
4. Trigger：選擇 "Merge request events"

### Bitbucket
1. 前往 Repository Settings > Webhooks > Add webhook
2. URL：`https://your-domain/api/webhook/bitbucket`
3. Triggers：選擇 "Pull Request: Created" 和 "Pull Request: Updated"

## 資料庫結構

### 主要資料表
- **Repositories** - Git repository 設定
- **Rules** - Dify API 審查規則
- **RepositoryRuleMappings** - Repository 與規則的多對多對應
- **ReviewLogs** - Code review 執行紀錄
- **ReviewFileLogs** - 單檔審查詳細紀錄
- **HotKeywords** - 敏感關鍵字設定
- **RuleStatistics** - 每日規則觸發統計
- **DifyUsageLogs** - Dify API 使用量與費用追蹤

## 授權

MIT
