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
            // Open homepage
            await Page.GotoAsync("https://djinni.co/");

            // Use HomePage page object to click CTA that goes to signup
            var home = new HomePage(Page);
            Assert.IsTrue(await home.IsSubmitButtonVisibleAsync(), "Submit CTA not visible on homepage");
            await home.ClickSubmitButtonAsync();

            // Use SignInPage page object to select candidate and wait for fields
            var signIn = new SignInPage(Page);
            await signIn.SelectCandidateAsync();

            // Verify visibility and enabled state of fields
            Assert.IsTrue(await signIn.IsEmailVisibleAsync(), "Email input is not visible");
            Assert.IsTrue(await signIn.IsPasswordVisibleAsync(), "Password input is not visible");
            Assert.IsTrue(await signIn.IsEmailEnabledAsync(), "Email input is not enabled");
            Assert.IsTrue(await signIn.IsPasswordEnabledAsync(), "Password input is not enabled");

            // Verify Continue button is enabled
            var continueButton = Page.Locator("button.js-send-btn");
            Assert.IsTrue(await continueButton.IsEnabledAsync(), "Continue button is not enabled");
        }
    }
}
