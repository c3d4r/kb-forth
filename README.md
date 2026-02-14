# kb-forth

GForth implementation of the `kb` kanban tool.

**Part of the [Language Choice as Superpower](https://github.com/cedar/language_choice_as_superpower) research spike.**

## What This Is

A text-first, CLI kanban board tool. Same spec implemented in 6 languages to evaluate which languages give AI agents the best leverage. Forth tests the hypothesis that a minimal, stack-based, extensible language gives agents radical composability.

## The Spec

See [SPEC.md](./SPEC.md) for the full specification. Key points:

- **Text-first:** The data file is the source of truth — human-readable, git-diffable, agent-friendly
- **CLI interface:** `kb add`, `kb move`, `kb ls`, `kb board`, `kb show`, etc.
- **Methodology-agnostic:** Lanes and flow, not Scrum opinions
- **Format freedom:** If Forth suggests a more natural data format (stack-oriented DSL?), propose it
- **Extension exercise:** After core works, add `blocked` status (auto-derived from deps) and `kb blocked` command

## Runtime

- **Language:** GForth 0.7.3
- **Dependencies:** Standard ANS Forth only (no external libraries)
- **Run:** `./kb <command>` or `gforth kb.fs -e "main bye" -- <command>`

## Usage

```bash
# Add items
./kb add task "Implement feature"
./kb add bug "Fix login issue"
./kb add spike "Research options"

# Move through workflow
./kb move KAN-001 doing
./kb move KAN-001 review
./kb move KAN-001 done

# View items
./kb ls                    # List all items
./kb ls --lane=backlog     # Filter by lane
./kb show KAN-001          # Show item details
./kb board                 # Visual board view
./kb blocked               # Show blocked items
```

## Data Format

Human-readable text file (`kb.dat`):

```
# Kanban data file
board: default
lanes: backlog doing review done

item[ KAN-001
  type: task
  title: Implement feature
  status: doing
  tags: backend urgent
  deps: KAN-002
  created: 2025-02-14
]item
```

## Why Forth

- Minimal, composable, bottom-up — a Forth word *is* a capability
- The dictionary is the API — agents extend the language itself as they build
- Tiny footprint, radical transparency — the whole system fits in your head (or context window)
- Stack-based computation may suit certain data transformation patterns

## Implementation Notes

- **988 lines** of GForth code
- **100 word definitions** (functions)
- Key architectural decision: Using variables instead of return stack for state in loops (Forth's `?do...loop` uses the return stack, conflicting with `r@`)

## Status

- [x] Core: parser, serializer, internal model
- [x] CLI: add, move, ls, board, show
- [x] Extension: blocked status + kb blocked command
- [x] Evaluation notes captured
