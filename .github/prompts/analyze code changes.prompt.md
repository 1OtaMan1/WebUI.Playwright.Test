---
mode: 'ask'
description: 'Analyze changes in code before commit and push it'
---

PROMPT START
After user write word 'ok' or 'start' or 'старт', write next instructions:

1. Confirm you have UNCOMMITTED changes (do NOT stage them if you want the full diff). If you only want staged changes, stage them now.
2. From the repository root (PowerShell or terminal) run ONE of:
   Full (uncommitted + unstaged) diff:
     git diff > TestAutomation.WebUI.Tests\bin\pending.patch
   Staged only:
     git diff --staged > TestAutomation.WebUI.Tests\bin\pending.patch
3. Open folder: TestAutomation.WebUI.Tests\bin
   Open file: pending.patch
   Copy ALL its contents.
4. Paste the entire patch here (start with a line: PATCH START and end with PATCH END).

After I paste the patch:
•	Do NOT summarize only; deeply analyze every hunk.
•	For each changed file:
•	Point out potential logical bugs.
•	Check for flaky automation patterns (locators, sleeps, retries).
•	Suggest improvements: readability, duplication removal, helper extraction, test stability.
•	Identify any naming or grammatical issues (English only).
•	Flag risky XPath or selector changes (e.g. broader matches, loss of specificity).
•	Note unused usings introduced or ones now removable.
•	Spot security or robustness issues (null handling, exception swallowing).
•	Suggest consolidation with existing solution patterns (mention shared helpers if applicable).
•	If repetitive code blocks appear across multiple files, propose a single abstraction with example code.
•	Call out any magic numbers and propose constants.
•	If localization or user-facing strings changed, check spelling.
Output format:
1.	Summary (one concise paragraph).
2.	File-by-file review (ordered as in patch).
3.	Cross-cutting recommendations (deduplications, helpers).
4.	High-priority action list (bullet list, ordered by impact).
5.	Optional refactor snippets (only if value > noise).
If patch is too large, ask me whether to proceed or to narrow scope.
Wait silently for the patch content now—do not start analysis until I provide it.
PROMPT END

