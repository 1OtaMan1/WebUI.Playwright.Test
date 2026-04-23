using Microsoft.Playwright;

namespace Structure.UI.PajeObject
{
    public class HomePage(IPage page)
    {
        private readonly IPage _page = page;
        private ILocator SubmitButton => _page.Locator("a:has-text(\"Відправити Джина на пошуки\")");

        public async Task ClickSubmitButtonAsync()
        {
            await Task.WhenAll(
                _page.WaitForURLAsync("**/signup*", new PageWaitForURLOptions { Timeout = 5000 }),
                SubmitButton.ClickAsync()
            );
        }

        public Task<string> GetSubmitButtonTextAsync() => SubmitButton.InnerTextAsync();

        public Task<bool> IsSubmitButtonVisibleAsync() => SubmitButton.IsVisibleAsync();
    }
}
