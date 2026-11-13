# 業務要件一覧

| id | category | requirement | priority | description |
| --- | --- | --- | --- | --- |
| BR-001 | 認証管理 | 時間型ワンタイムパスワードの生成 | 高 | シークレットキーと現在時刻または同期されたサーバー時刻に基づき、二要素認証で使用する時間型ワンタイムパスワード（TOTP）を生成する。GoogleAuthenticator、BattleNetAuthenticator、SteamAuthenticator、TrionAuthenticator、MicrosoftAuthenticator、OktaVerifyAuthenticator、GuildWarsAuthenticatorに対応する。 |
| BR-002 | 認証管理 | カウンター型ワンタイムパスワードの生成 | 中 | コード生成要求が発生するごとにカウンターをインクリメントし、シークレットキーとカウンター値に基づいてカウンター型ワンタイムパスワード（HOTP）を生成する。 |
| BR-003 | セキュリティ管理 | マスターパスワードによる認証情報の暗号化保護 | 高 | ユーザーが指定したマスターパスワードを使用して、登録されているシークレットキーなどの認証アプリデータを暗号化して保管する。復号時には入力されたマスターパスワードの妥当性を検証し、不一致の場合は処理を失敗させる。 |
| BR-004 | 時刻同期 | ワンタイムパスワード生成のための時間同期とドリフト検出 | 高 | 正確なワンタイムパスワード算出のため、外部サーバーと通信して端末とサーバー間の時間差（時間ドリフト）を取得・補正する。一定時間以上（24時間超など）の極端な時間ドリフトを検出した場合は無効として保護する。 |
| BR-005 | トレード管理 | Steamトレード確認および承認・拒否処理 | 高 | Steamのモバイル認証API（/IMobileAuthService/GetWGToken/v0001、/ISteamWebUserPresenceOAuth/Logon/v0001など）と連携し、ユーザーのトレード確認一覧を取得し、ユーザー操作に応じて該当トレードの承認または拒否を行う。 |
| BR-006 | ハードウェア連携 | YubiKeyを利用した認証情報保護 | 中 | USBで接続されたYubiKey（HIDDevice）を検出し、YubiKeyのハードウェアトークンを利用して暗号化シードおよびキーの保護を行う。 |
| BR-007 | 画面デザイン管理 | UIテーマおよびスタイルの統一適用 | 低 | MetroStyleManager、MetroStyleExtender、MetroToolTipコンポーネントを利用し、画面内の拡張コントロールに対してテーマ（Theme）やスタイル（Style）を一括して更新・適用する。 |