# 機能要件一覧

| id | functionName | description | relatedRequirement | priority |
| --- | --- | --- | --- | --- |
| FR-001 | 時間型ワンタイムパスワード（TOTP）生成機能 | GoogleAuthenticator等の標準規格（RFC 6238）に準拠した時間型ワンタイムパスワードを算出・表示する。 | BR-001 | 高 |
| FR-002 | カウンター型ワンタイムパスワード（HOTP）生成機能 | HOTPAuthenticatorにより、RFC 4226に基づくカウンター増加方式のワンタイムパスワードを生成する。 | BR-001 | 中 |
| FR-003 | Battle.net向け二要素認証機能 | BattleNetAuthenticatorを用いてBattle.net固有の登録仕様およびワンタイムパスワード生成を行う。 | BR-002 | 高 |
| FR-004 | SteamGuard認証およびSteamトレード確認機能 | SteamAuthenticatorおよびSteamClientを使用し、SteamGuard用コード生成とトレード確認の取得・承認・拒否（/IMobileAuthService/GetWGToken/v0001, /ISteamWebUserPresenceOAuth/Logon/v0001等）を行う。 | BR-002 | 高 |
| FR-005 | マルチサービス向け二要素認証拡張機能 | GuildWarsAuthenticator, MicrosoftAuthenticator, OktaVerifyAuthenticator, TrionAuthenticatorなどの各サービスに対応した二要素認証プロファイルを提供する。 | BR-002 | 中 |
| FR-006 | 認証データ暗号化およびマスターパスワード保護機能 | シークレットキーや認証設定情報をマスターパスワードおよびPBKDF2/AES（WINAUTH4形式）を用いて安全に暗号化・復号・保護する。 | BR-003 | 高 |
| FR-007 | ハードウェアトークン（YubiKey）連携機能 | HIDDeviceを介してUSB接続されたYubiKeyハードウェアトークンを検出し、認証データの保護およびチャレンジ・レスポンス処理を実施する。 | BR-003 | 中 |
| FR-008 | サーバー時間同期機能 | 認証局またはGoogle等のエンドポイントと通信してクライアントとサーバー間の時刻差分（ServerTimeDiff）を補正し、正確なワンタイムパスワードを生成する。 | BR-001 | 高 |
| FR-009 | MetroスタイルUIテーマ管理および描画機能 | MetroStyleManager, MetroStyleExtender, MetroToolTip等を用いて、メイン画面や設定画面のコントロールに対して統一されたMetroデザインテーマとツールチップを適用・更新する。 | BR-004 | 低 |