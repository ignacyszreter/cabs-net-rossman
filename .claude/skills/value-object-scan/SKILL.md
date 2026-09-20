---
name: value-object-scan
description: Searches C# code for primitives that carry a domain concept — the same check in several places, neighbouring parameters of one type, quantities and codes in primitives — rates each candidate with the value-object checklist, gives feedback on the value objects that exist, and plans the introduction of the best candidate. Reads code and history; writes only its report.
argument-hint: '[directory with production code]'
allowed-tools: Bash, Read, Grep, Glob, Write
---

# /value-object-scan

One directory of production code, or the repository when none is given. The result is a short list
of value-object candidates with evidence, feedback on the value objects that exist, and a plan for
the best candidate. You search and read the code yourself and read the git history. You never edit a
code or test file: the only file you write is `value-object-scan.md`. Every row names its
`file:line`; every count comes from a command you ran.

## Step 1 — search

Five searches over production code, each a table in `value-object-scan.md`:

1. **The same check in several places** — conditions and guards (`if` followed by `throw`,
   `Regex.IsMatch`, `.Length`, `string.IsNullOrEmpty`, a comparison with a constant) that read the
   same once local names are ignored. For each group: the places, how many are guards, and the
   commits that touched it: `git log --oneline -S'<constant or literal>' -- <files>`.
2. **Neighbouring parameters of one primitive type** — declarations with two or more neighbours of
   `string`, `int`, `long`, `decimal`, `DateTime` and the like; then the call sites whose argument
   names match the parameter names in another order. A call in another order is a swap that
   compiled.
3. **Quantities and codes in primitives** — members whose name says they carry a unit or a format
   (price, amount, fee, rate, percent, distance, miles, min, max; license, email, phone, code,
   number, postal code, currency) held in a primitive type. For each: the files that use it, lines
   with arithmetic or comparisons, lines that divide or multiply by 100 (one number, two meanings),
   format checks, and commits: `git log --oneline -G'<Name>'`.
4. **Primitives set together** — two or more members or locals written in the same arm of an `if`
   or a `switch`, one of them a name or a code (a `string`, a literal that reads as a label) and one
   a number (`int`, `float`, `decimal`). Grep the assignment of one and read what stands next to it
   in every arm: `git grep -n '<Name> ='`. One value split across two fields reads as two ordinary
   primitives; the arms are what give it away. For each group: the arms with their lines, the places
   that read the fields apart, whether the same arms appear a second time elsewhere, and the
   commits: `git log --oneline -G'<Name>'`.
5. **Value objects that exist** — types without an `Id` that compare by value or define operations.

## Step 2 — name the candidates

Open every place. Drop a row when its places turn out to be two different rules, or when every place
sits at a boundary (a DTO, a request model, a column mapping); say why in one line.

A candidate shows one or more of five problems:

- **One rule, many copies** — a new format means editing and testing every copy. The value object's
  factory holds the rule once.
- **Nobody knows the value is valid** — every method checks it again, or trusts that someone did.
  The value object becomes the type of the member and of the parameters.
- **Swapped arguments compile** — the value object gives each neighbour its own type (or the pair
  becomes one value object, such as a range).
- **Every operation repeats the check** — the operation moves into the value object, with the check.
- **The representation leaks** — a change from cents to decimal, or an added currency, touches every
  file that reads the primitive. The value object hides it.

Rate each candidate with four questions: in how many places does it live, did it change in the
history, will it change in the coming months (a question for the team), do you run operations on
it? One place, no history, no operations and no check make a string that is fine as a string —
`FirstName` usually ends there.

Write a table of at most five candidates, best first:
`candidate | problems | places | commits | operations | will it change?`. Name each candidate with
an identifier the code already uses — a constant, a member, a parameter — and say which.

## Step 3 — feedback on the value objects that exist

For each, one line per feature, `✓` or a finding: no identity; immutable (no public setter, an
operation returns a new value); joins the attributes that belong together (an amount with its
currency); compares by value; hides its representation (count the reads of an exposed primitive in
other files); a bad value throws when the object is made; no factory that skips the check. Then
grep for the concept still travelling as a primitive inside the domain, outside the boundary.

## Step 4 — the plan for the best candidate

Steps, one commit each; each commit changes either tests or production:

1. **Safety net** — name the tests that reach every place; a place no test reaches gets a
   characterization test first.
2. **The type** — the value object with its factory, its check, equality by value and the
   operations from the evidence. Nothing uses it yet.
3. **One place at a time** — one parameter or one local changes to the new type; the compiler leads
   to the callers. Tests green after each place.
4. **The member** — the field or property type changes last; the persistence mapping (a value
   converter or an owned type) is its own step.
5. **The copies go** — every check from search 1 that the factory now holds.

Close with the **compiler probe**: one call with swapped arguments from search 2, written out. Today
it compiles. After step 3 it must fail with `CS1503`.

## Step 5 — close the file

End `value-object-scan.md` with the line that reproduces the run, `/value-object-scan <directory>`,
tell the user where the file is, and stop.

Arguments: $ARGUMENTS
