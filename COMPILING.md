# WinAuth のビルド手順

このドキュメントでは、ソースコードから WinAuth をビルド・テスト・パッケージングする手順を説明します。

## 前提条件

### 必須要件
* **OS**: Windows 10 / Windows 11（64ビット環境推奨）
* **.NET SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（`10.0.x`）

### オプション要件
* **Visual Studio で開発・ビルドする場合**:
  * Visual Studio 2022 以降（「.NET デスクトップ開発」ワークロードを含む）
* **YubiKey 用ネイティブ DLL（`WinAuth.YubiKey`）もビルドする場合**:
  * Visual Studio（または Build Tools for Visual Studio）の「C++ によるデスクトップ開発」ワークロード
  * Windows 10 / 11 SDK（ターゲット プラットフォーム バージョン `10.0`）

> **補足**: YubiKey 用ネイティブ DLL をビルドしなくても、C# ソリューション（`WinAuth.sln`）単体でのビルド・通常機能の実行・単体テストの実行が可能です。

---

## ビルド前の注意事項

開発中やデバッグ実行時に既存の認証器データが意図せず変更されるのを防ぐため、作業前に `%AppData%\WinAuth\winauth.xml` をバックアップするか、WinAuth のエクスポート機能でデータを退避しておくことを推奨します。

---

## ソリューション構成

ルートディレクトリの `WinAuth.sln` で全 C# プロジェクトを管理しています。

* `WinAuth/WinAuth.csproj` — メインの Windows Forms アプリケーション（`net10.0-windows`）
* `Authenticator/Authenticator.csproj` — 認証器および暗号化（WINAUTH4 / WINAUTH3）コアライブラリ（`net10.0-windows`）
* `Authenticator.Tests/Authenticator.Tests.csproj` — xUnit 単体テストプロジェクト（`net10.0-windows`）
* `Third Party/` — 同梱サードパーティライブラリ（`InputSimulator`, `MetroFramework`, `MetroFramework.Fonts`）
* `WinAuth.YubiKey/` — YubiKey 連携用のネイティブ C プロジェクト（`WinAuth.YubiKey.x86.vcxproj` / `WinAuth.YubiKey.x64.vcxproj`）

---

## 1. コマンドライン（.NET CLI）でのビルドとテスト

### (任意) YubiKey ネイティブ DLL のビルド
YubiKey 連携機能を組み込む場合は、C# プロジェクトのビルド前に MSBuild でネイティブ DLL（x86 / x64）をビルドします。出力された DLL は `Authenticator/Resources/` に配置され、`Authenticator` アセンブリのリソースとして自動的に埋め込まれます。

```powershell
msbuild "WinAuth.YubiKey/WinAuth.YubiKey.x86.vcxproj" /p:Configuration=Release /p:Platform=Win32
msbuild "WinAuth.YubiKey/WinAuth.YubiKey.x64.vcxproj" /p:Configuration=Release /p:Platform=x64
```

### 復元・ビルド・単体テストの実行

```powershell
# NuGet パッケージの復元
dotnet restore WinAuth.sln

# Release 構成でソリューション全体をビルド
dotnet build WinAuth.sln -c Release --no-restore

# 単体テストの実行
dotnet test WinAuth.sln -c Release --no-build
```

ビルドが成功すると、`WinAuth\bin\Release\net10.0-windows\WinAuth.exe`（Debug 構成の場合は `WinAuth\bin\Debug\net10.0-windows\WinAuth.exe`）が生成されます。

---

## 2. 配布用の単一実行ファイル（Single-File）の発行

現行バージョンでは旧来の `ILMerge` は使用せず、.NET 標準の単一ファイル発行機能（`PublishSingleFile`）を使用して 1 つの `WinAuth.exe` を生成します（GitHub Actions の `.github/workflows/release.yml` と同等の手順です）。

```powershell
dotnet publish "WinAuth/WinAuth.csproj" `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=none `
  -p:DebugSymbols=false `
  -o "./publish"
```

成功すると、`./publish/WinAuth.exe` に配布可能な単一実行ファイルが出力されます（実行にはターゲット環境に .NET 10 Desktop Runtime が必要です。ランタイムごと同梱する場合は `--self-contained true` を指定してください）。

> **補足（ランタイム探索設定 `AppHostDotNetSearch`）**:
> `WinAuth/WinAuth.csproj` では `<AppHostDotNetSearch>AppLocal;Global</AppHostDotNetSearch>` を設定しているため、発行された単一 `WinAuth.exe` の `apphost` は実行ファイルと同階層（`AppLocal`）およびシステム標準のインストール先（`Global`: `C:\Program Files\dotnet\` 等）のみからランタイムを探索します。これにより、利用者の端末に不完全な `DOTNET_ROOT` / `DOTNET_ROOT_X64` 環境変数が残存していても干渉を受けず、公式インストーラーで導入された .NET 10 Desktop Runtime を確実に認識して起動します。

---

## 3. Visual Studio でのビルド

1. リポジトリ直下の `WinAuth.sln` を Visual Studio で開きます。
2. ビルド構成（`Debug` または `Release`）を選択します。
3. メニューの **［ビルド］＞［ソリューションのビルド］** を実行します。
4. 出力先 `WinAuth\bin\<Debug|Release>\net10.0-windows\WinAuth.exe` に実行ファイルが生成されます。

