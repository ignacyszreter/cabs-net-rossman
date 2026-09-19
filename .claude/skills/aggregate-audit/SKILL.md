---
name: aggregate-audit
description: Audits one C# class that holds business state — who writes it from outside, where the checks live, the same information twice, concurrency token, uniqueness, raw SQL — and plans how it becomes an aggregate, or gives feedback on the aggregate it already is. Reads code and runs tests; writes only its report.
argument-hint: '<Class.cs>'
allowed-tools: Bash, Read, Grep, Glob, Write
---

# /aggregate-audit

One class. Run it before the class is an aggregate, and the result is a plan: which rules move in,
which setters close, what the database must guard, where the boundary may run. Run it after, and the
result is feedback on what still leaks. You read the code yourself and run tests. You never edit a
code or test file: the only file you write is `aggregate-audit.md`. Every finding names its
`file:line`.

## Step 1 — read the class and everyone who touches it

Find every file that mentions the class in any form: the type, a `var` named after it, its
repository, its controller (grep without case). Write:

1. **State** — every property, the base class included: type, setter, shape (mutable collection,
   read-only collection, counter, limit, computed, plain value).
2. **Writes from outside** — assignments, `++` and `--`, object initializers and collection
   mutations in other files, whose receiver is this class. A `.Status` on another type is not a
   write to this class.
3. **Load, check, set** — production methods outside the class that load an instance, check its
   properties and set its properties.
4. **Write map** — `property | written inside by | written outside by | read in conditions outside by`.
5. **Persistence** — the mapping (`Entity<T>`, `IEntityTypeConfiguration<T>`, helper methods called
   there, the base class mapping); the version property; the concurrency token (`IsConcurrencyToken`,
   `IsRowVersion`, `[Timestamp]`, `[ConcurrencyCheck]`) and the code that raises it; unique indexes;
   every *check, then insert* — a lookup by key, a null test, a create — and whether a unique index
   guards that key; raw SQL that writes the table.

## Step 2 — the findings

Seven problems; for each, the evidence, or `✓` and why not:

1. **The rule lives far from the data** — a service loads, checks and sets. Move: a method with
   intent on the class that checks and changes in one place.
2. **The state changes around the rule** — public setters, a mutable collection, initializers, raw
   SQL. Move: private setters, a read-only collection, a constructor or factory.
3. **The same information in two places** — a counter next to the collection it counts. Move:
   compute it.
4. **Two people race for one invariant** — a check, and no concurrency token that every change
   raises. Move: a version on the root as the token.
5. **Independent changes wait for each other** — properties written by different actors at very
   different rates (an administrator once a quarter, a login hundreds of times a day) under one
   version. Move: the frequent data to its own record, referenced by identity.
6. **The rule reads data outside its boundary** — a method takes as a parameter a value another
   record owns and checks it. Move: the data the invariant reads inside one boundary.
7. **A rule over the whole set** — *check, then insert* with no unique index. Move: a unique index;
   when the set fits inside one aggregate, the aggregate checks it.

## Step 3 — the rules

Every rule you found, as `rule | kind | lives today | belongs in`. Kinds: *validation* (the shape of
one input — at the entrance, in a value object), *invariant* (the state stays valid during a change —
in the aggregate), *calculation* (how a value is computed — next to the aggregate), *process* (the
order of steps over time — in a service).

## Step 4 — the methods with intent

For every load-check-set method: the method the class gets, its signature, the checks that move
inside, what stays in the service. Name it by three questions: how does the business name this
operation (`Add(line, when)`, not `AddLineAndIncrementCounter`)? Does the name give away the
implementation? Could someone invent a different implementation from the name?

## Step 5 — the boundary

Group the properties by who writes them. How often each group changes is for the team to say;
write it as a question. For every pair of groups that one invariant reads, or that different actors
write, ask: must these changes happen together, all or nothing? What happens when one of them
happens 30 seconds later? Who loses, and how much? Offer a split only as an option with its price.

## Step 6 — the steps

One commit each; each commit changes either tests or production:

1. **Safety net** — the tests that reach the class; a use case without one gets a test first.
2. **Methods with intent** — one per commit, the service calls it.
3. **Close the state** — setters private, the collection read-only, the counter computed. The team
   checks the audit first: one setter made `private set;` locally and a build — the error count
   must match the writes from outside for that property.
4. **Race tests** — two contexts load the same object, both change it, both save. Tests only; red.
5. **Concurrency token** — production only; the race tests go green. The refactoring and the fix
   stay two commits.
6. **Unique index** for every *check, then insert* without one.
7. **Split** — only when the team's answers in step 5 ask for it.

When the class already is an aggregate, the findings say what is left, and the steps list only that.

## Step 7 — close the file

End `aggregate-audit.md` with the line that reproduces the run, `/aggregate-audit <Class.cs>`, tell
the user where the file is, and stop.

Arguments: $ARGUMENTS
