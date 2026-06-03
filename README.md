# Cultivation RPG

[中文文档](./README_CN.md)

Keyboard & mouse idle cultivation game. Type to gain Spirit, click to Alchemy, move for Agility, stay active for Wisdom. Online leaderboard.

## Features

- Global keyboard/mouse hooks, silent background tracking
- 8 cultivation realms (Qi Refining → Ascension) with progress bar
- 4 stats (Spirit/Agility/Wisdom/Alchemy) + total XP
- Today's cultivation summary
- Online leaderboard (Supabase, syncs every 10s)
- Rebirth — reset and start over
- System tray — runs in background

## Quick Start

```bash
cd CultivationRPG
dotnet run
```

## Tech Stack

C# .NET 9 + WPF · SQLite · Supabase · LiveCharts2 · Win32 Hooks

## Realms

| Realm | XP |
|------|-----|
| Qi Refining | 0 |
| Foundation | 10K |
| Golden Core | 50K |
| Nascent Soul | 200K |
| Spirit Severing | 1M |
| Tribulation | 5M |
| Mahayana | 20M |
| Ascension | 100M |

## Stats

| Stat | Source | Ratio |
|------|--------|-------|
| Spirit | Keystrokes | 1:1 |
| Agility | Mouse (px) | /1000 |
| Alchemy | Clicks | 1:1 |
| Wisdom | Active (min) | 1:1 |

XP = sum of all four.
