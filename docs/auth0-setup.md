# Auth0 configuration

Auth0 settings are supplied through local configuration or environment variables. Checked-in examples contain no tenant-specific values.

1. Create/use a free Auth0 tenant. Register an API with an Identifier of your choice and RS256 signing.
2. Set the backend `Auth0:Domain` to the tenant hostname (without `https://`) and `Auth0:Audience` to that exact Identifier. The backend needs no client secret.
3. Register a **Single Page Application** for the React frontend. Set these local URLs:
   - Allowed Callback URLs: `http://localhost:5173/app`
   - Allowed Logout URLs: `http://localhost:5173`
   - Allowed Web Origins: `http://localhost:5173`
   - Allowed Origins (CORS), if configured for the SPA: `http://localhost:5173`
4. Copy `frontend/.env.example` to `.env.local`. Set `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, and `VITE_AUTH0_AUDIENCE`. The audience must match the API. Keep `VITE_API_URL=http://localhost:5080` for local development.
5. Restart Vite. Open `/app`, sign in through Universal Login, create an organization, and edit your local profile in Settings.
6. Test invitation acceptance with another signed-in user. Auth0 returns to `/app`; the SDK restores the intended invitation route and fragment using application state.

The Auth0 React SDK uses Authorization Code with PKCE. Tokens are kept in its default memory cache. There is no local-storage token persistence, custom password form, Management API dependency, paid Auth0 Organizations feature, or development login bypass. The backend verifies issuer, audience, signature and lifetime through HTTPS discovery/JWKS. Only user access tokens for this API are accepted, not ID tokens or client-credentials machine tokens.

ClientFlow organizations and Admin/Member roles live in PostgreSQL. A verified Auth0 `sub` is the identity key under the configured issuer. The editable display name is presentation data and never grants permissions. Switching issuers/tenants requires a deliberate identity migration.

For another local port or deployed origin, update callback/logout/web-origin values, frontend build configuration, and backend CORS together. Use HTTPS for hosted environments.

References: [Auth0 React SDK](https://auth0.com/docs/libraries/auth0-react), [API access tokens](https://auth0.com/docs/secure/tokens/access-tokens/get-access-tokens), [Auth0 pricing](https://auth0.com/pricing). Verify current free-tier quotas in your tenant before deployment.

## Docker Compose

The container stack reads AUTH0_DOMAIN, AUTH0_CLIENT_ID, and AUTH0_AUDIENCE from the repository-root .env. These correspond to the frontend VITE_AUTH0 settings and backend Auth0 settings above. The default frontend URL and callback are unchanged. Rebuild the frontend image after changing Auth0 values. frontend/.env.local is excluded from Docker builds.
