# Automation Practice Form (C# + Selenium)

This console sample fills **First Name**, selects a **Gender** radio option, and chooses a **State** dropdown option. It resolves controls from visible label text and nearby semantic groups, rather than relying on the current `id`, `name`, `class`, or field order. If a label is ambiguous, it stops with an error rather than guessing.

## Run

Requires the .NET 8 SDK and Google Chrome. Selenium Manager downloads/resolves ChromeDriver as needed.

```powershell
dotnet run -- "https://app.cloudqa.io/home/AutomationPracticeForm" "Hriday" "Male" "India"
```

All four values are configurable. For example, to target a modified copy:

```powershell
dotnet run -- "https://test.example/form-v2" "Poorav" "Male" "Pakistan"
```

The dropdown argument must match an available option's visible text. The program intentionally does not submit the form.

## Locator strategy and limits

- Textbox and dropdown: exact visible label text, associated `<label>`/ARIA relationship where available, then a small nearby control group.
- Gender: the radio group near the **Gender** label, then the radio option's associated label or nearby visible option text.
- Fields may move, and their `id`, `name`, and `class` may change without affecting these locators.
- The supplied page contains repeated forms in frames and shadow roots. This compact example operates in the current top-level document; it deliberately errors if label matching is ambiguous. For a variant that moves the target form into an iframe or shadow root, add an explicit form/iframe/shadow-root scope rather than selecting the first global match.
- No locator can survive arbitrary changes to the user's visible terminology or removal of accessible relationships and nearby grouping. Keep labels stable or supply configurable label/option text when testing such variants.
