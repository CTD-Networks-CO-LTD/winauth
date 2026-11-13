# API仕様書

WinAuth はスタンドアロン型デスクトップアプリケーションであり自らは外部公開サーバー API を持たず、各種二要素認証プロバイダおよび時刻同期サービスの外部 HTTPS エンドポイントを呼び出すクライアントとして動作します。本書では呼び出し対象の全外部 API 仕様を定義します（全通信で TLS 1.2 / 1.3 を強制）。

---

## 1. Steam Web API / Steam Community エンドポイント群（`SteamAuthenticator` / `SteamClient`）

| エンドポイントURL | メソッド | 用途・概要 | 主なリクエストパラメータ | レスポンス形式・主要項目 | 認証・署名方式 |
| --- | --- | --- | --- | --- | --- |
| `https://steamcommunity.com/mobilelogin` | GET / POST | モバイル OAuth ログインおよび CAPTCHA・メール認証処理 | `username`, `password`(RSA暗号化), `emailauth`, `captchagid`, `captcha_text`, `oauth_client_id=DE45CD61` | JSON: `success`, `oauth` (`steamid`, `oauth_token`, `wgtoken`) | RSA公開鍵暗号化パスワード |
| `https://api.steampowered.com/ITwoFactorService/AddAuthenticator/v0001` | POST | Steam Guard モバイル認証器の初期登録・SMS送信要求 | `access_token`, `steamid`, `authenticator_type=1`, `device_identifier`, `sms_phone_id=1` | JSON: `response` (`shared_secret`, `identity_secret`, `serial_number`, `revocation_code`, `uri`, `status`) | OAuth `access_token` |
| `https://api.steampowered.com/ITwoFactorService/FinalizeAddAuthenticator/v0001` | POST | SMSコードと生成OTPの送信による認証器有効化確定 | `access_token`, `steamid`, `activation_code`, `authenticator_code`, `authenticator_time` | JSON: `response` (`success`, `want_more`, `server_time`, `status`) | OAuth `access_token` |
| `https://api.steampowered.com/ITwoFactorService/SendEmail/v0001` | POST | 登録完了確認メールの送信トリガー | `access_token`, `steamid`, `email_type=2` | JSON: `response` | OAuth `access_token` |
| `https://api.steampowered.com:443/ITwoFactorService/QueryTime/v0001` | POST | Steam サーバー現在時刻の照会（時刻同期） | なし（空ボディ、Timeout 5,000ms） | JSON: `response.server_time` (Unix秒) | なし |
| `https://api.steampowered.com/IMobileAuthService/GetWGToken/v0001` | POST | Web セッション Cookie 用トークン（WGToken）の再取得 | `access_token` (Form-UrlEncoded) | JSON: `response` (`token`, `token_secure`) → `steamLogin` / `steamLoginSecure` Cookie 構築 | OAuth `access_token` |
| `https://api.steampowered.com/ISteamWebUserPresenceOAuth/Logon/v0001` | POST | Steam Web プレゼンス（UMQ）セッションへのログオン | `access_token` (Form-UrlEncoded) | JSON: `umqid`, `message`, `steamid` | OAuth `access_token` |
| `https://api.steampowered.com/ISteamWebUserPresenceOAuth/PollStatus/v0001` | POST | Web プレゼンスセッションの状態維持ポーリング | `steamid`, `umqid`, `message` | JSON: `error`, `messagelast` | OAuth / `umqid` |
| `https://api.steampowered.com/ISteamWebUserPresenceOAuth/Logoff/v0001` | POST | Steam Web プレゼンスからのログオフ・セッション切断 | `access_token`, `umqid` | JSON: `error` (`OK`) | OAuth `access_token` |
| `https://steamcommunity.com/mobileconf/conf` | GET | 保留中のトレード・マーケット出品確認一覧の取得 | `p` (`device_id`), `a` (`steamid`), `k` (HMAC署名), `t` (Unix秒), `m=android`, `tag=conf` | HTML（`data-confid`, `data-key`, アイコン・取引詳細要素を抽出） | Session Cookie + `IdentitySecret` HMAC-SHA1 |
| `https://steamcommunity.com/mobileconf/details/{id}` | GET | 個別トレード確認項目の詳細HTML取得 | 上記クエリパラメータ（`tag=details`） | JSON: `success`, `html` | Session Cookie + `IdentitySecret` HMAC-SHA1 |
| `https://steamcommunity.com/mobileconf/ajaxop` | GET | 個別トレード確認の承認（Allow）または拒否（Cancel）実行 | `op` (`allow` or `cancel`), `p`, `a`, `k` (HMAC署名), `t`, `m=android`, `tag` (`allow`/`cancel`), `cid`, `ck` | JSON: `{"success": true}` またはエラーメッセージ | Session Cookie + `IdentitySecret` HMAC-SHA1 |

---

## 2. Battle.net Mobile Service エンドポイント群（`BattleNetAuthenticator`）

ベースURL: `https://mobile-service.blizzard.com`（中国地域 `CN` のみ `https://mobile-service.battlenet.com.cn`）

| エンドポイントURL / パス | メソッド | 用途・概要 | リクエストボディ | レスポンスボディ | 認証・暗号化方式 |
| --- | --- | --- | --- | --- | --- |
| `https://geoiplookup.wikimedia.org` | GET | 接続元国コードからのリージョン（US/EU/KR/CN）自動判定 | なし（Timeout 10,000ms） | JSON: `{"country": "JP", ...}` | なし |
| `/enrollment/enroll2.htm` | POST | 新規 Battle.net 認証器の登録（秘密鍵・シリアル発行） | `application/octet-stream` (128B): 20Bワンタイムパッド + 2B国コード + 16Bモデル文字列のRSA暗号文 | Binary (45B): 8Bサーバー時刻 + ワンタイムパッドXOR済み(20B秘密鍵 + 17Bシリアル) | Blizzard 1024bit RSA公開鍵 + OTP XOR |
| `/enrollment/time.htm` | GET | Battle.net サーバー時刻同期 | なし | Binary (8B): Big-Endian UTC ミリ秒時刻 | なし |
| `/enrollment/initiatePaperRestore.htm` | POST | シリアル番号に対するリストア用チャレンジ要求 | `application/octet-stream`: 正規化シリアル文字列バイト列 | Binary (32B): ランダムチャレンジデータ | なし |
| `/enrollment/validatePaperRestore.htm` | POST | リストアコード署名検証および秘密鍵の復元取得 | シリアルバイト列 + RSA暗号化(HMAC-SHA1(RestoreCode, Serial\|\|Challenge) + 20B OTP) | Binary (20B): ワンタイムパッドXOR済み 20B 秘密鍵 | HMAC-SHA1(RestoreCode) + RSA公開鍵 |

---

## 3. Trion Worlds および Google 時刻同期エンドポイント群

| 連携先 | エンドポイントURL | メソッド | 用途・概要 | リクエスト | レスポンス | セキュリティ・備考 |
| --- | --- | --- | --- | --- | --- | --- |
| Trion | `https://rift.trionworlds.com/external/create-device-key` | POST | 新規デバイスキー・シリアル・秘密鍵の発行 | `deviceId=<15文字乱数>` (`Uri.EscapeDataString`済) | XML `<DeviceKey>` (`DeviceId`, `SerialKey`, `SecretKey`, `ErrorCode`) | `XmlDocument.XmlResolver = null` で XXE 防止 |
| Trion | `https://rift.trionworlds.com/external/get-account-security-questions.action` | POST | リストア時のアカウント秘密の質問取得 | `username`, `password` (Form-UrlEncoded) | XML `<SecurityQuestions>` | パラメータ URL エンコード必須 |
| Trion | `https://rift.trionworlds.com/external/retrieve-device-key.action` | POST | 秘密の質問回答による既存デバイスキーの復元 | `username`, `password`, 質問回答パラメータ | XML `<DeviceKey>` | `XmlResolver = null` |
| Trion | `https://auth.trionworlds.com/time` | GET | Trion 認証サーバー時刻の取得 | なし | Text: Unix ミリ秒文字列 | エラー時は60分間再同期抑止 |
| Google | `https://www.google.com` | GET | 標準 TOTP 認証器向けの基準 UTC 時刻取得 | なし（Timeout 5,000ms） | HTTP レスポンスヘッダー `Date` (RFC 1123 UTC日時) | `\|ServerTimeDiff\| > 24h` は異常値として棄却、エラー時は5分間抑止 |
