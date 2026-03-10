# Webhook ShouldProcess 判斷邏輯說明

## 什麼是 ShouldProcess？

當 GitHub / GitLab / Bitbucket 有任何動作（push、issue、PR 建立、PR 留言…）都可能觸發 webhook，但我們只需要處理「PR/MR 有新變化」的事件。

ShouldProcess 就是這個過濾器，**只讓需要做 Code Review 的事件通過，其他一律回傳 200 OK 忽略**（不能回 4xx，否則平台會一直重試）。

---

## 各平台判斷條件

### GitHub

| 判斷項目 | 來源 | 符合條件 |
|---------|------|---------|
| 事件類型 | Header：`X-GitHub-Event` | 必須是 `pull_request` |
| 動作 | Body：`action` | 必須是 `opened` / `synchronize` / `reopened` |

```
opened      → PR 剛建立
synchronize → PR 有新的 commit 推上來
reopened    → PR 從關閉重新開啟
```

---

### GitLab

| 判斷項目 | 來源 | 符合條件 |
|---------|------|---------|
| 事件類型 | Header：`X-Gitlab-Event` | 必須是 `Merge Request Hook` |
| 動作 | Body：`object_attributes.action` | 必須是 `open` / `update` / `reopen` |

```
open   → MR 剛建立
update → MR 有新的 commit 推上來
reopen → MR 從關閉重新開啟
```

> **注意**：GitLab 的 Header 值是 `"Merge Request Hook"`，Body 裡的 `object_kind` 是 `"merge_request"`，兩者是同一個事件的不同表示方式。新版 Controller 改為直接比對 Header，與原本比對 Body 的結果等效。

---

### Bitbucket

| 判斷項目 | 來源 | 符合條件 |
|---------|------|---------|
| 事件類型 | Header：`X-Event-Key` | 必須是 `pullrequest:created` 或 `pullrequest:updated` |
| PR 狀態 | Body：`pullrequest.state` | 必須是 `OPEN` |

```
pullrequest:created → PR 剛建立
pullrequest:updated → PR 有更新（新 commit 或 merge）
```

---

## 新舊版本對照

舊版（Phase 1 之前）是透過 Parser 物件的 `ShouldProcess(payload)` 方法判斷，判斷的值是 Parser 從 body / header 整理後存進 `WebhookPayload` 的欄位。

新版（Phase 1）直接在各自的 Controller action 裡判斷，來源更直接清楚：

| 平台 | 舊版判斷來源 | 新版判斷來源 | 是否等效 |
|------|------------|------------|---------|
| GitHub | Header（經由 Parser 轉存） | Header 直接比對 | ✅ 完全一樣 |
| Bitbucket | Header（經由 Parser 轉存） | Header 直接比對 | ✅ 完全一樣 |
| GitLab | **Body 的 `object_kind`** | **Header 的 `X-Gitlab-Event`** | ✅ 等效，來源不同 |

---

## 不符合條件時的處理

所有不符合條件的 webhook 一律回傳 `200 OK`：

```json
{ "message": "Event skipped" }
```

**為什麼不回 4xx？**  
回傳 4xx（例如 400 Bad Request）會讓 GitHub / GitLab / Bitbucket 誤以為我們的服務有問題，並自動重試，造成不必要的流量。回傳 200 OK 告訴平台「我們收到了，但不需要處理」。
