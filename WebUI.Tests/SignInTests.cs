using Microsoft.Playwright;
using Structure.UI.PajeObject;

namespace WebUI.Tests
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class SignInTests : PageTest
    {
        [Test]
        public async Task CandidateSelection_EnablesEmailPasswordAndContinue()
        {
            // recordings directory (will be committed or uploaded by CI)
            var recordingsDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "recordings");
            await using var context = await Browser.NewContextAsync(PlaywrightConfig.CreateContextOptions(recordingsDir));
            var page = await context.NewPageAsync();
            await page.GotoAsync(PlaywrightConfig.BaseUrl);

            // Use HomePage page object (pass the local page)
            var home = new HomePage(page);
            Assert.IsTrue(await home.IsSubmitButtonVisibleAsync(), "Submit CTA not visible on homepage");
            await home.ClickSubmitButtonAsync();

            // Use SignInPage page object to select candidate and wait for fields
            var signIn = new SignInPage(page);
            await signIn.SelectCandidateAsync();

            // Verify visibility and enabled state of fields
            Assert.IsTrue(await signIn.IsEmailVisibleAsync(), "Email input is not visible");
            Assert.IsTrue(await signIn.IsPasswordVisibleAsync(), "Password input is not visible");
            Assert.IsTrue(await signIn.IsEmailEnabledAsync(), "Email input is not enabled");
            Assert.IsTrue(await signIn.IsPasswordEnabledAsync(), "Password input is not enabled");

            // Verify Continue button is enabled
            var continueButton = page.Locator("button.js-send-btn");
            Assert.IsTrue(await continueButton.IsEnabledAsync(), "Continue button is not enabled");

            // Close context to flush and save the video file
            await context.CloseAsync();

            // Get generated video path (Playwright writes the file when context closes)
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var savedPath = await page.Video.PathAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Move to friendly filename
            var dest = Path.Combine(recordingsDir, $"SignInTest-{DateTime.Now:yyyyMMdd-HHmmss}.webm");
            File.Move(savedPath, dest, overwrite: true);

            // Attach to NUnit test results (optional)
            TestContext.AddTestAttachment(dest, "Playwright recording");
        }
    }
}
