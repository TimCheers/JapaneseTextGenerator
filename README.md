# JapaneseTextGenerator

Kotoba is a Japanese-learning app that turns your own vocabulary decks into short, AI-generated practice texts. You build decks of words (Anki-style), and the app writes new Japanese reading material using only the words you're actually learning.

## Features

- **User accounts** — register/login, all decks and texts are scoped to your account.
- **Decks & words** — create decks, add words manually, or bulk-import them from an Excel file (`word / reading / meaning / example` columns).
- **AI text generation** — generates a short Japanese-only text from the words in your deck, using a pluggable AI provider (currently Gemini; OpenAI is supported through the same interface).
- **Infinite feed** — generated texts appear in a scrollable, snap-to-card feed on the home page; scrolling to the last text automatically requests a new one, with a loading indicator while it's generating.

## Tech stack

**Backend:** ASP.NET Core Web API, Entity Framework Core, PostgreSQL
**Frontend:** React, TypeScript, Vite

## Project structure

```
backend/KotobaApi/     ASP.NET Core Web API (controllers, services, EF Core models)
frontend/              React + TypeScript client
```

## Running locally

**Backend**

```
cd backend/KotobaApi
dotnet user-secrets set "Gemini:ApiKey" "<your-key>"
dotnet run
```

The API runs on `http://localhost:5166` by default.

**Frontend**

```
cd frontend
npm install
npm run dev
```

## AI provider

The app talks to the AI provider through a single `IAiTextGenerationService` interface, so switching providers (Gemini ↔ OpenAI) only requires changing the DI registration in `Program.cs` and the corresponding config section in `appsettings.json` — no other code changes needed.

## Status

Work in progress — built as a portfolio project.
