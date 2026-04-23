using Microsoft.Playwright;

namespace Structure.UI.PajeObject
{
    public class HomePage(IPage page)
    {
        private readonly IPage _page = page;

        // Use a resilient selector that matches links/buttons that navigate to signup
        private ILocator SubmitButton => _page.Locator("a[href^='/signup'])");

        public async Task ClickSubmitButtonAsync()
        {
            // Ensure the CTA is visible before attempting to click, then wait for navigation
            await SubmitButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

            await Task.WhenAll(
                _page.WaitForURLAsync("**/signup*", new PageWaitForURLOptions { Timeout = 10000 }),
                SubmitButton.ClickAsync()
            );
        }

        public Task<string> GetSubmitButtonTextAsync() => SubmitButton.InnerTextAsync();

        public Task<bool> IsSubmitButtonVisibleAsync() => SubmitButton.IsVisibleAsync();
    }
}
