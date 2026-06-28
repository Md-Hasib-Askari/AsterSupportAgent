# Aster Support Agent

An AI-powered customer support chatbot for **Aster**, a fictional online clothing store. It answers questions about shipping, returns, sizing, and payments; looks up real-looking order data; and can even book a live support call through Calendly - all by letting an LLM decide which tool to use, step by step.

The project is a **Turborepo monorepo** with two apps living side by side:

- `apps/backend` - a .NET 10 minimal API that runs the agentic loop and talks to the LLM
- `apps/frontend` - a Next.js 16 chat UI that feels polished and ships as a static site

## Why this exists

This is a portfolio piece that shows off a practical agentic pattern: the LLM doesn't just generate text - it calls tools, parses results, and decides what to do next. The backend loop (max 4 steps) gives the model room to search docs, check an order, *then* compose a final reply. The frontend even surfaces a "receipt" of which tools were used, so you can see what happened under the hood.

## Quick start

You'll need **Bun** and the **.NET 10 SDK**.

```bash
# 1. Install everything
bun install

# 2. Set up the backend config
cp apps/backend/appsettings.Development.example.json apps/backend/appsettings.Development.json
# → Populate AppSettings.Development.json with your LLM API key (Ollama or OpenRouter)

# 3. Set up the frontend env
cp apps/frontend/.env.local.example apps/frontend/.env.local

# 4. Fire it all up
bun run dev
```

The backend starts on **port 5000**, the frontend on **port 3000**. Open `http://localhost:3000` and say hello.

If you prefer to run one app at a time:

```bash
dotnet run --project apps/backend/AsterSupportAgent.csproj   # API on :5000
cd apps/frontend && bun run dev                               # UI on :3000
```

## What it can do

| You say… | The agent… |
|---|---|
| *"What's your return policy?"* | Searches the knowledge base and summarizes the relevant article |
| *"Where is order ORD-1001?"* | Looks up the order and tells you the status, tracking, and ETA |
| *"I want to talk to a real person"* | Creates a one-time Calendly booking link on the spot |

You can switch LLM providers by changing a single config value (`LLM:Provider` → `"Ollama"` or `"OpenRouter"`) - no code changes needed.

## How it's built

### Backend (`apps/backend`)

A .NET 10 minimal API with MVC controllers. The interesting bit is `AgentService`, which runs a tool-calling loop:

1. The user's message (plus conversation history) goes to the LLM with a system prompt
2. The LLM responds with a JSON action: `search_kb`, `get_order_status`, `create_booking_link`, or `respond`
3. The service executes the matching tool and feeds the result back to the LLM
4. Rinse and repeat until the LLM says `respond` (or the 4-step budget runs out)

The response includes a trace of every tool call, so the frontend can show what happened behind each reply.

Other notable pieces: keyword-overlap KB search, in-memory session store (capped at 20 messages per session), and a strategy pattern that lets you swap Ollama ↔ OpenRouter at runtime.

### Frontend (`apps/frontend`)

A Next.js 16 App Router app with a single page. The `Chat` component handles everything - message state, API calls, typing indicator, auto-scroll, and tool-call receipts. Tailwind CSS v4 does all the styling with a custom palette that feels warm and clothing-brand-appropriate.

In production it builds to a static export (`output: 'export'`), so you can throw the `dist/` folder on any CDN or static host.

### Monorepo glue

Turborepo orchestrates builds, dev servers, linting, and cleanup across both apps. Bun handles package management and scripting.

```bash
bun run build      # both apps, cached
bun run dev        # both apps, development
bun run lint       # frontend lint
bun run clean      # wipe build artifacts
```

## Project layout

```
.
├── apps/
│   ├── backend/       # .NET 10 API (Controllers, Services, Models, Data)
│   └── frontend/      # Next.js 16 chat UI (App Router, Tailwind v4)
├── package.json       # root workspace + turbo scripts
├── turbo.json         # turbo pipeline
├── bun.lock           # bun lockfile
└── AGENTS.md          # contributor quick-reference
```

## Configuration

- **Backend:** Copy `apps/backend/appsettings.Development.example.json` → `appsettings.Development.json` and fill in your keys
- **Frontend:** Copy `apps/frontend/.env.local.example` → `.env.local` (points to `http://localhost:5000` by default)

Both config files are gitignored, so your keys stay local.

## Known trade-offs

- **Session store is in-memory** - conversations vanish on restart and it won't scale horizontally. Swap for Redis in production.
- **KB search is keyword overlap** - works fine for exact matches, but an embedding-based approach would handle paraphrased queries better.
- **No auth** - the API is wide open. Lock it down before deploying.
- **Data is static JSON** - orders and articles live in files. A real store would use a database.

## License

MIT - see [LICENSE](LICENSE).
