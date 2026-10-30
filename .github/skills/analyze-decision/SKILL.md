---
name: analyze-decision
description: "Compare options for a technical decision and record it as an ADR"
---
Help me make this decision and record it as an architecture decision record (ADR):

the request given after the command name

1. Restate the decision, the forces behind it (requirements, constraints, quality goals), and how we will know later that it was right.
2. Identify two to four realistic options, including "do nothing" where relevant. Research each against this codebase: what it touches, what it costs to build and run, and what it locks us into.
3. Compare the options in a table across the forces. Be concrete: name files, libraries, versions and numbers.
4. Recommend one option and explain why the others lose. State the risks of the recommendation and how to reduce them.
5. If weighing the forces needs my input, ask before writing.
6. Write the ADR where this repository keeps decisions, or `docs/adr/NNNN-<slug>.md` (next free number) if it has none, with sections Context, Options, Decision, Consequences. Change no other file.
