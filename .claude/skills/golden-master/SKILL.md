---
name: golden-master
description: Plans a Golden Master for one C# method, or gives feedback on the one that exists. Reads the method's conditions and turns them into boundaries, axes and conjunction rows, finds the observation point a test controls, and reads Stryker's verdict on the net. Reads code and runs tests; writes only its report.
argument-hint: '<file.cs> <Method>'
allowed-tools: Bash, Read, Grep, Glob, Write, Skill
---

# /golden-master

One method. When no Golden Master protects it yet, the result is a plan for one. When one exists,
the result is feedback on it. You read the code yourself, run tests and run mutations. You never
edit a code or test file: the only file you write is `golden-master.md`. The people who asked
write the test.

Two rules hold for every table below. **Every row names its line** — a value without `file:line`
is a guess. **Every number comes from something you ran or read** — a count you did not see is
not a count.

## Step 1 — read the method

Read the whole method, then every condition in it: `if`, `while`, `?:`, `switch` labels, `is`
patterns, `&&` and `||` chains, `.Contains(x)` on a set. Write four tables into `golden-master.md`.

**1. Boundaries** — `line | input | test | values to cover`, one row per comparison:

| test | values to cover |
|---|---|
| `x > n` | n, n+1 |
| `x >= n` | n−1, n |
| `x < n` | n−1, n |
| `x <= n` | n, n+1 |
| `x == n`, `x != n` | n, one other value |
| `x == Enum.A` | every member the conditions name, one member they do not |
| `x == null` | null, not null |
| `set.Contains(x)` | in the set, not in the set |
| `a <= b`, two inputs | equal, one below, one above |

**2. Axes** — one row per input: the union of its values, their count, the lines. Then the product
of all counts.

**3. Conjunctions** — every group joined by `&&`: one row per operand in which that operand alone
is false and the rest are true. Without these rows, `&&` turned into `||` stays green.

**4. Inputs** — for every input, where it comes from: a parameter, a local assigned from what, a
field. Then everything the method reads from the environment: the system time zone, the current
culture, a clock read inside, an environment variable, a random value.

While you read, mark every branch that an earlier branch always catches first. Its operands can
never decide anything, so they get no row, and a mutant on them survives every grid.

## Step 2 — find the observation point

The observation point is where a test reads the result. Run the `observable-behavior-test-advisor`
skill on the file that holds the method, with `--no-questions`. It writes `observable-behaviours.md`.
Take from it the entry points, the level, the observation point and the behaviours that depend on
this method. Then walk the callers of the method up to the entry point that tests already use (grep
the test project), and note the fixtures and API clients those tests call. Write the chain:
`entry point → … → Method → where the result shows`. Note every other output the entry point shows
next to the result, and where each one comes from — a second copy of the rule often sits there.

For every input from table 4, write how a test sets it through that entry point. An input no test
can set gets no axis: list it under **unreachable from this height** with the seam it needs.

When the result of the method reaches no entry point, say so and stop.

## Step 3 — look for the Golden Master that exists

Grep the test project for tests that reach the entry point and assert with a recording:
`Verify(`, `Combination()`, `*.verified.*`, `Approvals.`. One of them reaching the method means
step 5. None means step 4.

## Step 4 — the plan

Append:

1. **Observation point** — the chain, and the row: every output the point shows about the result,
   in invariant culture and a fixed order, e.g. `4200 | Gold | 15 %`. A Golden Master sees what it
   records and nothing else.
2. **Axes** — `input | how the test sets it | values`, each value labelled with its reason:
   `Fri 2024-01-12 · weekend sale starts`. Inputs from one source merge: hour and weekday both
   come from the order time, so the axis is days × hours.
3. **Conjunction rows** — each row of table 3 as a concrete input, or `none` with the branch that
   makes it impossible.
4. **Before the grid** — each unreachable input with its seam, and each environment read with what
   to pin. A seam is a production change and goes in its own commit before the Golden Master.
5. **Size** — the rows of the grid. Above about 200, cut values that repeat a boundary; never cut
   a conjunction row.
6. **The recorder** — [Verify](https://github.com/VerifyTests/Verify) holds the recording:
   `Verify.NUnit` or `Verify.Xunit`, whichever the test project runs, and
   `using static VerifyNUnit.Verifier;`. The grid is one call — `Combination().Verify(Row, Axis1, Axis2)` —
   where `Row` takes one value per axis and returns the row of point 1 as a single string. Give
   every axis value a record whose `ToString()` is its label, so the `*.verified.txt` file reads as
   the grid itself. When the project already records with another tool, keep that one and say so.
7. **Rules for the author** — no expected value typed or computed in the test; the first run
   writes `*.received.txt`, a person reads it and only then accepts it; the commit holds test
   files only. Name the rows where two outputs of the point should disagree, as questions for the
   business.
8. **How you will know it holds** — flip one boundary by hand: the test goes red and the diff names
   its rows. Then the mutation run from step 5, with your prediction of which mutants survive.

## Step 5 — the feedback

Compare the test with your tables, one line per check, each a ✓ or a finding with `file:line`:

1. boundary values that no axis visits;
2. conjunction rows that no row gives — read the labels in the `*.verified.*` file;
3. outputs of the observation point the row leaves out;
4. expected values typed or computed in the test;
5. the `*.verified.*` file missing or uncommitted, and rows in it where two columns disagree;
6. environment reads the test run does not pin.

Then measure. Run the test with the environment pinned and `DiffEngine_Disabled=true`: it must be
green. Run Stryker on the method's file from the test project directory, with the JSON reporter:

```bash
dotnet stryker -p <Production>.csproj -m '<path of the file inside the production project>' -r json -r cleartext
```

When `StrykerOutput/` is tracked in the repository, add `--output <a directory outside it>`. Read
`reports/mutation-report.json` for the method's lines: count killed, survived, timeout and no
coverage, and give the score. Give every surviving or uncovered mutant one sentence and one class:

- **dead branch** — an earlier branch catches the value first; no input reaches this one.
- **missing row** — an input the grid does not visit; name the row.
- **unreachable from this height** — the entry point cannot set the input; name the seam.
- **outside the method** — the mutant sits in code this grid does not protect.
- **effect outside the observation point** — the change shows only in something the row does not
  record; name the observation point that would see it.

Close with **next three moves**: the rows or seams that kill the most mutants, first.

## Step 6 — close the file

End `golden-master.md` with the line that reproduces the run, `/golden-master <file.cs> <Method>`,
tell the user where the file is, and stop.

Arguments: $ARGUMENTS
