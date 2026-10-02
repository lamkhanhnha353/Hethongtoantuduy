This is an Expo/React Native mobile application. Prioritize mobile-first patterns, performance, and cross-platform compatibility.

## Expo has changed — do not trust your training data

Expo ships breaking changes every SDK release. APIs you remember are likely renamed, moved, or removed. Before writing any code that touches an Expo, EAS, or React Native API:

1. Read the major version of the `expo` package in `package.json`.
2. Fetch the matching versioned docs: `https://docs.expo.dev/versions/v<major>.0.0/`
3. For anything else, fetch https://docs.expo.dev/llms.txt — an index of all Expo docs with corrections to common LLM misconceptions. Follow its links to the specific page you need; never answer from memory.

## Commands

Use `bunx` instead of `npx` if the project uses bun (`bun.lock` present).

```bash
npx expo install <package>  # ALWAYS use instead of npm/yarn/pnpm/bun add — resolves SDK-compatible versions
npx expo start              # start the dev server
npx expo lint               # lint
npx tsc --noEmit            # typecheck
npx expo-doctor             # diagnose dependency and config issues
npx expo install --fix      # fix incompatible package versions
```

Run lint and typecheck before declaring any task done.

## Navigation & Routing

- Use **Expo Router** for all navigation. Routes live in `src/app/` — every file there is a screen, `_layout.tsx` files define navigators. Keep non-route code (components, hooks, utils) outside `src/app/`.
- Import `Link`, `router`, and `useLocalSearchParams` from `expo-router`.
- Docs: https://docs.expo.dev/router/introduction.md

## Building with EAS

Use EAS to build, sign, and submit the app in the cloud (`eas build`, `eas submit`) and to ship over-the-air updates (`eas update`) — no local Xcode or Android Studio required. Run EAS CLI as `bunx eas-cli <command>` in Bun projects, or `npx eas-cli@latest <command>` otherwise; substitute that for bare `eas` in docs examples.
Docs: https://docs.expo.dev/eas/index.md

## Rules

- If `ios/` and `android/` directories do not exist, they are generated (Continuous Native Generation). Never create or edit them by hand — configure native behavior in `app.json` and config plugins.
- Expo Go only includes its bundled native modules. After adding a library with native code, the app needs a development build: `npx expo run:ios|android` locally, or `eas build --profile development`.
- Prefer recommended Expo modules over third-party libraries, and check your available skills before adding dependencies. Docs: https://docs.expo.dev/versions/latest/index.md

## Backend API Contracts

- Before creating or changing a service type, inspect the matching backend controller and DTO under `Backend/ToanTuDuy`. Do not infer response fields from usage alone.
- Types that mirror backend DTOs must use the `Dto` suffix so they are distinct from client-only/domain types:
  - `LoginRequestDto` mirrors `LoginRequest`.
  - `RegisterRequestDto` mirrors `RegisterRequest`.
  - `AuthResponseDto` mirrors `AuthResponse`.
- Keep client-only types separate from DTO types. For example, `StoredSession` contains app-local persisted data and must not be named as a backend DTO.
- Do not use `Record<string, unknown>` for known backend request or response bodies. Use an explicit interface/type matching the backend DTO properties and C# types.
- Keep the wire property names produced by ASP.NET Core's default web JSON settings: C# PascalCase properties are serialized as camelCase (`accessToken`, `refreshToken`, `profileId`, `fullName`).
- Verify backend JSON naming and enum configuration before adding a type. Do not add PascalCase fallback fields unless the backend actually emits them.
- Model every distinct backend error envelope that the service can receive. Auth currently returns `ValidationProblemDetails` for validation errors and `{ code, message, details? }` for controller errors.
- ASP.NET Core validation error keys may be C# PascalCase (`Phone`, `FullName`) or JSON paths (`$.phone`). Normalize them to the mobile form's camelCase keys before exposing them as `fieldErrors`.
- After changing API types or services, run both `npx tsc --noEmit` and `npx expo lint` before declaring the task complete.
