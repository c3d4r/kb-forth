# kb-forth — Evaluation

## Quantitative

| Metric | Value |
|--------|-------|
| Lines of code | 988 |
| Token economy (solution) | ~15K tokens (estimated from file size) |
| Token economy (process) | High — significant debugging required |
| Abstraction density | 100 word definitions / 988 LOC = 10.1% |
| Iteration count | ~25 prompt-response rounds |
| Extension cost (tokens) | Low — blocked feature was ~50 LOC |
| Extension cost (LOC) | ~50 lines for blocked status + command |
| Error recovery cost | High — many stack manipulation bugs |

## Qualitative

| Dimension | Notes |
|-----------|-------|
| Naturalness of fit | **Mixed.** Text parsing is natural (word-by-word). Data structures are awkward (manual offset calculations, no records). |
| Composability | **Good in theory, tricky in practice.** Words compose well, but stack effects must be mentally tracked. Return stack conflicts with loops were a recurring issue. |
| Readability (agent) | **Moderate.** Stack comments help but are easy to get wrong. Debugging required tracing stack state manually. |
| Readability (human) | **Low-moderate.** Forth is unfamiliar to most. Stack manipulation reads backwards. Comments essential. |
| Format expressiveness | **Good.** The `item[ ... ]item` bracketed format feels natural for Forth's word-oriented parsing. |
| Surprise factor | **Negative surprise:** Return stack (`r@`) conflicts with `?do...loop` caused multiple bugs. Had to refactor to use variables. |
| Library pain | **Minimal.** Standard ANS Forth sufficed. `allocate`/`free` for dynamic strings worked fine. |
| Self-extension quality | **Good.** Adding `blocked` command reused existing words (`item-blocked?`, `dep-done?`, `find-item-by-id`). |

## Agent-readiness

| Dimension | Notes |
|-----------|-------|
| Context pressure | **Moderate.** 988 LOC fits easily in context. Stack effects require careful tracking across the codebase. |
| Toolability | **Good.** Words are natural tool boundaries. Each command is a self-contained word. |
| Inspectability | **Excellent.** Can interactively test any word. `.s` shows stack. Transparent execution model. |
| Grammar-in-context cost | **High.** Forth's postfix notation, stack manipulation words (`dup`, `swap`, `rot`, `-rot`, `2dup`, `2swap`, `>r`, `r@`, `r>`), and loop constructs require significant context to use correctly. |

## Key Bugs Encountered

1. **`str-equal?` / `str-prefix?`** — Stack manipulation wrong, comparing wrong values
2. **`lane-add`** — Stored address/length in reversed order
3. **`generate-id`** — Length calculation used wrong pictured numeric format
4. **`parse-word-from`** — Subtraction operand order reversed (a-b vs b-a)
5. **`item-add-tag` / `item-add-dep`** — Stack reordering before `item-tag!`/`item-dep!` was wrong
6. **Multiple functions** — Used `r@` inside `?do...loop`, but loops use return stack, corrupting the value

## Lessons Learned

- **Return stack is shared infrastructure.** Don't use `>r`/`r@`/`r>` when `?do...loop` is active.
- **Stack comments are essential but error-prone.** Easy to write incorrect stack effects.
- **Test incrementally.** Forth's interactive nature allows testing each word, which caught bugs early.
- **Postfix is powerful but demanding.** Requires different mental model than most languages.

## Session Log

Single Claude Code session. Key phases:
1. Initial code review — found existing skeleton with bugs
2. Bug fixing — `str-equal?`, `str-prefix?`, `lane-add`, `generate-id`
3. Return stack refactoring — changed `r@` to variables in `save-data`, `save-item`, `cmd-show`, `cmd-board`
4. Stack ordering fixes — `item-add-tag`, `item-add-dep`, `parse-word-from`
5. Final testing and wrapper script
