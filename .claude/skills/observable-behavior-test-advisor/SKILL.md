---
name: observable-behavior-test-advisor
description: Reads production code and names the behaviours an outside observer can see — the entry points, the level and the point where a test reads the result, the behaviours in business language, and the boundary values where behaviour changes. The report is a map of what is observable. Invoke when the user asks "what is observable here", "what does this code do", "what should I assert on", "where can a test see this".
argument-hint: "[path to production code file/directory] [--no-questions | --questions-at-end]"
---

# Observable Behavior Test Advisor

Reads production code and reports what an outside observer sees when the code runs: the ways into
the module, the point where the result shows, the behaviours in business language, and the values
at which behaviour changes.

Core question: **what can the user or the system do now that it could not do before, and where
does that show?**

The report ends with that map. The reader decides what to do with it.

## Interaction modes

| Flag | Mode | Behaviour |
|------|------|-----------|
| *(no flag)* | **Interactive** (default) | Ask at each point marked "Ask the user" through `AskUserQuestion`, wait for the answer, continue. |
| `--questions-at-end` | **Batch** | Collect the questions as Q1, Q2, Q3… Continue with your best reading. Present them all at the end in one `AskUserQuestion`, then correct the report. |
| `--no-questions` | **Autonomous** | Decide alone. Mark every uncertain line with `⚠️ Assumption: [what and why]`. |

**How to read the mode**: look for `--no-questions` or `--questions-at-end` in the argument. Neither
one present means interactive.

## Scope

- Read the code as it stands. Its shape is the subject of the report, not a target for change.
- Leave bugs and code smells to a code reviewer.
- The user knows the domain better than you. Ask about meaning, decide about mechanics yourself.

## Input

- With a path: read the production code. Read the tests next to it as a second source on meaning.
- Without a path: ask what to read.
- Production code first, always.

---

## Step 0 — map every entry point

Find every public way into the module: facades, controllers, schedulers, event handlers, background
jobs. A module often has several independent ways in.

Scan for `public class`, `[ApiController]`, `[HttpGet]`/`[HttpPost]`, hosted services, event
handlers, `public async Task`.

Ask the user:
> "I found these entry points:
> 1. `[MainFacade]` — main facade
> 2. `[QueueApi]` — queue operations
> 3. `[Scheduler]` — rejection after the cutoff
>
> Is the list complete? Is there another way into this module?"

### Step 0b — the level

For each entry point in the interface layer, read its body and place the level:

- **Thin delegate** (one line to a facade) → the level is the facade below it.
- **Holds logic** (conditions, filtering, orchestration) → the level is the interface layer.

Ask the user whether they agree with the split.

## Step 1 — the observation point

The observation point is where a test reads the result: a DTO, a read model, a query, a response.

1. Read the **full public API** of every DTO and read model the entry points return. Every public
   member is a place where behaviour can show.
2. Say what each one means in business terms, and ask the user to correct you.
3. Classify the module: **CRUD** — the stored row is the result; **domain logic** — something more
   visible than stored state carries the result.

Ask the user:
> "I found the read model `[ClassName]`:
> - `Balance(ownerId)` — how much this owner owes
> - `Loss()` — the part above the limits
>
> Is that reading right? Which of these carry business meaning?"

The header of the report comes from this step: **scope, level, observation point.**

## Step 2 — name behaviours, not methods

Write each behaviour as one sentence a domain expert would accept:

- "A VIP client gets a discount on a weekend ride" — not "test CalculatePrice with isVip=true".
- "After the third rejected claim in a month the next one is blocked" — not "test claimCounter >= 3".
- "An offer expires after seven days and cannot be accepted" — not "test IsExpired()".

Group the behaviours by subject (the estimate, the tariff, the route, the final amount, the fee,
the invoice, the effects in other contexts). Give each group a letter and each behaviour a number
inside it: A1, A2, B1.

Ask the user whether the list matches what the code should do, and what is missing.

## Step 3 — boundaries

A boundary is a value at which behaviour changes: a threshold, an hour, a date, an empty list, one
element against many, a state that closes the operation.

- Read the conditions in the code and write both sides of each one.
- Read the input types for variations that carry a **different business meaning**, not for every
  permutation.
- Put the boundaries in one line under the group they belong to.

## Step 4 — notes

Short notes for what the map alone hides:

- **Two readings of one rule.** Two places decide separately and can disagree at a boundary. Name
  both, name the value where they part, and say which pair of outputs holds them together.
- **Intermediate abstractions** (policy objects, strategy selectors, validator registries) —
  visible from outside, or internal machinery? Ask the user.
- **Calls into another module** — name the read model of that module where the effect shows.

## Where behaviour shows

| Shape in code | Signal | Where it shows |
|---|---|---|
| Invisible side effect | calls a service, sends mail, publishes an event | the read model of the target context; the events on the aggregate |
| Expected no-op | the operation is refused | the returned decision, and the state that reads the same before and after |
| Time-dependent | dates, expiry, windows, schedules | the same call at two clock values |
| Process | several steps, status changes | what the client sees after each step |
| History-dependent | counts earlier occurrences | the Nth call through the public API |
| Concurrency | lock, version, synchronised block | one operation wins, the other fails |
| Many parameters | branches, strategy choice | the choice and the amount are two separate observations |
| Permission gate | roles, access checks | the operation passes or fails |
| Eventual effect | an event another module consumes | a query in the target context |
| Costly call | a paid or rate-limited API inside a computation | the number of calls is itself observable |
| Cross-module call | the facade of another module | the read model of that module |

---

## The report

One markdown block, this shape:

```
## Observable behaviours around [subject]

Scope: [n] entry points. Test level: `[Facade]` facade, observed through `[Dto]`.

### A. [Group name]

- **A1** — [behaviour in business language]
- **A2** — [behaviour in business language]

### B. [Group name]

- **B1 [Variant]** — [rule] → [values]
- **B2 [Variant]** — [rule] → [values]

Boundaries worth one look each: [value / value · value / value · …]

> [note: the two readings, the pair that holds them together, the assumption]
```

Groups follow the subject of the code. Numbers stay stable inside a report, so a reader can point
at A3 or B5 in a conversation.

Write the report to `observable-behaviours.md` in the working directory. Close the file with the
line that reproduces the run, `/observable-behavior-test-advisor <path>`, and the commit you read
the code at. Tell the user where the file is. The next reader, a person or another skill, starts
from that file.

## Principles

1. **Business language first** — a domain expert reads the list without the code next to it.
2. **Observable is what the user or the system can do** — not what changed in the database and not
   which method ran.
3. **The observation point belongs to the outside** — a DTO, a read model, a response, an event.
4. **Climber's rule** — internal types read from outside freeze in place. Name the behaviour, name
   the outside point.
5. **CRUD is the exception** — when the whole behaviour is "save it and read it back", the stored
   row is the observation point.
6. **A costly call is observable** — money and rate limits are business effects.
7. **One boundary, one line** — a value that changes behaviour earns a place in the map.
8. **No dogma** — these are heuristics. The user's reason beats them.
