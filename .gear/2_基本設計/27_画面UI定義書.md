# 画面UI定義書

```json
{
  "screens": [
    {
      "events": [
        {
          "action": "RecalculateCode",
          "trigger": "TickTimer",
          "description": "30秒間隔のインターバル経過時に時間型ワンタイムパスワードを自動再計算しMetroProgressBarを更新する"
        },
        {
          "action": "CopyToClipboard",
          "trigger": "CopyCodeButtonClick",
          "description": "表示中のワンタイムパスワードをWindowsクリップボードに格納する"
        },
        {
          "action": "PerformTimeSync",
          "trigger": "SyncTimeButtonClick",
          "description": "Googleまたは各サービスサーバーと時刻を取得比較し、時間同期オフセット（ServerTimeDiff）を更新する"
        }
      ],
      "fields": [
        {
          "name": "SearchFilterTextBox",
          "type": "text",
          "required": false,
          "validation": "最大100文字、文字列形式",
          "description": "登録済み認証アプリを名前やアカウント名で絞り込むための検索入力フィールド"
        },
        {
          "name": "CurrentCodeDisplay",
          "type": "text",
          "required": false,
          "validation": "数字5〜8桁（SteamGuadrdは英数字5桁）、読み取り専用",
          "description": "現在計算されているワンタイムパスワードの表示フィールド"
        }
      ],
      "category": "メイン業務",
      "overview": "登録されている各種二要素認証トークン（時間型ワンタイムパスワード・カウンター型ワンタイムパスワード）の一覧表示、ワンタイムパスワードの更新・コピー、時間同期、各認証アプリの管理を実行するWinAuthの中心画面。",
      "screenId": "SCR-001",
      "components": [
        {
          "name": "MetroStyleManager",
          "type": "header",
          "description": "Metro UIテーマ（Light/Dark）およびアクセントカラーの統合管理コンポーネント"
        },
        {
          "name": "AuthenticatorListPanel",
          "type": "card",
          "description": "BattleNetAuthenticator、SteamAuthenticator、GoogleAuthenticator等の各トークンカードをリスト表示する領域"
        },
        {
          "name": "MetroProgressBar",
          "type": "card",
          "description": "時間型ワンタイムパスワードの次回更新までの残り有効時間を視覚化するプログレスバー"
        },
        {
          "name": "MainToolbarButtonGroup",
          "type": "button-group",
          "description": "認証アプリ追加、トレード確認、設定、同期、更新などの主要機能へのショートカットボタン群"
        },
        {
          "name": "MetroToolTip",
          "type": "modal",
          "description": "各操作ボタンや認証コードのコピー状態をガイドするツールチップ"
        }
      ],
      "screenName": "メイン画面",
      "targetUser": "一般ユーザー / アカウント管理者",
      "transitions": [
        {
          "action": "AddButtonClick",
          "condition": "ユーザーが新規追加操作を選択した場合",
          "destination": "SCR-002"
        },
        {
          "action": "SettingsButtonClick",
          "condition": "ユーザーがオプション・設定を選択した場合",
          "destination": "SCR-003"
        },
        {
          "action": "TradeConfirmationsButtonClick",
          "condition": "SteamAuthenticatorが登録されており、トレード確認を選択した場合",
          "destination": "SCR-004"
        },
        {
          "action": "AboutButtonClick",
          "condition": "ユーザーがバージョン情報を選択した場合",
          "destination": "SCR-005"
        }
      ],
      "operationSteps": [
        {
          "step": 1,
          "action": "メイン画面起動時にマスターパスワード（またはYubiKey）による保護が有効な場合は解除認証を行う",
          "systemResponse": "暗号化されたトークンデータが復号され、登録済みAuthenticator一覧が表示される"
        },
        {
          "step": 2,
          "action": "目的のAuthenticatorカードをクリックまたはコピーボタンを押下する",
          "systemResponse": "最新のワンタイムパスワードがクリップボードにコピーされ、ツールチップで通知される"
        },
        {
          "step": 3,
          "action": "HOTPトークンの場合はカウンター更新ボタンを押下する",
          "systemResponse": "カウンターがインクリメントされ、新しいワンタイムパスワードが再計算・表示される"
        },
        {
          "step": 4,
          "action": "追加ボタンまたは右クリックメニューから各管理メニューを選択する",
          "systemResponse": "認証アプリ追加画面、設定画面、トレード確認画面またはバージョン情報画面へ遷移する"
        }
      ],
      "sourceEvidence": [
        "winauth/Authenticator/Authenticator.cs",
        "winauth/Third Party/MetroFramework/MetroFramework/Components/MetroStyleManager.cs",
        "winauth/Third Party/MetroFramework/MetroFramework/Controls/MetroProgressBar.cs"
      ]
    },
    {
      "events": [
        {
          "action": "EnrollAuthenticator",
          "trigger": "EnrollButtonClick",
          "description": "BattleNetまたはSteamの登録APIと通信し、シークレットキーと復号トークンをオンライン取得する"
        },
        {
          "action": "GenerateTestCode",
          "trigger": "VerifyButtonClick",
          "description": "入力されたシークレットキーと時間同期設定から現在のコードを計算・検証する"
        }
      ],
      "fields": [
        {
          "name": "AuthenticatorTypeCombo",
          "type": "select",
          "required": true,
          "validation": "定義された認証アプリ種別から選択必須",
          "description": "Google, Battle.net, Steam, Trion, Guild Wars, HOTP等の種別選択"
        },
        {
          "name": "NameTextBox",
          "type": "text",
          "required": true,
          "validation": "1文字以上100文字以下、空文字不可",
          "description": "認証アプリを識別するための任意の表示名"
        },
        {
          "name": "SecretKeyTextBox",
          "type": "text",
          "required": true,
          "validation": "有効なBase32文字列（RFC 3548/4648規格）または16進文字列",
          "description": "認証サーバーから提供された共有シークレットキー"
        },
        {
          "name": "DigitsComboBox",
          "type": "select",
          "required": true,
          "validation": "5, 6, 7, 8 のいずれか",
          "description": "生成するワンタイムパスワードの桁数"
        },
        {
          "name": "CounterNumericUpDown",
          "type": "number",
          "required": false,
          "validation": "0以上の整数（HOTPAuthenticator選択時のみ必須）",
          "description": "カウンター型ワンタイムパスワード用の初期カウンター値"
        }
      ],
      "category": "認証設定",
      "overview": "Battle.net、Steam、Google、Trion、Microsoft、Okta Verify、Guild Wars 2およびRFC標準（TOTP/HOTP）の新規認証アプリ情報を入力・登録し、シークレットキーの発行またはインポートを行う画面。",
      "screenId": "SCR-002",
      "components": [
        {
          "name": "AuthenticatorTypeSelector",
          "type": "form",
          "description": "サポートされている認証タイプ（BattleNetAuthenticator, SteamAuthenticator, GoogleAuthenticator, HOTPAuthenticator等）を選択するドロップダウン"
        },
        {
          "name": "AuthenticatorDetailFormPanel",
          "type": "form",
          "description": "アカウント名、シークレットキー、コード桁数、インターバルなどのパラメータ入力用パネル"
        },
        {
          "name": "EnrollActionButtonGroup",
          "type": "button-group",
          "description": "シークレットキーの自動生成/オンライン登録(Enroll)または手動設定を実行・保存する操作ボタン群"
        }
      ],
      "screenName": "認証アプリ追加画面",
      "targetUser": "一般ユーザー",
      "transitions": [
        {
          "action": "SaveSuccess",
          "condition": "登録・検証が正常に完了し設定が保存された場合",
          "destination": "SCR-001"
        },
        {
          "action": "CancelButtonClick",
          "condition": "ユーザーが登録をキャンセルした場合",
          "destination": "SCR-001"
        }
      ],
      "operationSteps": [
        {
          "step": 1,
          "action": "追加するサービスタイプ（Google, Battle.net, Steam, 標準HOTP/TOTP等）を選択する",
          "systemResponse": "選択したサービス種別に応じた固有の入力フォームおよび設定オプションが表示される"
        },
        {
          "step": 2,
          "action": "アカウント表示名およびシークレットキー（またはSteam/Battle.netの自動登録情報）を入力する",
          "systemResponse": "入力バリデーションが実行され、Base32/16進キーの形式チェックが行われる"
        },
        {
          "step": 3,
          "action": "「認証コードの確認」ボタンを押下して生成されたワンタイムパスワードをテスト確認する",
          "systemResponse": "計算された現在のワンタイムパスワードがプレビュー表示される"
        },
        {
          "step": 4,
          "action": "「OK / 保存」ボタンを押下する",
          "systemResponse": "認証アプリ情報が暗号化構成に追加保存され、メイン画面へ戻る"
        }
      ],
      "sourceEvidence": [
        "winauth/Authenticator/GoogleAuthenticator.cs",
        "winauth/Authenticator/BattleNetAuthenticator.cs",
        "winauth/Authenticator/HOTPAuthenticator.cs",
        "winauth/Authenticator/Base32.cs"
      ]
    },
    {
      "events": [
        {
          "action": "ScanHIDDevice",
          "trigger": "DetectYubiKeyButtonClick",
          "description": "HIDDeviceを列挙して挿入されているYubiKeyスロットを検索・検証する"
        },
        {
          "action": "UpdateMetroTheme",
          "trigger": "ThemeSelectionChange",
          "description": "MetroStyleManagerを通じて画面全体のカラースタイルとテーマを動的プレビュー反映する"
        }
      ],
      "fields": [
        {
          "name": "UseMasterPasswordCheckBox",
          "type": "checkbox",
          "required": false,
          "validation": "真偽値",
          "description": "設定ファイル暗号化にマスターパスワードを要求するかどうかの指定"
        },
        {
          "name": "MasterPasswordTextBox",
          "type": "password",
          "required": false,
          "validation": "UseMasterPasswordCheckBoxが有効な場合必須。最低1文字以上、推奨英数記号",
          "description": "暗号化のキー生成（PBKDF2/WINAUTH4）に使用するマスターパスワード"
        },
        {
          "name": "UseYubiKeyCheckBox",
          "type": "checkbox",
          "required": false,
          "validation": "真偽値。YubiKeyネイティブDLLおよびデバイス検出が必要",
          "description": "YubiKey（ハードウェアトークン）によるチャレンジレスポンス保護の有効化"
        },
        {
          "name": "AutoLockTimeOutNumeric",
          "type": "number",
          "required": false,
          "validation": "0〜1440分の整数（0は無効）",
          "description": "無操作時に自動で再ロックするまでの時間"
        },
        {
          "name": "MetroThemeComboBox",
          "type": "select",
          "required": true,
          "validation": "Default, Light, Dark のいずれか",
          "description": "MetroStyleExtenderおよびMetroStyleManagerで適用するテーマスタイル"
        }
      ],
      "category": "システム設定",
      "overview": "マスターパスワードの保護設定、ハードウェアトークン（YubiKey）による二重暗号化、UIテーマ（MetroStyleManager）の切り替え、自動ロックおよび時間同期オプションを設定する画面。",
      "screenId": "SCR-003",
      "components": [
        {
          "name": "SettingsTabControl",
          "type": "tabs",
          "description": "「保護・暗号化」「表示・外観」「一般設定」の各設定ページを切り替えるMetroTabControl"
        },
        {
          "name": "SecuritySettingsPanel",
          "type": "form",
          "description": "マスターパスワード設定、Windowsユーザーアカウント暗号化、YubiKeyスロット設定を行うパネル"
        },
        {
          "name": "AppearanceSettingsPanel",
          "type": "form",
          "description": "MetroThemeStyle（Dark/Light）およびアクセントカラーを選択するパネル"
        },
        {
          "name": "SettingsDialogButtonGroup",
          "type": "button-group",
          "description": "設定の保存（OK）、破棄（キャンセル）、適用ボタン"
        }
      ],
      "screenName": "設定画面",
      "targetUser": "一般ユーザー / 管理者",
      "transitions": [
        {
          "action": "SaveButtonClick",
          "condition": "設定のバリデーションが完了しデータが正常に更新された場合",
          "destination": "SCR-001"
        },
        {
          "action": "CancelButtonClick",
          "condition": "設定変更を破棄して閉じる場合",
          "destination": "SCR-001"
        }
      ],
      "operationSteps": [
        {
          "step": 1,
          "action": "設定画面のタブを選択して各種環境設定項目を表示する",
          "systemResponse": "現在の構成設定値が各入力フィールドに反映される"
        },
        {
          "step": 2,
          "action": "保護方式として「マスターパスワードを使用」を選択し、パスワードを入力する",
          "systemResponse": "パスワード強度チェックが行われ、確認再入力フィールドが有効化される"
        },
        {
          "step": 3,
          "action": "YubiKeyスロット（HIDDevice連携）またはWindowsログイン保護の併用を選択する",
          "systemResponse": "接続されているYubiKeyデバイスの検知とスロット設定状態が表示される"
        },
        {
          "step": 4,
          "action": "「OK」ボタンを押下して変更を適用する",
          "systemResponse": "構成ファイルが新方式で再暗号化され、メイン画面へ戻る"
        }
      ],
      "sourceEvidence": [
        "winauth/Authenticator/Authenticator.cs",
        "winauth/Authenticator/YubiKey.cs",
        "winauth/Authenticator/HIDDevice.cs",
        "winauth/Third Party/MetroFramework/MetroFramework/Components/MetroStyleExtender.cs"
      ]
    },
    {
      "events": [
        {
          "action": "SendAcceptConfirmation",
          "trigger": "AcceptConfirmationButtonClick",
          "description": "Steam Web APIへ選択されたトレードの承認リクエストを発行する"
        },
        {
          "action": "SendRejectConfirmation",
          "trigger": "RejectConfirmationButtonClick",
          "description": "Steam Web APIへ選択されたトレードの拒否・キャンセルリクエストを発行する"
        },
        {
          "action": "FetchConfirmationsList",
          "trigger": "RefreshConfirmationsClick",
          "description": "Steamサーバーへアクセスして最新の保留トレード確認一覧を再取得する"
        }
      ],
      "fields": [
        {
          "name": "SelectedTradeIdHidden",
          "type": "text",
          "required": true,
          "validation": "有効なSteam Confirmation ID文字列",
          "description": "操作対象として選択されたトレード確認の一意な識別子"
        },
        {
          "name": "AutoRefreshIntervalSelect",
          "type": "select",
          "required": false,
          "validation": "0（手動更新）、1分、5分、15分",
          "description": "トレード確認一覧の自動定期ポーリング間隔"
        }
      ],
      "category": "Steam連携機能",
      "overview": "SteamClientおよびSteam Web APIと連携し、モバイル二要素認証で保留されているSteamアイテムトレードおよびマーケット出品のトレード確認一覧を取得・承認・拒否する画面。",
      "screenId": "SCR-004",
      "components": [
        {
          "name": "SteamAccountSelectorHeader",
          "type": "header",
          "description": "対象となるSteamAuthenticatorアカウントの切り替えとログイン状態表示"
        },
        {
          "name": "TradeConfirmationTable",
          "type": "table",
          "description": "保留中のトレード確認項目（取引相手、取引アイテム概要、要求日時、確認ID）を一覧表示するグリッド"
        },
        {
          "name": "TradeDetailCardPanel",
          "type": "card",
          "description": "選択されたトレード確認の詳細画像および相手情報を表示する領域"
        },
        {
          "name": "ActionToolbarButtonGroup",
          "type": "button-group",
          "description": "選択したトレードの「承認（Accept）」「拒否（Reject）」「最新の一覧に更新」を実行するボタン群"
        }
      ],
      "screenName": "トレード確認画面",
      "targetUser": "Steamユーザー",
      "transitions": [
        {
          "action": "CloseWindow",
          "condition": "画面を閉じるボタンまたはEscキーが押された場合",
          "destination": "SCR-001"
        }
      ],
      "operationSteps": [
        {
          "step": 1,
          "action": "メイン画面から対象Steamアカウントの「トレード確認」を選択して開く",
          "systemResponse": "SteamClientが/IMobileAuthService/GetWGToken/v0001等を利用してセッショントークンを取得し認証を行う"
        },
        {
          "step": 2,
          "action": "保留中のトレード確認一覧がテーブルに表示される",
          "systemResponse": "各取引の取引相手アイコン、出品アイテム詳細が読み込まれる"
        },
        {
          "step": 3,
          "action": "一覧から対象トレードを選択し、「承認」または「拒否」ボタンを押下する",
          "systemResponse": "Steam Web APIへ確認・拒否リクエストを署名付きで送信する"
        },
        {
          "step": 4,
          "action": "処理結果のダイアログを確認し、一覧が自動更新される",
          "systemResponse": "完了した取引が一覧から削除され、更新された保留件数が反映される"
        }
      ],
      "sourceEvidence": [
        "winauth/Authenticator/SteamAuthenticator.cs",
        "winauth/Authenticator/SteamClient.cs"
      ]
    },
    {
      "events": [
        {
          "action": "OpenBrowserUrl",
          "trigger": "LinkClick",
          "description": "指定されたURL（GitHubリポジトリ等）を既定のブラウザで起動する"
        },
        {
          "action": "CloseDialog",
          "trigger": "CloseButtonClick",
          "description": "ダイアログフォームを閉じる"
        }
      ],
      "fields": [
        {
          "name": "VersionLabel",
          "type": "text",
          "required": false,
          "validation": "セマンティックバージョニング形式（例: 3.6.2.0）、読み取り専用",
          "description": "アセンブリバージョンおよび製品バージョン"
        },
        {
          "name": "CopyrightLabel",
          "type": "text",
          "required": false,
          "validation": "著作権表記文字列、読み取り専用",
          "description": "著作権およびライセンス所有者情報"
        }
      ],
      "category": "システム情報",
      "overview": "WinAuthのバージョン、ビルド情報、開発者クレジット、GPLオープンソースライセンス情報、および公式サイトやリポジトリへのリンクを表示するモーダルダイアログ画面。",
      "screenId": "SCR-005",
      "components": [
        {
          "name": "AboutTitleHeader",
          "type": "header",
          "description": "WinAuthのロゴアイコンと製品名、バージョン番号を表示するヘッダー部"
        },
        {
          "name": "LicenseInfoCard",
          "type": "card",
          "description": "GNU General Public License (GPL) および利用しているサードパーティ製ライブラリの著作権表示カード"
        },
        {
          "name": "ExternalLinksButtonGroup",
          "type": "button-group",
          "description": "プロジェクトGitHubページ、更新確認用リンク、ライセンス全文表示リンク"
        },
        {
          "name": "CloseMetroButton",
          "type": "button-group",
          "description": "ダイアログを閉じる「OK」ボタン（右上クローズボタン含む）"
        }
      ],
      "screenName": "バージョン情報画面",
      "targetUser": "全ユーザー",
      "transitions": [
        {
          "action": "CloseDialog",
          "condition": "OKボタン押下またはウィンドウクローズ時",
          "destination": "SCR-001"
        }
      ],
      "operationSteps": [
        {
          "step": 1,
          "action": "メイン画面の右クリックメニューまたはツールバーから「バージョン情報」を選択する",
          "systemResponse": "AboutFormがモーダルダイアログとして右上ウィンドウボタン付きで表示される"
        },
        {
          "step": 2,
          "action": "外部リンク（プロジェクトURL等）をクリックする",
          "systemResponse": "既定のWebブラウザが起動し、指定されたリポジトリページが開く"
        },
        {
          "step": 3,
          "action": "「OK」または「閉じる」ボタンを押下する",
          "systemResponse": "モーダルダイアログが破棄され、メイン画面へ制御が戻る"
        }
      ],
      "sourceEvidence": [
        "winauth/Authenticator.Tests/BugFixTests.cs",
        "winauth/Authenticator/Properties/AssemblyInfo.cs"
      ]
    }
  ]
}
```