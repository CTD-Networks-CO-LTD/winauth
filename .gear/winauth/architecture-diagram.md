# アーキテクチャ構成図

WinAuthはWindows上で動作する各種二要素認証（2FA）・ワンタイムパスワード生成デスクトップアプリケーションです。Steam、Battle.net、Google等の認証プロトコル対応に加え、YubiKeyハードウェアトークンやキーストローク自動入力（InputSimulator）、MetroFrameworkによるモダンUIを備えています。

**プレゼンテーション層:**
- メイン画面 [Windows Forms (.NET)]
- 設定画面・認証アプリ追加画面・トレード確認画面 [Windows Forms (.NET)]
- バージョン情報画面 [Windows Forms (.NET)]
- MetroStyleManager / MetroStyleExtender / MetroToolTip [MetroFramework]

**アプリケーション・認証コア層:**
- Authenticator (基底コア) [C# (.NET), BouncyCastle]
- TOTP / HOTP 認証エンジン [GoogleAuthenticator, HOTPAuthenticator, MicrosoftAuthenticator, OktaVerifyAuthenticator, GuildWarsAuthenticator]
- 専用認証エンジン (BattleNet / Trion) [BattleNetAuthenticator, TrionAuthenticator]
- SteamAuthenticator / SteamClient [SteamAuthenticator, SteamClient, Newtonsoft.Json]
- InputSimulator (キーストローク・マウス自動入力) [User32 SendInput (P/Invoke)]

**ハードウェア・暗号化データ層:**
- YubiKey / HIDDevice [WinAuth.YubiKey Native C++ DLL (x86/x64), HID API]
- 暗号化構成ファイル (マスターパスワード保護) [PBKDF2, AES-256 (BouncyCastle / DPAPI), WINAUTH4 XML]

**外部サービス層:**
- Steam Web API (/IMobileAuthService/GetWGToken/v0001, /ISteamWebUserPresenceOAuth/Logon/v0001, Logoff) [HTTPS REST API (api.steampowered.com)]
- Battle.net 認証・時刻サーバー [HTTPS]
- 時間同期サーバー [HTTPS (Google / NTP)]

**接続:**
- メイン画面 → MetroStyleManager / MetroStyleExtender / MetroToolTip (Internal Call)
- 設定画面・認証アプリ追加画面・トレード確認画面 → MetroStyleManager / MetroStyleExtender / MetroToolTip (Internal Call)
- バージョン情報画面 → MetroStyleManager / MetroStyleExtender / MetroToolTip (Internal Call)
- メイン画面 → Authenticator (基底コア) (Method Call)
- 設定画面・認証アプリ追加画面・トレード確認画面 → Authenticator (基底コア) (Method Call)
- TOTP / HOTP 認証エンジン → Authenticator (基底コア) (Inheritance)
- 専用認証エンジン (BattleNet / Trion) → Authenticator (基底コア) (Inheritance)
- SteamAuthenticator / SteamClient → Authenticator (基底コア) (Inheritance)
- メイン画面 → InputSimulator (キーストローク・マウス自動入力) (Method Call)
- Authenticator (基底コア) → 暗号化構成ファイル (マスターパスワード保護) (File I/O)
- Authenticator (基底コア) → YubiKey / HIDDevice (P/Invoke)
- SteamAuthenticator / SteamClient → Steam Web API (/IMobileAuthService/GetWGToken/v0001, /ISteamWebUserPresenceOAuth/Logon/v0001, Logoff) (HTTPS)
- 専用認証エンジン (BattleNet / Trion) → Battle.net 認証・時刻サーバー (HTTPS)
- TOTP / HOTP 認証エンジン → 時間同期サーバー (HTTPS)

```mermaid
flowchart TD
    subgraph presentation["プレゼンテーション層"]
        main_ui["メイン画面 (Windows Forms (.NET))"]
        settings_ui["設定画面・認証アプリ追加画面・トレード確認画面 (Windows Forms (.NET))"]
        about_ui["バージョン情報画面 (Windows Forms (.NET))"]
        metro_framework["MetroStyleManager / MetroStyleExtender / MetroToolTip (MetroFramework)"]
    end
    subgraph application["アプリケーション・認証コア層"]
        authenticator_base["Authenticator (基底コア) (C# (.NET), BouncyCastle)"]
        totp_hotp_authenticators["TOTP / HOTP 認証エンジン (GoogleAuthenticator, HOTPAuthenticator, MicrosoftAuthenticator, OktaVerifyAuthenticator, GuildWarsAuthenticator)"]
        specialized_authenticators["専用認証エンジン (BattleNet / Trion) (BattleNetAuthenticator, TrionAuthenticator)"]
        steam_client_module["SteamAuthenticator / SteamClient (SteamAuthenticator, SteamClient, Newtonsoft.Json)"]
        input_simulator["InputSimulator (キーストローク・マウス自動入力) (User32 SendInput (P/Invoke))"]
    end
    subgraph hardware_security["ハードウェア・暗号化データ層"]
        yubikey_module["YubiKey / HIDDevice (WinAuth.YubiKey Native C++ DLL (x86/x64), HID API)"]
        encrypted_storage["暗号化構成ファイル (マスターパスワード保護) (PBKDF2, AES-256 (BouncyCastle / DPAPI), WINAUTH4 XML)"]
    end
    subgraph external["外部サービス層"]
        steam_web_api["Steam Web API (/IMobileAuthService/GetWGToken/v0001, /ISteamWebUserPresenceOAuth/Logon/v0001, Logoff) (HTTPS REST API (api.steampowered.com))"]
        battlenet_api["Battle.net 認証・時刻サーバー (HTTPS)"]
        time_sync_service["時間同期サーバー (HTTPS (Google / NTP))"]
    end
    main_ui -->|"Internal Call"|metro_framework
    settings_ui -->|"Internal Call"|metro_framework
    about_ui -->|"Internal Call"|metro_framework
    main_ui -->|"Method Call"|authenticator_base
    settings_ui -->|"Method Call"|authenticator_base
    totp_hotp_authenticators -->|"Inheritance"|authenticator_base
    specialized_authenticators -->|"Inheritance"|authenticator_base
    steam_client_module -->|"Inheritance"|authenticator_base
    main_ui -->|"Method Call"|input_simulator
    authenticator_base -->|"File I/O"|encrypted_storage
    authenticator_base -->|"P/Invoke"|yubikey_module
    steam_client_module -->|"HTTPS"|steam_web_api
    specialized_authenticators -->|"HTTPS"|battlenet_api
    totp_hotp_authenticators -->|"HTTPS"|time_sync_service
```