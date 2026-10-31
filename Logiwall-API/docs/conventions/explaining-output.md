> Detail for `instruction.md` §20. Load before you write a final report, a plan or spec, an ADR,
> a QA file, a PR description, or any explanation a person must read and understand.

# 20. Explaining Output to People

The agent does most of the work. The person checks the work and decides. That check is only as
good as the person's understanding of what the agent wrote. So every output for a person must be
easy to read, easy to check, and quick to understand.

There are three output formats. Each one is stronger than the one before it:

| Level | Format | Use it when |
|---|---|---|
| 1 | Writing in Simplified Technical English (ASD-STE100, about 80%) | Always. This is the default for all prose. |
| 2 | A diagram | The answer has a structure: a flow, a sequence, states, relationships, dependencies. |
| 3 | An HTML page | The answer is too large or too multi-part for the terminal, or interaction helps. |

A higher level adds to the lower levels. It does not replace them. A diagram has a caption in
Simplified Technical English. An HTML page has diagrams and Simplified Technical English text, and
the terminal reply still gives a short summary.

---

## 20.1 Level 1 — Writing in Simplified Technical English

ASD-STE100 is a controlled language. It was made for aerospace maintenance manuals, where a
misunderstood sentence can cause an accident. Its rules make text short, direct and unambiguous.
Logiwall uses about 80% of the specification: the writing rules apply in full, but the approved
dictionary does not, because software needs technical names (controller, service, migration,
provider) that the dictionary does not contain.

### Where it applies

* Every chat reply, status update and final report.
* Prose that the agent writes in `docs/`: plans, `tasks.md`, ADRs, conventions.
* QA files (`*.QA.md`), PR descriptions and commit message bodies.
* XML doc comments and code comments that explain behaviour.

It does not apply to code, identifiers, log message templates, quoted error text, or text that a
person wrote. Do not rewrite existing docs only to make them conform (`instruction.md` §16).
Apply the rules when you write new text or when you change a paragraph for another reason.

### The rules

**Sentences**

1. Write a maximum of 20 words in an instruction sentence (a step, a command, a rule).
2. Write a maximum of 25 words in a descriptive sentence. Code identifiers count as one word.
3. Write one instruction in each sentence. Two actions in one sentence are permitted only when
   they occur at the same time.
4. Put a condition before the action: "If the migration fails, roll back the database." Not:
   "Roll back the database if the migration fails."
5. Do not omit words to make a sentence shorter. Keep the articles ("the", "a") and the verb.
   Telegraphic text ("Fixed bug, added test, all green") hides meaning.

**Verbs**

6. Use the active voice. Name who or what does the action: "`OrderService` writes the order to
   `AppDbContext`." Not: "The order is written."
   The passive voice is permitted only when the actor is unknown or not important.
7. Use the imperative for instructions: "Run the migration." Not: "You should run the migration."
8. Use only simple tenses: present, simple past and future. Do not use "-ing" forms as verbs.
   "The provider retries the request." Not: "The provider is retrying the request."
9. Use specific verbs. Not: "handle", "deal with", "take care of", "manage" when you can say what
   the code actually does ("validates", "rejects", "retries", "logs").
10. Avoid phrasal verbs that have more than one meaning ("set up", "carry out", "go over"). Use
    one specific verb ("configure", "do", "examine").

**Words**

11. Use one word for one meaning, and one meaning for one word. When the code calls it an
    "option" (`OrderInqueryOption`), do not also call it a "quote", an "offer" and a "price".
12. Use the name the code uses. Write `OrderState`, not "the order status thing".
    Put code names in backticks.
13. Do not make noun clusters of more than three words. Not: "order payment status update
    callback". Write: "the callback that updates the payment status of the order".
14. Use short, common words. Use "use", not "utilize". Use "start", not "initiate". Use "about",
    not "approximately".
15. Do not use vague words: "some", "various", "etc.", "properly", "correctly", "should work".
    Give the number, the list or the fact.

**Paragraphs and structure**

16. Write one topic in each paragraph, and a maximum of six sentences in each paragraph.
17. Put the most important information first: the result, the answer or the decision. Give the
    reasons after it.
18. Use a vertical list for three or more items, steps or conditions. Use a numbered list when
    the order is important.
19. Use connecting words to show the relation between sentences: "because", "then", "but",
    "as a result".
20. Write a warning before the step that it applies to, and start it with a command:
    "Do not run this against production. It deletes all rows in `Orders`."

**Facts**

21. State facts with their source. Give `file_path:line` for code, the command for a test
    result, the section number for a rule.
22. Say what you did not verify. "The build passes. I did not run the integration tests,
    because Docker is not available." Do not use hedge words to hide a gap.

### Example

Before (typical LLM prose, one sentence of 52 words):

> So basically what's happening here is that when the user creates an order, the controller is
> going to be passing everything over to the order service which then ends up looking up the
> customer and saving the order to the database, assuming everything is configured properly.

After (Simplified Technical English, four sentences, each under 20 words):

> `OrderController.CreateOrder` reads the user ID from the token. It calls
> `OrderService.CreateOrderAsync`. The service finds the customer for that user. If there is no
> customer, it returns `Succuss = false`; else it saves the order and returns its number.

---

## 20.2 Level 2 — Diagrams

A diagram shows structure faster than prose. Use one when the reader must understand how parts
connect, not only what one part does.

### When to draw a diagram

Draw a diagram when the answer contains one of these:

| The answer contains | Diagram type | Mermaid keyword |
|---|---|---|
| A request that passes through more than two components | Sequence diagram | `sequenceDiagram` |
| A process with decisions or branches | Flowchart | `flowchart` |
| An entity with a lifecycle (`OrderState`, `PaymentStatus`) | State diagram | `stateDiagram-v2` |
| Tables, entities or their relationships | Entity-relationship diagram | `erDiagram` |
| Project or class dependencies | Graph | `flowchart LR` |
| A comparison of two designs | Two small diagrams side by side, or one before/after pair | any |

Do not draw a diagram for a single fact, for a list of fewer than three steps, or when a
three-line table says the same thing.

### Where the diagram goes, and in which format

| Destination | Format | Reason |
|---|---|---|
| Chat reply in the terminal | Plain-text diagram (box-drawing or ASCII) in a ` ```text ` block | The terminal does not render Mermaid. |
| A file in `docs/`, a QA file, a PR description | Mermaid in a ` ```mermaid ` block | GitHub renders Mermaid. The source stays editable in a diff. |
| An HTML page (Level 3) | Inline SVG, or Mermaid loaded from a CDN with a pinned version | The page must render with no build step. |

### Rules

1. Make the diagram match the code. Read the code before you draw it. Do not draw from memory
   or from a plan that may be out of date.
2. Use the real names: project, class, table and endpoint names, exactly as they appear in the
   code. The reader must be able to search for each box.
3. Keep a diagram to 12 nodes or fewer. If it needs more, split it: one overview diagram, then
   one detail diagram for each part.
4. Label each arrow with a verb: "calls", "reads", "writes", "calls `GetOptionsAsync`".
   An arrow with no label shows that a relation exists, but not what the relation is.
5. Show the direction of each dependency or data flow. Do not use lines with no arrow head.
6. Show the project boundary when a flow crosses it. Use a Mermaid `subgraph` or a box around
   each project's parts.
7. Keep a plain-text diagram 100 characters wide or less, so the terminal does not wrap it.
8. Write a caption of one or two sentences under each diagram. Say what the diagram shows and
   which part the reader must look at.
9. Explain any notation that is not obvious (a dashed arrow, a colour) in the caption.

### Example (terminal)

```text
 Logiwall.Server             Logiwall.Infrastructure
┌──────────────────────┐    ┌───────────────────────────────────────┐
│ OrderController      │    │ OrderService                          │
│   CreateOrder        │    │   CreateOrderAsync                    │
│     │ calls          │    │     │ reads People, writes Orders     │
│     └────────────────┼───▶│     ▼                                 │
│                      │    │ AppDbContext ──writes──▶ SQL Server   │
└──────────────────────┘    └───────────────────────────────────────┘
```

The controller calls the service through `IOrderService` (`Logiwall.Application`). The service
uses `AppDbContext` directly; there is no repository.

---

## 20.3 Level 3 — HTML pages

An HTML page can hold more than a terminal reply: diagrams, tables, tabs, sections that the reader
can expand, and links between parts. Code is cheap for the agent to write, so a page made only to
explain one answer is a correct use of effort. The page is disposable. It does not need to be
maintained.

### When to make an HTML page

Make an HTML page when one of these is true:

* The person asks for a page, a visual explanation or "HTML".
* The explanation is longer than about two terminal screens (about 120 lines).
* The answer has many parts that the reader must compare or move between: an area overview, a
  review of a large diff, the options for a design decision.
* Interaction helps the reader: filter a table, expand a code path, switch between "before" and
  "after", step through a flow.
* The data is tabular and large: test results, a schema walkthrough, a list of endpoints with
  their authorization.

If the explanation is long but none of these is true, offer the page in one sentence and wait.

### Rules

1. Write one self-contained `.html` file. Put all CSS and JavaScript inline. Load external
   libraries only from a CDN, with an exact version number. There is no build step.
2. Write the file outside the repository: in the session scratchpad or the system temporary
   folder. Do not commit a generated page unless the person asks for it.
3. If the session has the `Artifact` tool, publish the page with it and give the link. If it does
   not, give the file path so the person can open the file in a browser.
4. Put the answer at the top of the page: the result, the decision or the summary. Put the detail
   below it.
5. Apply Level 1 to all text on the page, and Level 2 to all diagrams on the page.
6. Give a source for each fact: `file_path:line` for code, the section number for a rule, the
   command for a test result.
7. Make the page work in a narrow window and in light and dark mode.
8. Do not put secrets on the page: no connection strings, API keys, tokens, passwords,
   `appsettings.*.json` values or personal data from a database. A published page can be shared.
9. After you make the page, give a short summary in the terminal too. The person must not need
   to open the page to know the main result.

### Good subjects for a page in Logiwall

* The order flow from end to end: create order, get options (MyTipax), select option, payment (SEP).
* The OTP sign-in flow: send OTP, verify, token.
* The status of the audit cycle: each plan, its dependencies and its state.
* A review of a large diff, grouped by area, with each finding next to its code.
* A comparison of two or three design options, with a diagram and the trade-offs of each.

---

## 20.4 Checklist before you send an output

* The first sentence gives the result or the answer.
* Each sentence is 20 words (instruction) or 25 words (description) or fewer.
* There is no passive voice where the actor is known, and no "-ing" verb forms.
* Each term has one name, and that name is the name in the code.
* Each fact has a source, and each gap in verification is stated.
* If the answer has a structure, there is a diagram, and the diagram matches the code.
* If the answer is longer than two screens or has many parts, there is an HTML page, or an offer
  of one.
