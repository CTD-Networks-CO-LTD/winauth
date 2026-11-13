# API仕様書

| endpoint | method | description | requestBody | responseBody | auth |
| --- | --- | --- | --- | --- | --- |
| /IMobileAuthService/GetWGToken/v0001 | POST | SteamGuardおよびOAuthアクセストークンを使用して、Steam Web APIセッション用トークン（WGToken）を取得する。 | [object Object] |  | [object Object] |
| /ISteamWebUserPresenceOAuth/Logon/v0001 | POST | OAuthアクセストークンおよびWGTokenを使用してSteam Webプレゼンスへログオンし、Webセッション認証Cookieを確立する。 | [object Object] |  | [object Object] |
| /ISteamWebUserPresenceOAuth/Logoff/v0001 | POST | Steam Webプレゼンスのログインセッションを切断・ログオフし、認証状態を破棄する。 | [object Object] |  | [object Object] |