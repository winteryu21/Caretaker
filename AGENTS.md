# AGENTS.md

Time-causality-based 2-player asymmetric co-op puzzle adventure. Unity 6 (URP 2D), C# 12, Netcode for GameObjects.

## Directory

```text
Caretaker/
├── AGENTS.md
├── README.md
├── CONTRIBUTING.md
├── CONTEXT.md                — domain terms & design principles
├── docs/
│   ├── design/               — DRD, DSD, game-design
│   ├── technical/            — architecture, coding-standards
│   ├── sprints/              — sprint plans & issues
│   └── archive/              — read-only records
└── Assets/_Project/
    ├── Scripts/
    │   ├── Core/             — game flow, session, checkpoint
    │   ├── World/            — rooms, causality, object interactions
    │   ├── Gameplay/         — player, AI, inventory, radio
    │   ├── Presentation/     — camera, HUD, minimap, split-view
    │   └── Shared/           — shared types, IDs, utilities
    ├── Data/                 — ScriptableObject assets
    ├── Prefabs/
    ├── Scenes/
    ├── Art/
    ├── Audio/
    └── Tests/
```

Create folders only when files exist. Details → [`architecture.md`](docs/technical/architecture.md)

## Code Style

Follow [`coding-standards.md`](docs/technical/coding-standards.md) in full. Key rules:

- `PascalCase` classes/methods, `_camelCase` private fields, `UPPER_SNAKE` constants.
- `[SerializeField] private`. No `public` fields.
- `GetComponent<T>()` → cache in `Awake()`. Never call in `Update()`.
- Subscribe in `OnEnable()`, unsubscribe in `OnDisable()`.
- XML doc comments on public API. Inline comments explain 'why' only.
- No `FindObjectOfType`, `SendMessage`, or `new` allocations in `Update()`.

## Git Workflow

Follow [`CONTRIBUTING.md`](CONTRIBUTING.md) in full. Key rules:

- Branch from `develop`: `{type}/{kebab-case}`. Never push `main` directly.
- Commits: `{type}({scope}): DEV-{N} description` — [Conventional Commits](https://www.conventionalcommits.org/).
- PR: base = `develop`. Body must include `Closes CARE-{N}`.
