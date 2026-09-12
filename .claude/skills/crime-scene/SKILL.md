---
name: crime-scene
description: Repository history analysis in one run — most-changed files, file pairs with J, clusters of files that move as one, a Mermaid graph, and a diagnosis for every cluster the symptom catalogue has a name for.
argument-hint: '[repo path]'
allowed-tools: Bash, Read, Write
---

# /crime-scene

One command, one report. The script counts, you read. Sections 1 to 4 come from `crime-scene.sh`
and are deterministic: the same repository always gives the same bytes. Section 5 you read out of
the commits, and every row carries the evidence that lets a reader check it.

Every commit counts. The numbers are fixed in the script header: top 20 rows, a pair counts from
3 shared commits, a cluster binds from J 0.30. There is nothing to tune.

## Step 1 — run the script

The repository is the current directory, or the first argument if one is given. Find the script and
run it in one Bash call:

```bash
S=$(for c in .claude/skills/crime-scene/crime-scene.sh tools/crime-scene.sh "${CLAUDE_PLUGIN_ROOT:-/nonexistent}/skills/crime-scene/crime-scene.sh" "$HOME/.claude/skills/crime-scene/crime-scene.sh"; do [ -f "$c" ] && { echo "$c"; break; }; done)
[ -n "$S" ] || S=$(find . "$HOME/.claude" -name crime-scene.sh -path '*crime-scene*' 2>/dev/null | head -1)
"$S" <repo path or nothing> crime-scene.md; cat crime-scene.md
```

The script writes markdown: every section is a table, plus one Mermaid block. Show sections 1 to 4 to the
user exactly as printed. Do not round, shorten or reorder a number, do not turn a table back into
prose, and leave the Mermaid block as it is.

The script writes two warnings when they apply: a history under 50 commits, and a single author
over the whole range. Repeat the warning in the first sentence of your answer, because change
coupling from such a history is unreliable.

## Step 2 — read the commits

Read `symptoms.md` next to this file first. It holds the catalogue of shapes you are looking for,
each with its signature in the data and the observation that would disprove it.

Then walk the joint commits of every cluster in section 3, newest first. They are where a symptom
shows. Take the diff of eight commits per Bash call:

```bash
A=$(mktemp); printf '*.cs diff=csharp\n*.java diff=java\n*.ts diff=html\n*.py diff=python\n' > "$A"
for h in <hashes>; do git -c core.attributesfile="$A" show --stat -U2 --format='%h %ad %an: %s%n' --date=short $h | head -160; echo; done
```

The attributes file puts the method name in every hunk header, so a signal can name the method.

For every commit note two things, for yourself:

- **intent** — one of: feature · rule change · bug fix · infrastructure · housekeeping · test.
- **signal** — a symptom signature from the catalogue that this diff shows, stated as an
  observation: "sets the discount rate in `Order.CalculateTotal` and again in
  `OrderDto.SetDiscount`". No signal is a valid answer and is the common one.

These notes stay in your head. Only section 5 gets written.

## Step 3 — write section 5

Append it to `crime-scene.md` and print it. Sections 1 to 4 stay untouched.

Three rules hold. **No name from your head** — a concept is a quote from a commit message or an
identifier from a diff, and the row says which. **No number you did not see** — every count is
copied from sections 1 to 3, every hash from a joint-commit table in section 3. **No second
measurement** — a diff says *what* changed and never *how often*, so a count read off a diff is
not a count.

### 5. Diagnosis

One diagnosis per cluster that matches a row of the catalogue. Open with the summary table, then
one block per row:

| cluster | symptom | confidence | joint commits |
|---|---|---|---|
| 1 | silent drift — two copies of one rule | high | 13 |
| 2 | registration coupling | high | 12 |

"Joint commits" is copied from section 3. Each block names the symptom from the catalogue and
gives the six fields:

> **Silent drift — two copies of one rule** · cluster 1 · confidence: high
> **Evidence:** `Order.CalculateTotal` and `OrderDto.SetDiscount` change in one commit four
> times — `a1b2c3d`, `b2c3d4e`, `c3d4e5f`, `d4e5f6a` — and every message names the discount.
> **What it means:** the discount rule is implemented twice, so the two copies can disagree and
> nothing has to fail for that to happen.
> **Move:** change one copy by hand and run the test suite. Green means nothing guards the rule.
> **What would disprove it:** the suite goes red on the one-sided change. Or the two edits are
> unrelated: one renames a field, the other changes a query.

A diagnosis without the last field is an opinion. Three more rules:

- **Registration coupling is a symptom to use, not to avoid.** A cluster of `Program.cs`, the
  `DbContext` and a new type per commit is the framework at work. A run that finds a design
  problem in every cluster has found nothing.
- **Silent drift stops at the drift.** History shows two copies; it cannot show whether a test
  guards them. Report the two copies and hand over the one-command move.
- **The catalogue is the whole vocabulary.** A cluster that matches neither row gets no name and
  no block — its numbers already stand in sections 1 to 3. Confidence is low whenever the naming
  rests on diffs alone.

When no cluster matches either row, section 5 is one sentence: how many clusters you read, and that
the catalogue held no name for them. That sentence is a result, not a gap.

## Step 4 — close the file

End `crime-scene.md` with:

```
## How to reproduce

/crime-scene <repo path>

Sections 1 to 4 are deterministic: the same repository always gives these bytes.
Section 5 is a reading of the commits, and every row names the observation that would kill it.
```

Tell the user where the file is, then stop.

Arguments: $ARGUMENTS
