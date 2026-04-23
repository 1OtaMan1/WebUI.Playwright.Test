---
mode: 'agent'
description: 'Generate automated UI test from manually pasted Jira details'
---

The user will paste test case details manually.

Expect the following input:
- **Test Case ID**: e.g., ENVI-377275
- **Parent Story ID**: e.g., ENVI-377270
- **Title**: Short description
- **Module/Feature**: e.g., "Contracts (Inbound) Interface"
- **Steps**: Numbered test steps
- **Expected Results**: What each step should verify
- **Labels**: Regression, Nightly, Acceptance, Prod, Smoke (if specified)
- **Old UI or New UI**

**Generate the test following ALL rules from** #file:.github/instructions/test-generation.instructions.md

If anything is unclear (which API service, page element, or builder to use), ASK before guessing.