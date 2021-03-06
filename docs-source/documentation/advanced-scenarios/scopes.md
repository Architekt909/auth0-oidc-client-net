---
uid: scopes
---

# Using Scopes

By default the Auth0 OIDC Client will request the `openid` and `profile` scopes. You can request different scopes by setting the @Auth0.OidcClient.Auth0ClientOptions.Scope property of the @Auth0.OidcClient.Auth0ClientOptions passed to the @Auth0.OidcClient constructor.

Depending on the `scope` which was request the `id_token` will contain a different set of claims.

Note however that even if you request a limited set of scopes, e.g. `openid name`, the `User` returned in the login result may contain a broader set of claims. This is because the set of claims contained in the `User` property is also governed by the `loadProfile` parameter which can be passed to the @Auth0.OidcClient constructor.

By default `loadProfile` will be set to `true` which means the UserInfo endpoint will be called during the authorization flow. To ensure the User object contain only the claims requested in the `scope`, be sure to also set `LoadProfile` to `false`, e.g:

```csharp
var client = new Auth0Client(new Auth0ClientOptions
{
    Domain = "YOUR_AUTH0_DOMAIN",
    ClientId = "YOUR_AUTH0_CLIENT_ID",
    Scope = "openid name",
    LoadProfile = false
});
```

For more information you can read the [Auth0 Scopes documentation](https://auth0.com/docs/scopes)