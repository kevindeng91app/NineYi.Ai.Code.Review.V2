# NineYi.Ai.Code.Review.V2

AI-powered Code Review Service supporting GitHub, GitLab, and Bitbucket webhooks with Dify API integration.

## Features

- **Multi-Platform Webhook Support**: Receive and process webhooks from GitHub, GitLab, and Bitbucket
- **Dify API Integration**: Leverage multiple Dify AI rules for comprehensive code review
- **Repository Rule Mapping**: Configure different rules for different repositories stored in database
- **Hot Keywords Detection**: Identify sensitive keywords (passwords, secrets, etc.) and alert developers
- **Cost Tracking**: Monitor Dify API usage and costs
- **Rule Statistics**: Track rule trigger frequency to optimize prompts
- **Execution Logging**: Full trace of review execution for debugging and auditing

## Architecture

The project follows Clean Architecture principles:

```
src/
├── NineYi.Ai.CodeReview.Api/           # WebAPI Layer
│   ├── Controllers/
│   │   ├── WebhookController.cs        # GitHub/GitLab/Bitbucket webhooks
│   │   ├── RepositoriesController.cs   # Repository management
│   │   ├── RulesController.cs          # Rule management
│   │   ├── HotKeywordsController.cs    # Keyword management
│   │   └── StatisticsController.cs     # Usage & cost statistics
│   └── Program.cs
│
├── NineYi.Ai.CodeReview.Application/   # Application Layer
│   ├── DTOs/
│   └── Services/
│       ├── CodeReviewService.cs        # Core review logic
│       └── StatisticsService.cs        # Statistics aggregation
│
├── NineYi.Ai.CodeReview.Domain/        # Domain Layer
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
└── NineYi.Ai.CodeReview.Infrastructure/ # Infrastructure Layer
    ├── Data/
    │   └── CodeReviewDbContext.cs
    ├── Repositories/
    └── Services/
        ├── GitHubService.cs
        ├── GitLabService.cs
        ├── BitbucketService.cs
        └── DifyService.cs
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server (or SQL Server Express)
- Dify API access

### Configuration

1. Update `appsettings.json` with your database connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NineYiCodeReview;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

2. Run database migrations:

```bash
cd src/NineYi.Ai.CodeReview.Api
dotnet ef migrations add InitialCreate --project ../NineYi.Ai.CodeReview.Infrastructure
dotnet ef database update --project ../NineYi.Ai.CodeReview.Infrastructure
```

3. Start the application:

```bash
dotnet run --project src/NineYi.Ai.CodeReview.Api
```

### API Endpoints

#### Webhooks
- `POST /api/webhook/github` - GitHub webhook endpoint
- `POST /api/webhook/gitlab` - GitLab webhook endpoint
- `POST /api/webhook/bitbucket` - Bitbucket webhook endpoint

#### Repository Management
- `GET /api/repositories` - List all repositories
- `POST /api/repositories` - Add a repository
- `PUT /api/repositories/{id}` - Update a repository
- `DELETE /api/repositories/{id}` - Delete a repository

#### Rule Management
- `GET /api/rules` - List all rules
- `POST /api/rules` - Create a rule
- `POST /api/rules/{ruleId}/repositories/{repositoryId}` - Map rule to repository
- `DELETE /api/rules/{ruleId}/repositories/{repositoryId}` - Unmap rule from repository

#### Hot Keywords
- `GET /api/hotkeywords` - List all hot keywords
- `POST /api/hotkeywords` - Create a hot keyword

#### Statistics
- `GET /api/statistics/usage` - Get usage summary (includes Dify costs)
- `GET /api/statistics/top-triggered-rules` - Get most triggered rules
- `GET /api/statistics/cost-by-rule` - Get cost breakdown by rule

## Setting Up Webhooks

### GitHub
1. Go to Repository Settings > Webhooks > Add webhook
2. Payload URL: `https://your-domain/api/webhook/github`
3. Content type: `application/json`
4. Secret: Your webhook secret (optional but recommended)
5. Events: Select "Pull requests"

### GitLab
1. Go to Repository Settings > Webhooks
2. URL: `https://your-domain/api/webhook/gitlab`
3. Secret token: Your webhook secret
4. Trigger: Select "Merge request events"

### Bitbucket
1. Go to Repository Settings > Webhooks > Add webhook
2. URL: `https://your-domain/api/webhook/bitbucket`
3. Triggers: Select "Pull Request: Created" and "Pull Request: Updated"

## Database Schema

### Main Tables
- **Repositories** - Git repository configurations
- **Rules** - Dify API rules for code review
- **RepositoryRuleMappings** - Many-to-many relationship between repositories and rules
- **ReviewLogs** - Code review execution logs
- **ReviewFileLogs** - Per-file review details
- **HotKeywords** - Keywords to detect and alert
- **RuleStatistics** - Daily rule trigger statistics
- **DifyUsageLogs** - Dify API usage and cost tracking

## License

MIT
