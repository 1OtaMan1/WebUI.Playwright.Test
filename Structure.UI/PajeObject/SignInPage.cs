using Microsoft.Playwright;

namespace Structure.UI.PajeObject
{
    public class SignInPage(IPage page)
    {
        private readonly IPage _page = page;

        private ILocator CandidateRadio => _page.Locator("input#account_type_candidate");
        private ILocator EmailInput => _page.Locator("input#email");
        private ILocator PasswordInput => _page.Locator("input#password");
        private ILocator ContinueButton => _page.Locator("button.js-send-btn");

        public async Task SelectCandidateAsync()
        {
            // Clicking the input itself can be intercepted by the label overlay in the UI.
            // Click the label that corresponds to the candidate radio instead.
            var candidateLabel = _page.Locator("label[for=\"account_type_candidate\"]");
            await candidateLabel.ClickAsync();

            // Wait until the radio becomes checked
            await WaitForCheckedAsync(CandidateRadio, 5000);

            await _page.WaitForSelectorAsync("input#email", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await _page.WaitForSelectorAsync("input#password", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

            await WaitForEnabledAsync(EmailInput, 5000);
            await WaitForEnabledAsync(PasswordInput, 5000);
        }

        public Task<bool> IsEmailVisibleAsync() => EmailInput.IsVisibleAsync();
        public Task<bool> IsPasswordVisibleAsync() => PasswordInput.IsVisibleAsync();
        public Task<bool> IsEmailEnabledAsync() => EmailInput.IsEnabledAsync();
        public Task<bool> IsPasswordEnabledAsync() => PasswordInput.IsEnabledAsync();
        public Task ClickContinueAsync() => ContinueButton.ClickAsync();

        public async Task SelectCandidateAndContinueAsync()
        {
            await SelectCandidateAsync();
            await ClickContinueAsync();
        }

        private static async Task WaitForEnabledAsync(ILocator locator, int timeoutMs = 5000)
        {
            var start = DateTime.UtcNow;
            while (true)
            {
                if (await locator.IsEnabledAsync())
                    return;

                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                    throw new TimeoutException($"Timed out waiting for locator to become enabled: {locator.Description ?? "<no description>"}");

                await Task.Delay(100);
            }
        }

        private static async Task WaitForCheckedAsync(ILocator locator, int timeoutMs = 5000)
        {
            var start = DateTime.UtcNow;
            while (true)
            {
                if (await locator.IsCheckedAsync())
                    return;

                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                    throw new TimeoutException($"Timed out waiting for locator to become checked: {locator.Description ?? "<no description>"}");

                await Task.Delay(100);
            }
        }
    }
}
