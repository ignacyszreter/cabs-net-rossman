# Symptom catalogue — what a co-change shape usually means

Every diagnosis carries the same six fields. The last one is what keeps the reading honest:

- **Symptom** — a name from this table. The table is the whole vocabulary of the reading.
- **Evidence** — files or methods, commit count, three hashes.
- **What it usually means** — one sentence.
- **Move** — the one action that tells whether the diagnosis holds.
- **What would disprove it** — the observation that kills this diagnosis.
- **Confidence** — high / medium / low.

| Symptom | Signature in the data | What it usually means | Move | What would disprove it |
|---|---|---|---|---|
| **Registration coupling** — not a finding | the cluster is the composition root, the `DbContext` or the DI configuration plus a new type, and the intent is "new X" every time | the framework demands the registration; the design is fine | none | the registration file also carries business rules of its own — then it is a finding |
| **Silent drift** — two copies of one rule | the same kind of value — a rate, a threshold, a format, one calculation — is set or computed in two or more methods in different files; they land in one commit three or more times, and the messages carry one business noun | the rule is implemented twice, so the two copies can disagree and nothing has to fail for that to happen | change one copy by hand and run the suite; green means nothing guards this rule | the suite goes red on the one-sided change. Or the two edits are unrelated: one renames, the other changes a query |

## Rules for the diagnosis

- One symptom per row. When both rows fit one cluster, give both and rank them.
- No symptom without evidence from the script output. A hash you cannot open is not evidence.
- Registration coupling has to be used when it fits. A reading that never says "this one is not a
  problem" finds a problem everywhere.
- **A cluster that matches neither row stays out of the report.** Sections 1 to 3 already carry its
  numbers, and the reader knows the domain you do not.
- Confidence is low when the naming rests on diffs alone, because the commit messages said nothing.
- **Silent drift stops at the drift.** The history shows two copies; it cannot show whether anything
  guards them. Report the two copies, hand over the one-command move, and let its result decide what
  comes next.

## Adding a symptom

The catalogue grows one row at a time, and a row is earned: it goes in after a team has read that
shape out of a history by hand. A row that arrives earlier does the reading for them.

A new row is finished when it has all five columns. Two of them do the work:

- **Signature in the data.** It has to be readable from the script output — a count, a pair, a
  shape of commit messages. "Bad code" is not a signature.
- **What would disprove it.** Write the observation that would make you withdraw the diagnosis, and
  make it something a person can check in one command. A row without this is an opinion in a table.

Name the symptom after what the history looks like, not after the fix. `Silent drift` says what you
saw; `Extract a Value Object` says what you decided, and hides the evidence that led there.
