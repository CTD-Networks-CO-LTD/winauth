# ER図

WinAuthアプリケーションの二要素認証設定・トークン保持構造を表すER図。基底となるAuthenticatorエンティティを中心に、Steam、Battle.net、HOTP等のプロバイダ固有の派生モデル、YubiKeyハードウェアトークン保護情報、およびSteamのトレード確認用セッションクライアント（SteamClient）とのリレーションシップをモデル化しています。

### エンティティ一覧

**AUTHENTICATOR**

| カラム名 | データ型 | キー |
| --- | --- | --- |
| id | UUID | PK |
| name | VARCHAR(100) |  |
| secret_key | VARBINARY(64) |  |
| server_time_diff | BIGINT |  |
| password_type | VARCHAR(50) |  |
| encryption_header | VARCHAR(50) |  |
| created_at | TIMESTAMP |  |
| updated_at | TIMESTAMP |  |

**STEAM_AUTHENTICATOR**

| カラム名 | データ型 | キー |
| --- | --- | --- |
| id | UUID | PK |
| authenticator_id | UUID |  |
| steam_id | VARCHAR(64) |  |
| shared_secret | VARCHAR(255) |  |
| identity_secret | VARCHAR(255) |  |
| device_id | VARCHAR(100) |  |
| revocation_code | VARCHAR(20) |  |

**HOTP_AUTHENTICATOR**

| カラム名 | データ型 | キー |
| --- | --- | --- |
| id | UUID | PK |
| authenticator_id | UUID |  |
| counter | BIGINT |  |
| code_digits | INTEGER |  |
| created_at | TIMESTAMP |  |

**BATTLE_NET_AUTHENTICATOR**

| カラム名 | データ型 | キー |
| --- | --- | --- |
| id | UUID | PK |
| authenticator_id | UUID |  |
| serial | VARCHAR(50) |  |
| restore_code | VARCHAR(50) |  |
| model | VARCHAR(50) |  |
| created_at | TIMESTAMP |  |

**YUBIKEY**

| カラム名 | データ型 | キー |
| --- | --- | --- |
| id | UUID | PK |
| authenticator_id | UUID |  |
| serial_number | VARCHAR(50) |  |
| slot | INTEGER |  |
| seed | VARCHAR(255) |  |
| protected_data | VARBINARY(255) |  |
| created_at | TIMESTAMP |  |

**STEAM_CLIENT**

| カラム名 | データ型 | キー |
| --- | --- | --- |
| id | UUID | PK |
| steam_authenticator_id | UUID |  |
| session_id | VARCHAR(128) |  |
| steam_login_secure | TEXT |  |
| wg_token | VARCHAR(255) |  |
| web_cookie | TEXT |  |
| last_login_at | TIMESTAMP |  |

### リレーション

- AUTHENTICATOR → STEAM_AUTHENTICATOR (1:1)
- AUTHENTICATOR → HOTP_AUTHENTICATOR (1:1)
- AUTHENTICATOR → BATTLE_NET_AUTHENTICATOR (1:1)
- AUTHENTICATOR → YUBIKEY (1:N)
- STEAM_AUTHENTICATOR → STEAM_CLIENT (1:N)

### ER図

```mermaid
erDiagram
    authenticator {
        UUID id PK
        VARCHAR_100 name
        VARBINARY_64 secret_key
        BIGINT server_time_diff
        VARCHAR_50 password_type
        VARCHAR_50 encryption_header
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }
    steam_authenticator {
        UUID id PK
        UUID authenticator_id
        VARCHAR_64 steam_id
        VARCHAR_255 shared_secret
        VARCHAR_255 identity_secret
        VARCHAR_100 device_id
        VARCHAR_20 revocation_code
    }
    hotp_authenticator {
        UUID id PK
        UUID authenticator_id
        BIGINT counter
        INTEGER code_digits
        TIMESTAMP created_at
    }
    battle_net_authenticator {
        UUID id PK
        UUID authenticator_id
        VARCHAR_50 serial
        VARCHAR_50 restore_code
        VARCHAR_50 model
        TIMESTAMP created_at
    }
    yubikey {
        UUID id PK
        UUID authenticator_id
        VARCHAR_50 serial_number
        INTEGER slot
        VARCHAR_255 seed
        VARBINARY_255 protected_data
        TIMESTAMP created_at
    }
    steam_client {
        UUID id PK
        UUID steam_authenticator_id
        VARCHAR_128 session_id
        TEXT steam_login_secure
        VARCHAR_255 wg_token
        TEXT web_cookie
        TIMESTAMP last_login_at
    }
    authenticator ||--|| steam_authenticator : "1:1"
    authenticator ||--|| hotp_authenticator : "1:1"
    authenticator ||--|| battle_net_authenticator : "1:1"
    authenticator ||--o{ yubikey : "1:N"
    steam_authenticator ||--o{ steam_client : "1:N"
```