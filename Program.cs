using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
var url = GetNamedValue(args, "--url") ?? positional.ElementAtOrDefault(0) ?? "https://app.cloudqa.io/home/AutomationPracticeForm";
var firstNameLabel = GetNamedValue(args, "--first-name-label") ?? "First Name";
var genderLabel = GetNamedValue(args, "--gender-label") ?? "Gender";
var stateLabel = GetNamedValue(args, "--state-label") ?? "State";
var firstName = GetNamedValue(args, "--first-name") ?? positional.ElementAtOrDefault(1) ?? "Hriday";
var gender = GetNamedValue(args, "--gender") ?? positional.ElementAtOrDefault(2) ?? "Male";
var state = GetNamedValue(args, "--state") ?? positional.ElementAtOrDefault(3) ?? "India";

using var driver = new ChromeDriver();
driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
driver.Navigate().GoToUrl(url);

var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
wait.IgnoreExceptionTypes(typeof(NoSuchElementException));

var firstNameBox = wait.Until(_ => FindByLabel(driver, firstNameLabel, "text"));
if (firstNameBox is null)
    throw new InvalidOperationException($"The '{firstNameLabel}' field was not found.");

firstNameBox.Clear();
firstNameBox.SendKeys(firstName);

var genderOption = wait.Until(_ => FindOptionByGroupLabel(driver, genderLabel, gender));
if (genderOption is null)
    throw new InvalidOperationException($"Gender option '{gender}' was not found under the '{genderLabel}' group.");

genderOption.Click();

var stateControl = wait.Until(_ => FindByLabel(driver, stateLabel, "select"));
if (stateControl is null)
    throw new InvalidOperationException($"The '{stateLabel}' dropdown was not found.");

var selectState = new SelectElement(stateControl);
selectState.SelectByText(state);

Console.WriteLine($"First Name: {firstNameBox.GetDomProperty("value")}");
Console.WriteLine($"Gender: {gender} (selected)");
Console.WriteLine($"State: {selectState.SelectedOption.Text}");
Console.WriteLine("The browser will remain open. Press Enter to close it.");
Console.ReadLine();

static string? GetNamedValue(string[] args, string optionName)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            return args[i + 1];
    }

    return null;
}

static IWebElement? FindByLabel(IWebDriver driver, string label, string kind)
{
    foreach (var candidate in GetLabelCandidates(label))
    {
        foreach (var match in driver.FindElements(By.XPath($"//label[normalize-space(.) = {XPathLiteral(candidate)}] | //legend[normalize-space(.) = {XPathLiteral(candidate)}] | //*[@aria-label = {XPathLiteral(candidate)}] | //*[@name = {XPathLiteral(candidate)}] | //*[@id = {XPathLiteral(candidate)}]")))
        {
            var resolved = ResolveControl(driver, match, kind);
            if (resolved is not null)
                return resolved;
        }
    }

    return null;
}

static IWebElement? FindOptionByGroupLabel(IWebDriver driver, string label, string option)
{
    foreach (var candidate in GetLabelCandidates(label))
    {
        var groupMatches = driver.FindElements(By.XPath($"//label[normalize-space(.) = {XPathLiteral(candidate)}] | //legend[normalize-space(.) = {XPathLiteral(candidate)}] | //*[@aria-label = {XPathLiteral(candidate)}]"));

        foreach (var group in groupMatches)
        {
            var scope = group.FindElement(By.XPath("./ancestor::*[self::div or self::fieldset or self::form][1]"));
            var radios = scope.FindElements(By.XPath($".//input[@type='radio' and (normalize-space(@value) = {XPathLiteral(option)} or normalize-space(@aria-label) = {XPathLiteral(option)} or normalize-space(../label) = {XPathLiteral(option)} or normalize-space(following-sibling::label) = {XPathLiteral(option)})]"));
            if (radios.Count == 0)
            {
                radios = scope.FindElements(By.XPath($".//input[@type='radio' and normalize-space(@value) = {XPathLiteral(option)}]"));
            }

            var visible = radios.FirstOrDefault(r => r.Displayed);
            if (visible is not null)
                return visible;
        }
    }

    throw new InvalidOperationException($"Could not find radio option '{option}' under the '{label}' group.");
}

static IWebElement? ResolveControl(IWebDriver driver, IWebElement labelElement, string kind)
{
    var forId = labelElement.GetDomAttribute("for");
    if (!string.IsNullOrEmpty(forId))
    {
        var target = driver.FindElements(By.Id(forId)).FirstOrDefault();
        if (target is not null && MatchesKind(target, kind))
            return target;
    }

    var scope = labelElement.FindElement(By.XPath("./ancestor::*[self::div or self::fieldset or self::form][1]"));
    var controls = scope.FindElements(By.XPath(".//input | .//textarea | .//select"));
    foreach (var control in controls)
    {
        if (control.Displayed && MatchesKind(control, kind))
            return control;
    }

    return null;
}

static IEnumerable<string> GetLabelCandidates(string label)
{
    var normalized = label.Trim();
    if (string.IsNullOrWhiteSpace(normalized))
        yield break;

    yield return normalized;
    yield return normalized.Replace(" ", "");
    yield return normalized.Replace("-", " ");
    yield return normalized.Replace("_", " ");
    yield return System.Text.RegularExpressions.Regex.Replace(normalized, "\\s+", " ");
}

static bool MatchesKind(IWebElement element, string kind)
{
    var tag = element.TagName.ToLowerInvariant();
    var type = (element.GetDomAttribute("type") ?? string.Empty).ToLowerInvariant();

    return kind switch
    {
        "text" => tag == "input" && (string.IsNullOrEmpty(type) || type is "text" or "search" or "email") || tag == "textarea",
        "select" => tag == "select" || (tag == "input" && type == "select"),
        "radio" => tag == "input" && type == "radio",
        _ => false
    };
}

static string XPathLiteral(string text) => "'" + text.Replace("'", "&apos;") + "'";