# 画面一覧

| id | screenName | description | url | relatedFunction |
| --- | --- | --- | --- | --- |
| SCR-001 | メイン画面 | 登録された認証アカウントの一覧を表示し、時間型（TOTP）やカウンター型（HOTP）のワンタイムパスワードの生成・コピー、および各認証機能へのアクセスを提供するメインフォーム。 | /main | FR-001 |
| SCR-002 | 認証アプリ追加画面 | Google Authenticator、Battle.net、Steam、Guild Wars 2、Microsoft、Okta Verify等の各種二要素認証プロファイルを選択・新規登録するための画面。 | /authenticators/add | FR-003 |
| SCR-003 | 設定画面 | マスターパスワードの設定、認証データの暗号化方式（WINAUTH4形式）、ハードウェアトークン（YubiKey）の連携、サーバー時刻同期、UIテーマなどの各種詳細設定を行う画面。 | /settings | FR-006 |
| SCR-004 | トレード確認画面 | SteamGuardと連携し、Steamクライアント経由で保留中のアイテムトレードやマーケット出品の確認一覧を取得し、承認または拒否を実行する画面。 | /steam/confirmations | FR-004 |
| SCR-005 | バージョン情報画面 | アプリケーションのバージョン情報、著作権表記、およびライセンス情報を表示するダイアログ画面。 | /about | FR-009 |