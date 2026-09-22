# AccountFlow frontend

The React client for AccountFlow. It contains the registration, verification, login, password recovery, account, and admin dashboard screens.

## Setup

Node.js 20.19 or newer is required.

```powershell
Copy-Item .env.example .env.local
npm ci
npm run dev
```

The development server runs at `http://localhost:5173`.

## Environment

```dotenv
VITE_API_BASE_URL=http://localhost:5205/api
```

`VITE_API_BASE_URL` must include the API's `/api` prefix. The value above matches the backend's `http` launch profile.

## Commands

| Command | Purpose |
| --- | --- |
| `npm run dev` | Start the Vite development server |
| `npm run lint` | Run ESLint |
| `npm run build` | Type-check and create a production build |
| `npm run preview` | Preview the production build locally |

Backend configuration, MongoDB setup, and admin account preparation are documented in the [main README](../../README.md).
