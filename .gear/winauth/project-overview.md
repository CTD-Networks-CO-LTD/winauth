# プロジェクト概要

# プロジェクト概要

## 概要
WinAuthは、Windowsデスクトップ環境向けに開発されたオープンソースの多要素認証（2FA / 2STEP認証）管理アプリケーションです。Google Authenticator（RFC 6238 TOTP / RFC 4226 HOTP）などの標準的なワンタイムパスワードプロトコルに加え、Battle.net、Steam Guard、Guild Wars 2、Trion/Glyph、SWTOR、Okta Verifyといった主要なゲーム・WEBサービスの独自認証プロトコルを幅広くサポートしています。

アプリケーションのコアはC#および.NETフレームワーク上のWindows Formsで構築されており、MetroFrameworkを採用することでモダンで直感的なユーザーインターフェースを実現しています。セキュリティ面においては、BouncyCastleおよび.NET標準の暗号化ライブラリを活用したPBKDF2 key derivation、AES暗号化、DPAPI連携を実装し、保管される認証キーの安全性を確保しています。さらに、YubiKeyなどのハードウェアセキュリティキー連携や、Windows APIを利用した自動キー入力（Auto-Type）機能も備えています。

継続的インテグレーション・デプロイ（CI/CD）としてGitHub Actionsが導入されており、MSBuildと.NET SDKを組み合わせたクロスプラットフォームなビルドおよび自動テスト（xUnit）、x86/x64向けのアーキテクチャ別ネイティブDLL作成とリリースパッケージ化が組み込まれています。