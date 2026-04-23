---
applyTo: "**/*.cs"
---
# Automated Test Generation from Jira Test Cases

## Purpose

This document instructs GitHub Copilot how to generate automated UI tests from Jira test case descriptions for the TestAutomation.WebUI project.

---

## INPUT FORMAT

When I provide a Jira test case, it will include:

- **Test Case ID**: e.g., ENVI-377275
- **Parent Story ID**: e.g., ENVI-377270
- **Title**: Short description of the test
- **Module/Feature**: e.g., "Contracts (Inbound) Interface", "HL7 Depletion Interface"
- **Preconditions**: Setup requirements
- **Steps**: Numbered test steps
- **Expected Results**: What each step should verify
- **Test Categories**: Regression, Nightly, Acceptance, Prod, Smoke (if specified)
- **Hardcoded Values**: Exact values for entities (e.g., Contract Tier "11111", dates "03/01/2021") � these must be converted to dynamic values (see Hardcoded Value Conversion Rules below)
- **HTML of the page** (optional): Raw HTML of the UI page under test

---

## PROJECT STRUCTURE AND CONVENTIONS

### Test Class Location

- Tests go in: `TestAutomation.WebUI.Tests\{Module}\{Feature}\{TestClassName}.cs`
- Example: `TestAutomation.WebUI.Tests\Utilities\Interfacing\ContractsInboundTests.cs`

### Page Object Location

- Page objects go in: `TestAutomation.WebUI\Business\{Module}\{Feature}\{PageName}.cs`
- Example: `TestAutomation.WebUI\Business\Inventory\ContractList\ContractListPage.cs`

### Target Framework

- .NET 10, C# 12.0
- NUnit 3 with Atata framework
- Allure for reporting

---

## CODE GENERATION RULES

### Test Class Structure

Every test class must follow this exact structure. The class inherits from BaseUITest, uses AllureFeature for reporting, and places static shared fields at the top. Private helper methods go at the bottom of the class.

    using <required namespaces>;

    namespace TestAutomation.WebUI.Tests.{Module}.{Feature};

    [AllureFeature(nameof({TestClassName}))]
    [NonParallelizable]
    public class {TestClassName} : BaseUITest
    {
        private static readonly Guid InterfacePK = DomainAdmin.Interfacing.GetList("Interface Name").First().InterfacePK;
        const string ExpectedMessage = "Interface was executed successfully.";

        // --- Test methods go here ---

        // --- Private helper methods at the bottom ---
    }

Notes:
- Add `[NonParallelizable]` when tests share state, use FTP, or use interfaces.
- Test class names use PascalCase and end with `Tests` (e.g., `ContractsInboundTests`).
- Static readonly fields are used for interface PKs and other shared data resolved once at class load.
- Constants like `ExpectedMessage` are declared at class level when reused across multiple tests.

### Test Method Structure

Every test method must follow this exact structure. The `#region Setup` block prepares data, the steps use `Helpers.Logging.Step()` for traceability, and assertions use NUnit constraint model.

    [Test, Retry(2), TestCaseId("{ENVI-XXXXX}")]
    [Category(Regression), Category("{Parent-Story-ID}"), Category(Old_UI), Category("{Feature Category}")]
    public void {DescriptiveTestName}ODTest()
    {
        #region Setup
        const string expectedWarningMessage = "No data for loading.";
        var contractTier = Generator.Alphanumeric(22);
        var internalContractNo = "GPO Contract No 0001-" + contractTier;
        var zipFilePath = CreateContractZipArchive(contractTier);
        FtpInterface.UploadInterfaceFile(zipFilePath, InterfaceTypes.ContractsInb);
        #endregion

        Helpers.Logging.Step(1, "Step description matching Jira step");
        var interfaceDetailsPage = LoginAndGo.To<InterfaceDetailsPage>(WebUrls.ToInterfaceDetails(InterfacePK), DomainAdmin);
        interfaceDetailsPage.RunTestButton.Wait(Until.Visible);

        Helpers.Logging.Step(2, "Next step description matching Jira step");
        var messageModal = interfaceDetailsPage.RunTestButton.ClickAndGo();
        messageModal.MessageOD.Wait(Until.Visible);
        Assert.That(messageModal.MessageOD.Value, Is.EqualTo(ExpectedMessage), ExpectedMessage + " message should appear");
        messageModal.OkButtonOD.ClickAndGo();

        Helpers.Logging.Step(3, "Verify final result");
        var contractPK = DomainAdmin.Contracts.GetItems().First(_ => _.InternalContractNo.Equals(internalContractNo)).ContractPK;
        TestActions.Add(() => DomainAdmin.Contracts.Deactivate(contractPK));
        Assert.That(contractPK, Is.Not.EqualTo(Guid.Empty), "Contract should be created");
    }

Notes on categories:
- `Category(Regression)` is always present.
- `Category("{Parent-Story-ID}")` uses the parent Jira story ID (e.g., `"ENVI-377270"`).
- `Category(Old_UI)` is added for old-design tests; use `Category(New_UI)` for new-design tests.
- `Category("{Feature Category}")` is a human-readable feature name (e.g., `"Contracts (Inbound) Interface"`).
- Add `Category(Nightly)`, `Category(Acceptance)`, `Category(Prod)`, `Category(Smoke)` ONLY if explicitly specified in the Jira test case in Labels.

Jira label to test category mapping:
- `Regression` ? `Category(Regression)`
- `Nightly` ? `Category(Nightly)`
- `AT_Tests` ? `Category(Acceptance)` (the Jira label `AT_Tests` corresponds to the `Acceptance` category in the TA solution)
- `Prod` ? `Category(Prod)`
- `Smoke` ? `Category(Smoke)`

### Data-Driven Tests

For simple string or int parameterization, use TestCase attributes directly:

    [Test, Retry(2), TestCaseId("ENVI-377372")]
    [Category(Regression), Category("ENVI-377270"), Category(Old_UI), Category("Contracts (Inbound) Interface")]
    [TestCase("ExternalMfgNo")]
    [TestCase("Description")]
    [TestCase("Price")]
    public void SomeDataDrivenODTest(string missingField)
    {
        string contractItemsFile = missingField switch
        {
            "ExternalMfgNo" => FileUpdate.ContractItemsUpdate(contractTier, externalMfgNo: string.Empty),
            "Description" => FileUpdate.ContractItemsUpdate(contractTier, contractDescription: string.Empty),
            "Price" => FileUpdate.ContractItemsUpdate(contractTier, contractPrice: string.Empty),
            _ => throw new ArgumentException("Invalid test case", nameof(missingField))
        };
        // ... rest of test
    }

For complex or boolean inputs, use TestCaseSource with a static method:

    [TestCaseSource(nameof(BoolInput))]
    public void SomeTestWithBoolInput(bool isViaScheduledTask)
    {
        // ...
    }

### Naming Conventions

- Test methods: `{WhatIsTested}{Condition}ODTest` for old design, `NDTest` for new design
- Use PascalCase, be descriptive (e.g., `ArchiveIsMovedToBADSubFolderIfIncorrectFileTypeODTest`)
- Constants: `const string ExpectedMessage = "...";`
- Variables: `var expectedValue = "...";`
- Test class names: `{FeatureName}Tests` (e.g., `ContractsInboundTests`, `HL7InventoryInboundTests`)

### Navigation Patterns

Login and navigate to the first page in a test. This opens the browser, authenticates, and goes to the URL:

    var page = LoginAndGo.To<InterfaceDetailsPage>(WebUrls.ToInterfaceDetails(InterfacePK), DomainAdmin);

Navigate to another page without logging in again (subsequent navigations within the same test):

    var page = Go.To<ContractDetailsPage>(url: WebUrls.ToContractDetails(contractPK));

Navigate with retry for pages that may take time to load:

    var page = GoWithRetry.To<InventoryListPage>(url: WebUrls.ToInventoryList);

Switch to old design mode. Call this on any page that has the old/new UI toggle, AFTER navigation and BEFORE interacting with page elements:

    page.SwitchDesignMode();

Tab navigation on detail pages that have tabs (e.g., Details, Resources):

    var resourcesPage = interfaceDetailsPage.Tabs.ResourcesTab.ClickAndGo();
    var deliveryHistoryPage = resourcesPage.InterfaceDeliveryHistoryLink.ClickAndGo();

Navigate to a typed page from a clickable element:

    var deliveryDetailsPage = deliveryHistoryPage.Deliveries[0].DeliveryStatusTextfield.ClickAndGo<DeliveryDetailsPage>();

### Modal Interaction Pattern

Most interface executions and actions trigger a message modal. Always wait for the modal before asserting:

    var messageModal = interfaceDetailsPage.RunTestButton.ClickAndGo();
    messageModal.MessageOD.Wait(Until.Visible);
    Assert.That(messageModal.MessageOD.Value, Is.EqualTo(ExpectedMessage), ExpectedMessage + " message should appear");
    messageModal.OkButtonOD.ClickAndGo();

### API Data Setup Patterns

Two API user instances are available for creating test data via API:

    DomainAdmin   // Admin-level API operations (create/deactivate contracts, interfaces, etc.)
    SimpleUser    // Standard user API operations (create scheduled tasks, adjustments, etc.)

Create data via a builder pattern:

    var model = new VendorSaveModelBuilder().Build();
    model.VendorPK = DomainAdmin.Vendors.Create(model);
    TestActions.Add(() => DomainAdmin.Vendors.Deactivate(model.VendorPK));

Get existing data by name or number:

    var contractPK = DomainAdmin.Contracts.GetItems().First(_ => _.InternalContractNo.Equals(internalContractNo)).ContractPK;

Generate unique test data using the Generator utility:

    var inventoryNo = Generator.InventoryNo;
    var alphanumeric = Generator.Alphanumeric(22);
    var vendorNo = Generator.VendorNo;
    var vendorName = Generator.VendorName;
    var manufacturerNo = Generator.ManufacturerNo;
    var classification = Generator.Classification;

File preparation and FTP upload for interface tests:

    var filePath = FileUpdate.HL7InboundUpdate(inventoryNo);
    TestActions.Add(() => File.Delete(filePath));
    FtpInterface.UploadInterfaceFile(filePath, InterfaceTypes.HL7Inventory);

Run an interface via API without going through the UI:

    DomainAdmin.Interfacing.GenerateInboundInterface(InterfacePK, InterfaceTypes.ContractsInb);

### Cleanup Pattern

Always register cleanup via `TestActions.Add()` so test data is removed even if the test fails. Register cleanup AFTER confirming the data was actually created:

    // After creating via API
    var model = new SomeModelBuilder().Build();
    model.PK = DomainAdmin.Service.Create(model);
    TestActions.Add(() => DomainAdmin.Service.Deactivate(model.PK));

    // After creating via interface execution (verify it exists first)
    var contractPK = DomainAdmin.Contracts.GetItems().First(_ => _.InternalContractNo.Equals(internalContractNo)).ContractPK;
    TestActions.Add(() => DomainAdmin.Contracts.Deactivate(contractPK));

    // For temporary files
    TestActions.Add(() => File.Delete(zipFilePath));

    // For FTP folders
    FtpInterface.DeleteBadFolder(InterfaceTypes.ContractsInb);

    // For scheduled tasks
    TestActions.Add(() => SimpleUser.Interfacing.DeactivateScheduledTask(InterfacePK, scheduledTaskPK));

### Scheduled Task Pattern

When testing that an interface executes via a scheduled task rather than manual click, create the task in setup and wait for it to fire:

    private static void CreateScheduledTask()
    {
        var startDate = DateTime.Now.AddMinutes(2).ToString("HH:mm:ss");
        var scheduleTaskModel = new InterfaceScheduleModelBuilder("Contracts Inb Interface").Build();
        scheduleTaskModel.ScheduledData["StartTime"] = startDate;
        scheduleTaskModel.ScheduledTaskPK = SimpleUser.Interfacing.CreateSchedule(scheduleTaskModel);
        TestActions.Add(() => SimpleUser.Interfacing.DeactivateScheduledTask(InterfacePK, scheduleTaskModel.ScheduledTaskPK));
    }

In the test, wait for the scheduled task to execute instead of clicking Run Test:

    CreateScheduledTask();
    var interfaceDetailsPage = LoginAndGo.To<InterfaceDetailsPage>(WebUrls.ToInterfaceDetails(InterfacePK), DomainAdmin);
    interfaceDetailsPage.RunTestButton.Wait(Until.Visible);
    interfaceDetailsPage.WaitSeconds(180);
    // Then verify results without clicking Run Test

### Assertion Patterns

Always use NUnit constraint-based assertions. Never use classic Assert.IsTrue or Assert.AreEqual.

Single assertion:

    Assert.That(actual, Is.EqualTo(expected), "Failure message describing what went wrong");

Grouped assertions (when verifying multiple properties of the same result):

    Assert.Multiple(() =>
    {
        Assert.That(actualStartTime.AddHours(8), Is.GreaterThan(expectedTime), "Delivery should be created after test started");
        Assert.That(deliveryStatus, Is.EqualTo("COMPLETED"), "Status should be COMPLETED");
    });

Contains assertion for partial text match:

    Assert.That(deliveryDetailsPage.DetailsTextfield.Value, Does.Contain(expectedDescription), "Details should contain message");

Presence/existence assertions:

    Assert.That(createdContract, Is.Not.Null, "Contract should be created");
    Assert.That(FtpInterface.BadFolderExists(InterfaceTypes.ContractsInb), "BAD folder should be created in FTP");

Numeric comparison:

    Assert.That(FtpInterface.GetFilesQtyInBadFolder(InterfaceTypes.ContractsInb), Is.EqualTo(1), "One archive should be in BAD folder");

---

## PAGE OBJECT RULES

### Page Object Structure for List Pages

When HTML of a list page is provided, create a page object following this exact pattern. The class inherits from `MainPage<_>`, uses nested classes for grid containers, and provides lookup helpers.

    using Atata;

    namespace TestAutomation.WebUI.Business.{Module}.{Feature};

    using _ = SomeListPage;

    public class SomeListPage : MainPage<_>
    {
        [FindByXPath("//envi-button[contains(@params,'txtAddContract')]//a")]
        public Link<_, _> AddContractButton { get; private set; }

        public Search<_> Search { get; private set; }

        public ContractItemsContainer ContractItemsTable { get; private set; }

        [ControlDefinition("div[@id='contracts-list']")]
        public class ContractItemsContainer : Control<_>
        {
            public ControlList<ContractItem, _> ContractItems { get; private set; }

            [ControlDefinition("div[contains(@class, 'row-inner-wrapper')]")]
            public class ContractItem : Control<_>
            {
                [FindByXPath("*[contains(@data-bind, 'text: InternalContractNo')]")]
                public Clickable<_, _> InternalContractNoTextfield { get; private set; }

                [FindByXPath("*[contains(@data-bind, 'text: ContractDescription')]")]
                public Clickable<_, _> ContractDescriptionTextfield { get; private set; }

                [FindByXPath("*[contains(@data-bind, 'text: ContractType')]")]
                public Clickable<_, _> ContractTypeTextfield { get; private set; }

                [FindByXPath("*[contains(@data-bind, 'gridDate: EffectiveDateString')]")]
                public Clickable<_, _> EffectiveDateTextfield { get; private set; }

                [FindByXPath("*[contains(@data-bind, 'gridDate: ExpirationDateString')]")]
                public Clickable<_, _> ExpirationDateTextfield { get; private set; }

                [FindByXPath("*[contains(@data-bind, 'colorfulStatusV3')]")]
                public Clickable<_, _> StatusTextfield { get; private set; }
            }

            public ContractItem GetContractItemByNo(string contractNo) =>
                ContractItems[el => el.InternalContractNoTextfield.Content.Value.Equals(contractNo)];
        }
    }

### Page Object Structure for Detail Pages

Detail pages use `data-test-automation-id` attributes when available. They may inherit from a base page that provides tab navigation.

    using Atata;

    namespace TestAutomation.WebUI.Business.{Module}.{Feature};

    using _ = ContractDetailsPage;

    public class ContractDetailsPage : ContractBasePage<_>
    {
        [FindByXPath("*[@data-test-automation-id='contract-details-tab-edit-btn']")]
        public Button<_, _> EditButton { get; private set; }

        [FindByXPath("span[@data-test-automation-id='contract-details-tab-internal-contract-number-field-text']")]
        public Text<_> InternalContractNoTextfield { get; private set; }

        [FindByXPath("span[@data-test-automation-id='contract-details-tab-gpo-contract-number-field-text']")]
        public Text<_> GPOContractNoTextfield { get; private set; }

        [FindByXPath("span[@data-test-automation-id='contract-details-tab-gpo-name-field-text']")]
        public Text<_> GPOContractNameTextfield { get; private set; }

        [FindByXPath("span[@data-test-automation-id='contract-details-tab-contract-type-field-text']")]
        public Text<_> ContractTypeTextfield { get; private set; }
    }

### Page Object Structure for Base Pages with Tabs

When a detail page has tabs, create a base page class that contains the tab navigation container:

    using Atata;

    namespace TestAutomation.WebUI.Business.{Module}.{Feature};

    public class ContractBasePage<TOwner> : MainPage<TOwner>
        where TOwner : ContractBasePage<TOwner>
    {
        public TabsContainer Tabs { get; private set; }

        [ControlDefinition("*[contains(@class, 'tabs-header')]")]
        public class TabsContainer : Control<TOwner>
        {
            [FindByXPath("*[@data-test-automation-id='details-tab']")]
            public Clickable<ContractDetailsPage, TOwner> DetailsTab { get; private set; }
        }
    }

### Key Identifiers from HTML in Priority Order

When reading HTML to build page objects, use these selectors in this priority:

1. `data-test-automation-id` � preferred, most stable. Use: `[FindByXPath("*[@data-test-automation-id='value']")]`
2. `data-bind="text: PropertyName"` � for grid cell text fields. Use: `[FindByXPath("*[contains(@data-bind, 'text: PropertyName')]")]`
3. `data-bind="gridDate: PropertyName"` � for date fields in grids. Use: `[FindByXPath("*[contains(@data-bind, 'gridDate: PropertyName')]")]`
4. `data-bind="colorfulStatusV3: ..."` � for status fields. Use: `[FindByXPath("*[contains(@data-bind, 'colorfulStatusV3')]")]`
5. `id` attribute � for specific containers. Use: `[ControlDefinition("div[@id='contracts-list']")]`
6. `envi-button params` containing a resource key � for buttons. Use: `[FindByXPath("//envi-button[contains(@params,'txtAddContract')]//a")]`
7. CSS class � last resort. Use: `[FindByCss(".some-class")]` or `[FindByClass("some-class")]`

### Page Object Rules

- All properties use `{ get; private set; }` � never `{ get; set; }`
- Every page object file must have `using _ = PageClassName;` alias at the top
- List pages inherit from `MainPage<_>`
- Detail pages may inherit from a base page (e.g., `ContractBasePage<_>`) that provides tab navigation
- Grid containers use `[ControlDefinition]` with the grid's `id` or distinguishing class
- Grid rows use `[ControlDefinition("div[contains(@class, 'row-inner-wrapper')]")]`
- Always provide a lookup helper method: `GetItemByField(string value) => Items[el => el.Field.Content.Value.Equals(value)];`

---

### Search and Filter Pattern

Use the Search component on list pages to find specific items:

    listPage.Search.Find(searchValue);
    listPage.ItemsTable.GetItemByField(searchValue).Wait(Until.Visible);

To show inactive items on a list page, use the Filter button and checkbox:

    listPage.FilterButton.ClickAndGo();
    listPage.Filter.DisplayInactiveCheckBoxLabel.Wait(Until.Visible);
    listPage.Filter.DisplayInactiveCheckBoxLabel.Click();
    listPage.Filter.ApplyFilterButton.ClickAndGo();

### RefreshPage Pattern

When a page needs to reload after an API change or navigation, call RefreshPage before interacting:

    var detailsPage = Go.To<SomeDetailsPage>(url: WebUrls.ToSomeDetails(pk));
    detailsPage.RefreshPage();
    detailsPage.SomeField.Wait(Until.Visible);

### Edit and Update Pattern on Detail Pages

Some detail pages have an Edit button that switches to edit mode:

    detailsPage.EditButton.Wait(Until.Visible);
    var editPage = detailsPage.EditButton.ClickAndGo();
    editPage.SomeDropdown.Set("New Value");
    editPage.UpdateButton.ClickAndGo();

### TestCaseSource with Enum Types

When tests need to run with different enum values (e.g., interface file types, item types), use TestCaseSource with the enum:

    [TestCaseSource(nameof(InterfaceUploadFileTypesInput))]
    public void SomeTestODTest(InterfaceFileTypes fileType)
    {
        var interfacePK = fileType == InterfaceFileTypes.TXT ? TxtInterfacePK : ExcelInterfacePK;
        var filePath = fileType == InterfaceFileTypes.TXT
            ? FileUpdate.SomeUpdate(properties)
            : ExcelFileUpdate.SomeUpdate(properties);
        // ...
    }

    [TestCaseSource(nameof(StockConsignmentItemTypesInput))]
    public void SomeTestODTest(ItemTypes itemType)
    {
        // ...
    }

### Helper Methods Returning Tuples

When a helper method creates multiple related entities, use a tuple return:

    private static (string PurchaseOrderNo, string InvoiceNo) CreateMatchedInvoice()
    {
        // ... create PO, receipt, invoice, match
        return (poModel.PurchaseOrderNo, invoiceNo);
    }

    // Usage in test:
    var (purchaseOrderNo, invoiceNo) = CreateMatchedInvoice();

### DefaultTestData Constants

Use the DefaultTestData static class for shared environment-specific values that are preconfigured in the test environment:

    DefaultTestData.FacilityName   // e.g., "Facility1"
    DefaultTestData.VendorName     // Default vendor name
    DefaultTestData.LocationName   // Default location name
    DefaultTestData.ManufacturerName // Default manufacturer name

### Collection Assertions with LINQ

When asserting over lists of UI elements, use .ToList() with LINQ expressions:

    Assert.That(list.ToList().All(_ => _.StatusTextfield.Value.Equals("Active")), "All items should be Active");
    Assert.That(list.ToList().Any(_ => _.NameTextfield.Value.Equals(expectedName)), "Item should exist in list");
    Assert.That(list.Count.Value, Is.EqualTo(expectedCount), "List should have expected count");

### Dropdown and Select Pattern

To set a value on a dropdown or select control, use the Set method with the display text:

    editPage.FacilityDropdown.Set("Facility2");
    editPage.SelectLocationDropdown.Set("Location Name");
    editPage.SelectVendorDropdown.Set("Vendor Name");

---

## WHAT NOT TO DO

- Do NOT create page objects without HTML or existing examples � ASK for HTML first
- Do NOT hardcode GUIDs � always resolve via API using `GetList()`, `GetItems()`, etc.
- Do NOT hardcode entity values from Jira test cases � always convert them to dynamic generated values (see Hardcoded Value Conversion Rules)
- Do NOT skip cleanup � always use `TestActions.Add(() => ...)`
- Do NOT use `Thread.Sleep` � use `.Wait(Until.Visible)` or `.WaitSeconds()`
- Do NOT compare strings lexicographically when numeric comparison is needed
- Do NOT fetch sequence numbers AFTER creating data (this causes race conditions)
- Do NOT add test categories (Nightly, Acceptance, Prod, Smoke) unless explicitly specified in the Jira test case. Remember: Jira label `AT_Tests` maps to `Category(Acceptance)`
- Do NOT use `{ get; set; }` in page objects � always use `{ get; private set; }`
- Do NOT forget the `using _ = PageClassName;` alias in page object files
- Do NOT use `Assert.IsTrue` or `Assert.AreEqual` � always use constraint-based `Assert.That`
- Do NOT create helper methods inline in tests � put them as `private static` methods at the bottom of the test class
- Do NOT guess which API service or page element to use � ASK if unsure

---

## HARDCODED VALUE CONVERSION RULES

When a Jira test case contains exact hardcoded values for entities, they MUST be converted to dynamically generated values in the automated test. Tests must be repeatable and not collide with other test runs.

### String/Identifier Values

Any hardcoded string identifier (Contract Tier, Inventory No, Vendor No, etc.) must be replaced with a `Generator` call producing a unique alphanumeric value of up to 20 characters:

| Jira test case value | Generated test code |
|---|---|
| Contract Tier = "11111" | `var contractTier = Generator.Alphanumeric(20);` |
| Inventory No = "INV-001" | `var inventoryNo = Generator.InventoryNo;` |
| Vendor No = "V-12345" | `var vendorNo = Generator.VendorNo;` |
| Manufacturer No = "MFG-99" | `var manufacturerNo = Generator.ManufacturerNo;` |
| Any other unique string = "SomeValue" | `var someValue = Generator.Alphanumeric(20);` |

### Date Values

Hardcoded dates must be converted to dynamic date expressions relative to today's date:

| Jira date context | Generated test code |
|---|---|
| Present/current date (e.g., Effective Date, Created Date, Usage Date = "03/01/2021" or "20210301") | `DateTime.UtcNow` |
| A few days in the future (e.g., Start Date = "20210402") | `DateTime.UtcNow.AddDays(2)` |
| Future date (e.g., Expiration Date, End Date = "03/01/2024" or "20240301") | `DateTime.UtcNow.AddYears(3)` |
| Updated/changed date for re-run scenarios | Use a different offset, e.g., `DateTime.UtcNow.AddYears(5)` or `DateTime.UtcNow.AddDays(2)` |

Format the date according to context:
- For API/file input (yyyyMMdd): `DateTime.UtcNow.ToString("yyyyMMdd")`
- For UI assertion (MM/dd/yyyy): `DateTime.UtcNow.ToString("MM/dd/yyyy")`
- For timestamps (yyyyMMddHHmmss): `DateTime.UtcNow.ToString("yyyyMMddHHmmss")`

### Examples

Jira says: *"Create contract with Tier = '11111', Effective Date = '03/01/2021', Expiration Date = '03/01/2024'"*

Generated code:

    var contractTier = Generator.Alphanumeric(20);
    var effectiveDate = DateTime.UtcNow.ToString("yyyyMMdd");
    var expectedEffectiveDate = DateTime.UtcNow.ToString("MM/dd/yyyy");
    var expirationDate = DateTime.UtcNow.AddYears(3).ToString("yyyyMMdd");
    var expectedExpirationDate = DateTime.UtcNow.AddYears(3).ToString("MM/dd/yyyy");

Jira says: *"Update Expiration Date to '04/02/2021'"*

Generated code:

    var newExpirationDate = DateTime.UtcNow.AddYears(5).ToString("yyyyMMdd");
    var expectedExpirationDate = DateTime.UtcNow.AddYears(5).ToString("MM/dd/yyyy");

---

## JIRA INTEGRATION

### If a Jira MCP Server Is Configured

When a Jira MCP server (e.g., Atlassian MCP or similar) is available and the user provides only a Jira test case ID (e.g., "Generate test for ENVI-377275"), use the MCP tool to fetch the issue details from Jira before generating the test.

Extract the following from the Jira issue:
- **Test Case ID** � from the issue key (e.g., ENVI-377275)
- **Parent Story ID** � from the parent or linked epic/story
- **Title** � from the Summary field
- **Steps and Expected Results** � from the Description field or Zephyr/Xray test steps
- **Test Categories** � look for labels or custom fields that mention Regression, Nightly, AT_Tests, Prod, Smoke. Map `AT_Tests` label to `Category(Acceptance)` in the generated test.

Then apply all the same code generation rules from this document.

### If No MCP Server Is Configured

The user will paste the Jira test case details manually in the chat. Follow the INPUT FORMAT section above.

### Existing Jira API Client (For Test Assertions Only)

The codebase has a built-in Jira REST client accessible via `SimpleUser.Jira` or `DomainAdmin.Jira`. This is used **only within tests** to verify Jira-related functionality (e.g., feedback feature creates a Jira issue). Do NOT use it for fetching test case definitions.

    // Used in tests that verify Jira issue creation
    var issueId = SimpleUser.Jira.GetIssues($"description ~ \"{feedback}\"").Issues.First().Id;
    var issueDetails = SimpleUser.Jira.GetIssueDetails(issueId);
    Assert.That(issueDetails.Fields.Summary, Is.EqualTo(expectedTitle));

---

## EXAMPLE WORKFLOW

1. I paste a Jira test case with steps and expected results.
2. You generate:
   - Page object class (if HTML is provided and a page object does not already exist)
   - Test method following all conventions above
   - Any required private helper methods
3. If anything is unclear (which API service to use, which page element to target, which builder to use), ASK before guessing

### Common Setup Shortcuts (Creator Patterns)

The codebase provides high-level Creator classes that chain multiple API calls. Prefer these over manual step-by-step setup:

    // Create inventory with vendor and location in one chain
    var inventory = SimpleUser.InventoryCreator.Create().WithVendor().WithLocation().Build();
    TestActions.Add(() => SimpleUser.Inventory.Deactivate(inventory.InventoryPK));

    // Create inventory with specific vendor, facility, and item type
    var inventory = SimpleUser.InventoryCreator.Create()
        .WithVendor(vendorPK, facilityPK)
        .WithLocation(DefaultTestData.FacilityName, DefaultTestData.LocationName, ItemTypes.Stock)
        .Build();

    // Create PO with a line item (auto-creates inventory if none specified)
    var poModel = SimpleUser.PurchaseOrderCreator.CreateWithLineItem();
    TestActions.Add(() => SimpleUser.PurchaseOrder.Close(poModel.OrderPK));

    // Create PO with specific inventory and quantity
    var poModel = SimpleUser.PurchaseOrderCreator.CreateWithLineItem(itemNo: inventory.InventoryNo, itemQty: 5);

    // Create PO with new inventory (returns both PO and inventory number)
    var (poModel, inventoryNo) = SimpleUser.PurchaseOrderCreator.CreateWithNewLineItem();

    // Create requisition with auto-setup
    var requisitionModel = SimpleUser.RequisitionCreator.Create();
    TestActions.Add(() => SimpleUser.Requisitions.Cancel(requisitionModel.RequisitionPK));

For complex multi-step workflows (PO ? Receipt ? Invoice ? Match), look for existing examples in the test class closest to the feature being tested. If the Jira test case mentions a precondition like "Matched Invoice exists", ASK which test file to use as reference.

### Common Setup Shortcuts (Creator Patterns) � continued

When a helper method creates multiple related entities, use a tuple return:

    private static (string PurchaseOrderNo, string InvoiceNo) CreateMatchedInvoice()
    {
        // ... create PO, receipt, invoice, match
        return (poModel.PurchaseOrderNo, invoiceNo);
    }

    // Usage in test:
    var (purchaseOrderNo, invoiceNo) = CreateMatchedInvoice();

### DefaultTestData Constants

Use the DefaultTestData static class for shared environment-specific values that are preconfigured in the test environment:

    DefaultTestData.FacilityName   // e.g., "Facility1"
    DefaultTestData.VendorName     // Default vendor name
    DefaultTestData.LocationName   // Default location name
    DefaultTestData.ManufacturerName // Default manufacturer name

### Collection Assertions with LINQ

When asserting over lists of UI elements, use .ToList() with LINQ expressions:

    Assert.That(list.ToList().All(_ => _.StatusTextfield.Value.Equals("Active")), "All items should be Active");
    Assert.That(list.ToList().Any(_ => _.NameTextfield.Value.Equals(expectedName)), "Item should exist in list");
    Assert.That(list.Count.Value, Is.EqualTo(expectedCount), "List should have expected count");

### Dropdown and Select Pattern

To set a value on a dropdown or select control, use the Set method with the display text:

    editPage.FacilityDropdown.Set("Facility2");
    editPage.SelectLocationDropdown.Set("Location Name");
    editPage.SelectVendorDropdown.Set("Vendor Name");

### SwitchDesignMode Pattern

When a test runs against the old UI design, call `SwitchDesignMode()` on the page AFTER navigation and BEFORE interacting with elements. For new-design tests (NDTest suffix), do NOT call `SwitchDesignMode()`.

    // Old design test � call SwitchDesignMode
    var listPage = Go.To<ContractListPage>(url: WebUrls.ToContractsList);
    listPage.SwitchDesignMode();
    listPage.SomeElement.Wait(Until.Visible);

    // New design test � no SwitchDesignMode call
    var listPage = LoginAndGo.To<ReceivingListPage>(WebUrls.ToReceivingList, SimpleUser);
    listPage.SwitchDesignMode();  // some ND tests still call this to toggle to NEW design
    listPage.SomeElement.Wait(Until.Visible);

### Retry and Wait Patterns

Use `Retry.Exponential` for polling server-side state changes (e.g., report generation, interface processing):

    Retry.Exponential(Sizes.RepeatActionTimes + 3, () =>
        reportsViewerPage.GetReport(ReportName).Status.Value.Equals(ReportStatuses.Ready.Description().ToUpper()));

Use `Retry.Exponential<TException>` when the operation may throw during polling:

    Retry.Exponential<InvalidOperationException>(Sizes.RepeatActionTimes, () =>
        reportFile = directory.GetFiles(pattern).Last(_ => !_.FullName.Contains(".crdownload")));

Use `.WaitSeconds(N)` only for time-based waits like scheduled task execution (prefer `.Wait(Until.Visible)` for element waits):

    interfaceDetailsPage.WaitSeconds(180); // Wait for scheduled task to fire

### Report Download and Verification Pattern

When tests download and verify report files, follow this pattern:

    // Clean up leftover report files before test
    var expectedReportFileName = "*" + ReportName.Replace(" ", "") + "Report_" + "*";
    var directory = new DirectoryInfo(DriverFactory.DownloadDirectory);
    var junk = directory.GetFiles(expectedReportFileName);
    if (junk.Length > 0)
        foreach (var item in junk)
            File.Delete(item.FullName);

    // After generating report, wait for download and verify
    FileInfo reportFile = new(ReportName);
    Retry.Exponential<InvalidOperationException>(Sizes.RepeatActionTimes, () =>
        reportFile = directory.GetFiles(expectedReportFileName).Last(_ => !_.FullName.Contains(".crdownload")));

    TestActions.Add(() => File.Delete(reportFile.FullName));
    Assert.That(reportFile, Is.Not.Null, "There should be generated report file: " + ReportName);

For Excel report content verification, use `ExcelGridParser`:

    var parser = new ExcelGridParser(reportFile.FullName);
    var rows = parser.WhereContains("Inventory Number", inventoryNo);
    Assert.That(rows.Any(_ => _.ContainsValue(usageNo)), "Report should contain expected row");

### BoolInput TestCaseSource for Manual/Scheduled Execution

When a test needs to run both via manual click and via scheduled task, use `BoolInput`:

    [TestCaseSource(nameof(BoolInput))]
    public void SomeTestODTest(bool isViaScheduledTask)
    {
        // Setup
        if (isViaScheduledTask)
            CreateScheduledTask();

        // Execution
        if (isViaScheduledTask)
            interfaceDetailsPage.WaitSeconds(180);
        else
        {
            var messageModal = interfaceDetailsPage.RunTestButton.ClickAndGo();
            messageModal.MessageOD.Wait(Until.Visible);
            Assert.That(messageModal.MessageOD.Value, Is.EqualTo(ExpectedMessage));
            messageModal.OkButtonOD.ClickAndGo();
        }
    }

### TwoDdtIndexesInput for DDT Index Parameterization

When a test runs multiple data variations (e.g., line items vs free-form items, or Sent vs Credited statuses), use `TwoDdtIndexesInput`:

    [TestCaseSource(nameof(TwoDdtIndexesInput))]
    public void SomeTestODTest(int ddtIndex)
    {
        if (ddtIndex == 1)
        {
            // Variation 1: add items from inventory
        }
        else
        {
            // Variation 2: add free-form items
        }
    }

### User Security Setup Pattern

When tests need to configure user restrictions (facility, department, vendor), use `DomainAdmin.UserOrganizations` and `DomainAdmin.UserSecurities` and ONLY modify SimpleUserTerpila:

    var terpilaUserPK = SimpleUserTerpila.UserPK;
    var userOrgFromList = DomainAdmin.UserOrganizations.GetList(terpilaUserPK).First();
    var userOrgDetails = DomainAdmin.UserOrganizations.GetDetails(terpilaUserPK, userOrgFromList.OrganizationPK);

    var updateOrgModel = new UserOrganizationUpdateModelBuilder(userOrgDetails).Build();
    updateOrgModel.RestrictFacilities = true;
    updateOrgModel.RestrictDepartments = true;
    DomainAdmin.UserOrganizations.Update(updateOrgModel);
    var userOrgPK = updateOrgModel.UserOrganizationsPK;

    // Always register cleanup to restore original settings
    TestActions.Add(() =>
    {
        var restoreModel = new UserOrganizationUpdateModelBuilder(userOrgDetails).Build();
        restoreModel.RestrictFacilities = false;
        restoreModel.RestrictDepartments = false;
        DomainAdmin.UserOrganizations.Update(restoreModel);
    });

    DomainAdmin.UserSecurities.AddFacility(userOrgPK, facilityPK);
    TestActions.Add(() => DomainAdmin.UserSecurities.DeleteFacility(userOrgPK, facilityPK));
    DomainAdmin.UserSecurities.AddDepartment(userOrgPK, departmentPK);
    TestActions.Add(() => DomainAdmin.UserSecurities.DeleteDepartment(userOrgPK, departmentPK));
