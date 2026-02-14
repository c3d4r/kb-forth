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

- **Language:** GForth
- **Dependencies:** Standard ANS Forth only
- **Run:** `gforth kb.fs -e "main bye"` (exact invocation TBD)

## Why Forth

- Minimal, composable, bottom-up — a Forth word *is* a capability
- The dictionary is the API — agents extend the language itself as they build
- Tiny footprint, radical transparency — the whole system fits in your head (or context window)
- Stack-based computation may suit certain data transformation patterns

## Status

- [ ] Core: parser, serializer, internal model
- [ ] CLI: add, move, ls, board, show
- [ ] Extension: blocked status + kb blocked command
- [ ] Evaluation notes captured
