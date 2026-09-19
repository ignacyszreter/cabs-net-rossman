---
name: hexagon-plan
description: Reads an entry class (controller, handler, job) and plans ports and adapters for the logic inside it — input port, output ports named after domain needs, fake or real per port, step order with checks. When the ports already exist, gives feedback on them. Reads code and runs tests; writes only its report.
argument-hint: '<entry file.cs>'
allowed-tools: Bash, Read, Grep, Glob, Write
---

# /hexagon-plan

One entry class. When its logic still lives inside it, the result is a plan of ports and adapters.
When it already delegates to a port, the result is feedback on the ports, the adapters and the
tests. You read the code yourself and run tests. You never edit a code or test file: the only file
you write is `hexagon-plan.md`. Every finding names its `file:line`.

## Step 1 — read the entry class

Write into `hexagon-plan.md`:

1. **Dependencies** — every constructor parameter and what it talks to: the database directly,
   HTTP directly, a repository (an interface, so already a port), the configuration, the clock,
   another abstraction, a concrete class.
2. **Lines by concern** — every line of every method, as `line | member | concern | code`. The
   concerns: *http in* (routing, request parsing, status codes), *http out*, *sql*, *database*
   (a `DbContext`), *repository*, *file*, *clock* (`DateTime.Now`), *configuration* (environment,
   settings), *data format* (JSON or XML parsing), *messaging*, *logic* (branches, arithmetic,
   aggregation). Close with a count per member.
3. **Branches on time or environment** — a condition or a flag fed by the clock or the
   configuration that picks a provider, a URL or a format.
4. **Technology in signatures** — `HttpResponseMessage`, `JsonElement`, `DbDataReader`,
   `IQueryable`, `Stream` and the like in a public signature.
5. **Who uses the class** — grep production code, tests and the DI registration. Name every caller
   that brings a different trigger: a scheduled job, a message handler, a second UI.

The class **delegates** when it depends on one or two abstractions and has no outside-world,
data-format or logic lines. Delegating means step 3; anything else means step 2.

## Step 2 — the plan

1. **Findings** — four problems; for each, the evidence, or `✓` and why not:
   - *One way in* — the logic lives in the entry class, so a second trigger has to call the
     controller or copy the logic. Move: an input port.
   - *Tests that fail for the network* — tests reach real HTTP or a real database and swap nothing.
     Move: a test driver at the input port and fakes for the output ports.
   - *Cases no test can build* — a rule reads data parsed from an outside service (holidays,
     rates), and the interesting case needs that service to produce chosen data. Move: an output
     port and a fake with a method that states a fact of the world.
   - *Change by editing the rule* — a branch from step 1.3 picks a provider. Move: a second
     adapter behind the same port; the choice goes to the DI registration.
2. **Input port** — the interface named after the capability (`IOrderSettlement`), one method per
   use case, arguments and result in domain types. Which lines move inside, which stay in the entry
   class (routing, parsing, the status code).
3. **Output ports** — one per outside need, named after the need, never after the provider:
   `ITaxRates.RateOn(date)`, not `IAcmeTaxClient.GetRatesJson(url)`. No technology type in the
   signature. For each: the lines that move into its adapter, the adapter name, and the test double:
   - supplies data from I/O or an integration → a **fake**, in memory, with methods that state
     facts the port does not have: `ClosedOn(2027-01-04)`, `RateIs(0.23m)`;
   - hides business logic that runs elsewhere (a stored procedure, a rules engine) → the **real**
     engine, e.g. a database in a container; a fake would be a second copy of the rule;
   - costs money or has a rate limit → a mock that counts calls, in one test.
4. **Registration** — the DI lines that bind each port to its adapter.
5. **Steps** — one commit each; each commit changes either tests or production:
   1. a safety net through the entry point, when no test reaches it today;
   2. the input port and the class behind it; the entry class delegates;
   3. one output port and its adapter at a time;
   4. tests that drive the input port instead of the entry point;
   5. fakes for the data ports, and the cases no test could build before;
   6. a second adapter for every branch of step 1.3.

   Every step ends with a check someone can run: the entry class has one dependency; the class
   behind the input port has no outside-world and no data-format lines; the tests are green.

## Step 3 — the feedback

Find the class behind the input port, the output ports it takes, their adapters and the tests, and
read each the same way as in step 1. Then check, one line each, `✓` or a finding:

1. the entry class holds routing and parsing only;
2. the class behind the input port has no outside-world and no data-format lines;
3. no port signature carries a technology type;
4. every port is named after a need, not after a provider;
5. every adapter implements a port, and no port references an adapter;
6. tests reach the inside through the input port and swap every data port for a fake;
7. mocks on port methods in the tests — each one breaks when the port changes shape, because it
   records *how* the code asks, while a fake records *what is true*;
8. a branch on time or environment still lives inside;
9. a caller with another trigger (step 1.5) that still goes around the input port.

Run the tests once and report the result. Close with **next three moves**, most valuable first.

## Step 4 — close the file

End `hexagon-plan.md` with the line that reproduces the run, `/hexagon-plan <entry file.cs>`, tell
the user where the file is, and stop.

Arguments: $ARGUMENTS
