---
mode: 'agent'
description: 'Generate automated UI test from Jira URL'
tools: ['atlassian-mcp-server']
---

The user will provide a Jira URL (e.g., `https://ioscorp.jira.com/browse/ENVI-920936`).

**Step 1 — Extract the issue key from the URL.**
Parse the issue key from the URL path (everything after `/browse/`). For example, `https://ioscorp.jira.com/browse/ENVI-920936` → issue key is `ENVI-920936`.

**Step 2 — Fetch the issue details from Jira.**
Use the `atlassian-mcp-server` MCP tools to retrieve the issue:
- Use `getJiraIssue` with the extracted issue key to get full issue details.
- If the cloudId is unknown, call `getAccessibleAtlassianResources` first.
- Request fields: `summary`, `description`, `status`, `issuetype`, `labels`, `parent`, `issuelinks`, `customfield_10008`.

From the response, extract:
- **Test Case ID** — from `key`
- **Parent Story ID** — from `fields.parent.key`, or `fields.customfield_10008` (epic link), or check `fields.issuelinks` for "is child of" / "is part of" links
- **Title** — from `fields.summary`
- **Description / Steps** — from `fields.description` (parse numbered steps and expected results)
- **Labels** — from `fields.labels` array — check for: Regression, Nightly, AT_Tests (maps to Acceptance), Prod, Smoke
- **Issue Type** — from `fields.issuetype.name` (confirm it's a Test or Sub-task)

If the fetch fails or the tool is unavailable, reply with:
> I cannot access Jira automatically. Please paste the test case details:
> - Test Case ID
> - Parent Story ID
> - Title
> - Module/Feature
> - Steps (numbered)
> - Expected Results
> - Labels (Regression, Nightly, AT_Tests, Prod, Smoke)
> - Old UI or New UI

Then stop and wait for the user to paste the details.

**Step 3 — Determine which test class this belongs to.**
Based on the Module/Feature extracted from the Jira issue (or inferred from the parent story), search the codebase for an existing test class in the matching folder under `TestAutomation.WebUI.Tests\`. If a matching class exists, generate a new test method to add to it. If no match exists, generate a new test class file.

**Step 4 — Generate the test following ALL rules from** #file:.github/instructions/test-generation.instructions.md

Generate:
- **Test method** following all conventions (BaseUITest, AllureFeature, Categories, Helpers.Logging.Step, NUnit constraint assertions, TestActions cleanup)
- **Page object class** (only if HTML is provided and a page object does not already exist)
- **Private helper methods** at the bottom of the test class

Key rules:
- Always use `{ get; private set; }` in page objects
- Always use `using _ = PageClassName;` alias
- Always use `Assert.That()` constraint-based assertions
- Always register cleanup via `TestActions.Add()`
- Never use `Thread.Sleep` — use `.Wait(Until.Visible)` or `.WaitSeconds()`
- Never hardcode GUIDs — resolve via API
- Never hardcode entity values from Jira test cases — convert string identifiers to `Generator.Alphanumeric(20)` and dates to dynamic `DateTime.UtcNow` expressions (see Hardcoded Value Conversion Rules in instructions)
- Add `Category(Nightly)`, `Category(Acceptance)`, `Category(Prod)`, `Category(Smoke)` ONLY if explicitly specified in the Jira Labels. Map Jira label `AT_Tests` to `Category(Acceptance)`
- If anything is unclear (which API service, page element, or builder to use), ASK before guessing
