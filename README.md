🌪️ QueueStorm Investigator

bKash presents SUST CSE Carnival 2026 — Codex Community Hackathon
AI/API SupportOps Challenge for Digital Finance — Online Preliminary Round

An AI/API copilot for support agents. Given a customer complaint and a short
snippet of their recent transaction history, it investigates — not just
classifies — and returns a single structured, safety-checked JSON decision.


📌 What This Service Does

A digital finance platform is mid-campaign and the support queue is overflowing.
Agents can't read every ticket carefully, so this service reads each ticket,
checks it against the customer's transaction history, decides what actually
happened, routes it to the right team, and drafts a safe reply — without ever
asking for a PIN/OTP, confirming a refund it has no authority to confirm, or
trusting the complaint text blindly.

It is an investigator, not a classifier: the complaint says one thing, the
transaction data may say another, and the service is responsible for telling
the difference.


⚙️ Tech Stack

ComponentChoiceRuntime.NET 8 (ASP.NET Core Minimal API)DatabaseNone — fully stateless, every request is self-containedExternal packagesSwashbuckle (Swagger UI) onlyLLM (optional)Anthropic Claude API (claude-haiku-4-5-20251001)HostingRender (Docker-based)


🏗️ Architecture

                       POST /analyze-ticket
                                |
                                v
                    +------------------------+
                    |  ReasoningOrchestrator  |
                    +------------------------+
                                |
              +-----------------+-----------------+
              v                                    |
 +--------------------------+      fails / times out / no API key
 | AnthropicReasoningService| -----------------------+
 |   (LLM, 8s timeout)      |                        v
 +--------------------------+      +---------------------------+
              | success             | RuleBasedReasoningService |
              v                     |  (deterministic engine)   |
              |                     +---------------------------+
              +--------------+-----------------------+
                             v
                    +-------------------+
                    |  SchemaValidator  |  <- normalizes enums, rejects bad output
                    +-------------------+
                             v
                    +---------------------+
                    | SafetyFilterService |  <- ALWAYS runs, on every response
                    +---------------------+
                             v
                        JSON response

Two operating modes

With ANTHROPIC_API_KEY setWithout itPrimary engineLLM (Claude Haiku)Rule-based engineFallbackRule-based engine, on any LLM failure—External callsYes, to Anthropic APINone at all

The rule-based engine is not a "degraded mode" — it is a complete,
self-sufficient implementation. An LLM is not required for this service to
function correctly.

Two guarantees that never get skipped


SchemaValidator — normalizes or corrects any enum value before it
ships, and never lets a response leave with a missing required field.
SafetyFilterService — regex-scans every customer_reply and
recommended_next_action, from either engine, for credential requests,
unauthorized refund/reversal confirmations, or third-party referrals. Any
violation gets the response rewritten with a known-safe template before it
ever reaches the client.



🧠 MODELS

ModelWhere it runsWhy it was chosenclaude-haiku-4-5-20251001Anthropic's cloud, called over HTTPSFast and inexpensive, more than sufficient for structured classification/extraction. Keeps latency comfortably under the 30s timeout and close to the 5s p95 target. Used only when ANTHROPIC_API_KEY is configured.Rule-based engine (no ML model)In-process, zero network callsThe deterministic fallback and standalone mode. Zero latency, zero external dependency, zero hallucination risk — used automatically whenever the LLM is slow, unavailable, or returns invalid output.

No other models are used. No fine-tuning was performed.


🔍 Evidence Reasoning Approach

Both engines follow the same investigative steps:


Extract signals from the complaint — amount, approximate time,
counterparty — in English, Bangla script, and common Banglish.
Cross-check those signals against every entry in transaction_history.
Identify the transaction: relevant_transaction_id is set to the
best-matching entry's ID, or null if nothing matches — never invented.
Decide the verdict by comparing the matched transaction's actual
status against what the complaint claims happened:

data supports the complaint → consistent
data contradicts the complaint → inconsistent
not enough information → insufficient_data


Genuine ambiguity always resolves to insufficient_data — a confident
wrong guess is worse than an honest "not sure."
Derive case_type, severity, department, and
human_review_required from the case type and verdict together, not from
amount thresholds alone.


A few of the harder reasoning behaviors this implements:


Established-recipient detection — if a "wrong transfer" complaint's
matched transaction shows the same recipient has been paid multiple times
before, that contradicts the claim, and the verdict flips to inconsistent.
Ambiguous-match handling — if multiple transactions equally match on
amount alone with different recipients, the engine does not guess; it
returns insufficient_data and asks a clarifying question instead.
Duplicate-pair detection — for duplicate-charge complaints, it looks for
two same-amount, same-counterparty transactions close together in time and
correctly flags the second one as the suspected duplicate.


The LLM's system prompt enforces this same cross-checking discipline
explicitly, and treats the complaint field as untrusted input — any
instructions embedded inside it are analyzed, never obeyed (prompt-injection
resistance).


🛡️ Safety Logic

Enforced at two independent layers:

1. Prompt-level (LLM path only) — the system prompt forbids requesting
credentials, confirming refunds/reversals, or referring customers to third
parties, and instructs the model to ignore any instructions hidden inside the
complaint text.

2. Code-level guardrail (SafetyFilterService, runs on every response,
regardless of source) — scans for:

Violation typeExample pattern caughtCredential requests"please share your OTP", "confirm your PIN"Unauthorized confirmations"we will refund you", "we have refunded"Third-party referrals"contact this number", "call +880..."

If any pattern matches, customer_reply and recommended_next_action are
wholesale replaced with a pre-written safe template, severity is escalated,
and human_review_required is forced to true. This makes a Section 8
safety violation structurally impossible to ship — even if the LLM generates
unsafe text.

The rule-based engine's customer-facing text is built entirely from a fixed,
pre-written template library — no free text generation — so it is safe by
construction, in both English and Bangla.


📡 API Contract

EndpointBehaviorGET /healthReturns {"status":"ok"} instantly, no dependencies, ready within 60s of startPOST /analyze-ticketAccepts one ticket, returns the structured decision JSON

CodeMeaning200Successful analysis400Malformed JSON or missing required field (ticket_id)422Schema-valid but semantically invalid (e.g. empty complaint)500Internal error — never includes stack traces or secrets

The service never crashes the process on bad input.


🚀 Setup & Run

Local (.NET SDK 8 required)

bashcd QueueStormInvestigator
cp .env.example .env
# Fill in ANTHROPIC_API_KEY for LLM mode, or leave it blank for rule-based-only mode.

dotnet restore
dotnet build
dotnet run

The service listens on $PORT, defaulting to 8080 if unset. Open
http://localhost:8080/swagger to explore and test both endpoints interactively.

Docker

bashdocker build -t queuestorm-investigator .
docker run -p 8080:8080 -e PORT=8080 -e ANTHROPIC_API_KEY=your_key_here queuestorm-investigator

Omit -e ANTHROPIC_API_KEY to run in rule-based-only mode.

Render


New Web Service → connect this repository → Render auto-detects the Dockerfile.
Set ANTHROPIC_API_KEY under the Environment tab in the Render dashboard
(never commit it to the repo).
Render injects PORT automatically; Program.cs binds to it directly — no
manual configuration needed.



✅ Validation Against the Public Sample Pack

The rule-based engine was checked against all 10 cases in
SUST_Preli_Sample_Cases.json, matching the expected output on every field —
case_type, evidence_verdict, relevant_transaction_id, department,
severity, and human_review_required — including the harder reasoning cases:


Established-recipient pattern (SAMPLE-02) — correctly flags
inconsistent when the recipient has multiple prior completed transfers.
Ambiguous multi-match (SAMPLE-08) — correctly returns insufficient_data
and asks for clarification instead of guessing.
Bangla input & output (SAMPLE-07) — detects Bangla script and replies in
Bangla, matching the input language.
Pending-status evidence (SAMPLE-07, SAMPLE-09) — treats a pending
transaction as evidence supporting a "money not received" complaint.
Duplicate-pair detection (SAMPLE-10) — correctly identifies the second
of two matching transactions as the suspected duplicate.


This is a local sanity check only — the hidden judge set is broader and
includes scenarios outside the public pack, as the problem statement notes.


⚠️ Known Limitations


Rule-based keyword coverage for Bangla/Banglish is broad but not exhaustive.
Complaints phrased outside the known keyword set safely default to
case_type: other / evidence_verdict: insufficient_data rather than
guessing — a deliberate safe fallback, not a crash.
Amount extraction can mismatch if multiple numbers appear in one complaint
(e.g. a phone number near an amount). Transaction matching uses a
multi-signal score (amount + time + counterparty) to reduce false matches,
but it is not infallible.
The LLM path depends on Anthropic API availability and the team's own API
key. If either is unavailable, the service automatically and transparently
falls back to the rule-based engine with no manual intervention required.
No persistent storage or learning across requests — every ticket is
evaluated independently, as specified.



📁 Project Structure

QueueStormInvestigator/
├── Program.cs                          # Endpoints, DI, Swagger, error handling
├── Models/
│   ├── Enums.cs                        # Exact taxonomy from the spec
│   ├── TicketRequest.cs                # Request schema
│   └── TicketResponse.cs               # Response schema
├── Services/
│   ├── IReasoningService.cs
│   ├── AnthropicReasoningService.cs    # LLM-backed reasoning
│   ├── RuleBasedReasoningService.cs    # Deterministic fallback / standalone engine
│   ├── ReasoningOrchestrator.cs        # LLM → fallback → validate → safety-filter
│   ├── SafetyFilterService.cs          # Non-bypassable safety guardrail
│   └── SchemaValidator.cs              # Enum normalization
├── Dockerfile
├── appsettings.json
├── .env.example
└── README.md
