using SemaphURL.Models;
using SemaphURL.Services;
using Xunit;

namespace SemaphURL.Tests;

/// <summary>
/// Unit tests for PatternMatcher - URL pattern matching logic
/// </summary>
public class PatternMatcherTests
{
    #region Basic Patterns - DomainContains

    [Theory]
    [InlineData("https://youtube.com/watch?v=123", "youtube", true)]
    [InlineData("https://www.youtube.com/watch", "youtube", true)]
    [InlineData("https://music.youtube.com", "youtube", true)]
    [InlineData("https://google.com", "youtube", false)]
    [InlineData("https://notyoutube.com", "youtube", true)] // Contains "youtube"
    [InlineData("https://YOUTUBE.COM", "youtube", true)] // Case insensitive
    [InlineData("https://youtube.com", "YOUTUBE", true)] // Case insensitive pattern
    public void DomainContains_ShouldMatchCorrectly(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.DomainContains, pattern);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Basic Patterns - DomainEquals

    [Theory]
    [InlineData("https://github.com/user/repo", "github.com", true)]
    [InlineData("https://github.com", "github.com", true)]
    [InlineData("https://www.github.com", "github.com", false)] // www prefix makes it different
    [InlineData("https://api.github.com", "github.com", false)] // subdomain
    [InlineData("https://github.com", "GITHUB.COM", true)] // Case insensitive
    [InlineData("https://github.company.com", "github.com", false)]
    public void DomainEquals_ShouldMatchExactly(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.DomainEquals, pattern);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Basic Patterns - UrlContains

    [Theory]
    [InlineData("https://example.com/api/v1/users", "/api/", true)]
    [InlineData("https://example.com/swagger/index.html", "swagger", true)]
    [InlineData("https://example.com/graphql", "graphql", true)]
    [InlineData("https://example.com/home", "api", false)]
    [InlineData("https://example.com/API/users", "api", true)] // Case insensitive
    [InlineData("https://youtube.com/watch?v=abc123", "watch?v=", true)]
    public void UrlContains_ShouldMatchAnywhere(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.UrlContains, pattern);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Advanced Patterns - DomainStartsWith

    [Theory]
    [InlineData("https://mail.google.com", "mail.", true)]
    [InlineData("https://calendar.google.com", "calendar.", true)]
    [InlineData("https://docs.google.com", "docs.", true)]
    [InlineData("https://google.com", "mail.", false)]
    [InlineData("https://gmail.com", "mail.", false)] // Not a prefix
    [InlineData("https://MAIL.google.com", "mail.", true)] // Case insensitive
    public void DomainStartsWith_ShouldMatchPrefix(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.DomainStartsWith, pattern);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Advanced Patterns - DomainEndsWith

    [Theory]
    [InlineData("https://harvard.edu", ".edu", true)]
    [InlineData("https://mit.edu", ".edu", true)]
    [InlineData("https://stanford.edu", ".edu", true)]
    [InlineData("https://education.com", ".edu", false)] // Not a suffix
    [InlineData("https://example.co.uk", ".uk", true)]
    [InlineData("https://example.CO.UK", ".uk", true)] // Case insensitive
    public void DomainEndsWith_ShouldMatchSuffix(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.DomainEndsWith, pattern);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Advanced Patterns - Regex

    [Theory]
    [InlineData("https://docs.google.com/document/d/123", @"^https://docs\.google\.com", true)]
    [InlineData("https://youtube.com/watch?v=abc123", @"youtube\.com/watch\?v=", true)]
    [InlineData("https://example.com", @"^https://example\.com$", true)]
    [InlineData("http://example.com", @"^https://", false)]
    [InlineData("https://subdomain.example.com", @"^https://[a-z]+\.example\.com", true)]
    [InlineData("https://123.example.com", @"^https://[a-z]+\.example\.com", false)] // Numbers don't match [a-z]+
    public void Regex_ShouldMatchPattern(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.Regex, pattern);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Regex_InvalidPattern_ShouldReturnFalse()
    {
        var uri = new Uri("https://example.com");
        var result = PatternMatcher.Matches(uri, "https://example.com", PatternType.Regex, "[invalid(regex");
        Assert.False(result);
    }

    [Fact]
    public void Regex_CaseInsensitive_ShouldMatch()
    {
        var uri = new Uri("https://EXAMPLE.COM/PATH");
        var result = PatternMatcher.Matches(uri, "https://EXAMPLE.COM/PATH", PatternType.Regex, "example\\.com");
        Assert.True(result);
    }

    #endregion

    #region Developer Patterns - HostPort

    [Theory]
    [InlineData("http://localhost:3000/app", "localhost:3000", true)]
    [InlineData("http://localhost:3000", "localhost:3000", true)]
    [InlineData("http://127.0.0.1:3000", "127.0.0.1:3000", true)]
    [InlineData("http://localhost:4200", "localhost:3000", false)]
    [InlineData("http://localhost:3000", "LOCALHOST:3000", true)] // Case insensitive
    [InlineData("http://localhost:3000/api/users", "localhost:3000", true)]
    [InlineData("http://192.168.1.100:8080", "192.168.1.100:8080", true)]
    public void HostPort_ShouldMatchExactHostAndPort(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.HostPort, pattern);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void HostPort_DefaultHttpPort_ShouldBe80()
    {
        // http://localhost without explicit port uses port 80
        var uri = new Uri("http://localhost/api");
        var result = PatternMatcher.Matches(uri, "http://localhost/api", PatternType.HostPort, "localhost:80");
        Assert.True(result);
    }

    [Fact]
    public void HostPort_DefaultHttpsPort_ShouldBe443()
    {
        // https://example.com without explicit port uses port 443
        var uri = new Uri("https://example.com/api");
        var result = PatternMatcher.Matches(uri, "https://example.com/api", PatternType.HostPort, "example.com:443");
        Assert.True(result);
    }

    #endregion

    #region Developer Patterns - PortEquals

    [Theory]
    [InlineData("http://localhost:3000", "3000", true)]
    [InlineData("http://127.0.0.1:3000", "3000", true)]
    [InlineData("http://192.168.1.1:3000", "3000", true)]
    [InlineData("http://myapp.local:3000", "3000", true)]
    [InlineData("http://localhost:4200", "3000", false)]
    [InlineData("http://localhost:30000", "3000", false)] // Different port
    public void PortEquals_ShouldMatchAnyHostWithPort(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.PortEquals, pattern);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PortEquals_DefaultHttpPort_ShouldMatch80()
    {
        var uri = new Uri("http://localhost/api");
        var result = PatternMatcher.Matches(uri, "http://localhost/api", PatternType.PortEquals, "80");
        Assert.True(result);
    }

    [Fact]
    public void PortEquals_DefaultHttpsPort_ShouldMatch443()
    {
        var uri = new Uri("https://example.com/api");
        var result = PatternMatcher.Matches(uri, "https://example.com/api", PatternType.PortEquals, "443");
        Assert.True(result);
    }

    #endregion

    #region Developer Patterns - PortRange

    [Theory]
    [InlineData("http://localhost:3000", "3000-3999", true)]  // Lower bound
    [InlineData("http://localhost:3999", "3000-3999", true)]  // Upper bound
    [InlineData("http://localhost:3500", "3000-3999", true)]  // Middle
    [InlineData("http://localhost:2999", "3000-3999", false)] // Below range
    [InlineData("http://localhost:4000", "3000-3999", false)] // Above range
    public void PortRange_ShouldMatchPortsInRange(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.PortRange, pattern);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("http://localhost:5173", "3000-9999", true)]  // Vite default
    [InlineData("http://localhost:4200", "3000-9999", true)]  // Angular default
    [InlineData("http://localhost:8080", "3000-9999", true)]  // Vue/Spring default
    [InlineData("http://localhost:3000", "3000-9999", true)]  // React CRA default
    [InlineData("http://localhost:6006", "3000-9999", true)]  // Storybook default
    public void PortRange_ShouldMatchCommonDevPorts(string url, string pattern, bool expected)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.PortRange, pattern);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("http://localhost:3000", "invalid")]
    [InlineData("http://localhost:3000", "3000-")]
    [InlineData("http://localhost:3000", "-3999")]
    [InlineData("http://localhost:3000", "abc-def")]
    [InlineData("http://localhost:3000", "3000")]  // No dash
    [InlineData("http://localhost:3000", "3000-3999-4000")]  // Too many parts
    public void PortRange_InvalidPattern_ShouldReturnFalse(string url, string pattern)
    {
        var uri = new Uri(url);
        var result = PatternMatcher.Matches(uri, url, PatternType.PortRange, pattern);
        Assert.False(result);
    }

    [Fact]
    public void PortRange_WithSpaces_ShouldTrimAndMatch()
    {
        var uri = new Uri("http://localhost:3500");
        var result = PatternMatcher.Matches(uri, "http://localhost:3500", PatternType.PortRange, " 3000 - 3999 ");
        Assert.True(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void NullUri_ShouldReturnFalse_ForDomainPatterns()
    {
        Assert.False(PatternMatcher.Matches(null, "invalid", PatternType.DomainContains, "example"));
        Assert.False(PatternMatcher.Matches(null, "invalid", PatternType.DomainEquals, "example"));
        Assert.False(PatternMatcher.Matches(null, "invalid", PatternType.DomainStartsWith, "example"));
        Assert.False(PatternMatcher.Matches(null, "invalid", PatternType.DomainEndsWith, "example"));
    }

    [Fact]
    public void NullUri_ShouldReturnFalse_ForPortPatterns()
    {
        Assert.False(PatternMatcher.Matches(null, "invalid", PatternType.HostPort, "localhost:3000"));
        Assert.False(PatternMatcher.Matches(null, "invalid", PatternType.PortEquals, "3000"));
        Assert.False(PatternMatcher.Matches(null, "invalid", PatternType.PortRange, "3000-3999"));
    }

    [Fact]
    public void NullUri_UrlContains_ShouldStillWork()
    {
        // UrlContains works on the raw string, not the Uri
        var result = PatternMatcher.Matches(null, "contains-the-pattern", PatternType.UrlContains, "pattern");
        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespacePattern_ShouldReturnFalse(string pattern)
    {
        var uri = new Uri("https://example.com");
        
        Assert.False(PatternMatcher.Matches(uri, "https://example.com", PatternType.DomainContains, pattern));
        Assert.False(PatternMatcher.Matches(uri, "https://example.com", PatternType.DomainEquals, pattern));
        Assert.False(PatternMatcher.Matches(uri, "https://example.com", PatternType.UrlContains, pattern));
        Assert.False(PatternMatcher.Matches(uri, "https://example.com", PatternType.Regex, pattern));
        Assert.False(PatternMatcher.Matches(uri, "https://example.com", PatternType.HostPort, pattern));
        Assert.False(PatternMatcher.Matches(uri, "https://example.com", PatternType.PortEquals, pattern));
        Assert.False(PatternMatcher.Matches(uri, "https://example.com", PatternType.PortRange, pattern));
    }

    [Fact]
    public void UnknownPatternType_ShouldReturnFalse()
    {
        var uri = new Uri("https://example.com");
        var result = PatternMatcher.Matches(uri, "https://example.com", (PatternType)999, "pattern");
        Assert.False(result);
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void Scenario_RouteWorkSitesToChrome()
    {
        var workUrls = new[]
        {
            "https://company.slack.com/messages",
            "https://app.company.com/dashboard",
            "https://jira.company.com/browse/PROJ-123"
        };

        foreach (var url in workUrls)
        {
            var uri = new Uri(url);
            var result = PatternMatcher.Matches(uri, url, PatternType.DomainContains, "company");
            Assert.True(result, $"Should match work URL: {url}");
        }
    }

    [Fact]
    public void Scenario_RouteYouTubeToFirefox()
    {
        var youtubeUrls = new[]
        {
            "https://youtube.com/watch?v=abc123",
            "https://www.youtube.com/channel/UC123",
            "https://music.youtube.com/playlist?list=123",
            "https://youtu.be/abc123" // Short URL - won't match DomainContains "youtube"
        };

        foreach (var url in youtubeUrls.Take(3))
        {
            var uri = new Uri(url);
            var result = PatternMatcher.Matches(uri, url, PatternType.DomainContains, "youtube");
            Assert.True(result, $"Should match YouTube URL: {url}");
        }
    }

    [Fact]
    public void Scenario_RouteLocalDevToDifferentBrowsers()
    {
        // React app on port 3000
        var reactUri = new Uri("http://localhost:3000/app");
        Assert.True(PatternMatcher.Matches(reactUri, "http://localhost:3000/app", PatternType.HostPort, "localhost:3000"));

        // Angular app on port 4200
        var angularUri = new Uri("http://localhost:4200/dashboard");
        Assert.True(PatternMatcher.Matches(angularUri, "http://localhost:4200/dashboard", PatternType.HostPort, "localhost:4200"));

        // API server on port 5000
        var apiUri = new Uri("http://localhost:5000/api/users");
        Assert.True(PatternMatcher.Matches(apiUri, "http://localhost:5000/api/users", PatternType.HostPort, "localhost:5000"));

        // All dev ports in range
        Assert.True(PatternMatcher.Matches(reactUri, "http://localhost:3000/app", PatternType.PortRange, "3000-9999"));
        Assert.True(PatternMatcher.Matches(angularUri, "http://localhost:4200/dashboard", PatternType.PortRange, "3000-9999"));
        Assert.True(PatternMatcher.Matches(apiUri, "http://localhost:5000/api/users", PatternType.PortRange, "3000-9999"));
    }

    [Fact]
    public void Scenario_RouteEducationalSites()
    {
        var eduUrls = new[]
        {
            "https://coursera.org/learn/course",
            "https://edx.org/course/cs50",
            "https://mit.edu/research"
        };

        // .edu domain
        var uri = new Uri("https://mit.edu/research");
        Assert.True(PatternMatcher.Matches(uri, "https://mit.edu/research", PatternType.DomainEndsWith, ".edu"));

        // .org domains
        var courseraUri = new Uri("https://coursera.org/learn/course");
        Assert.True(PatternMatcher.Matches(courseraUri, "https://coursera.org/learn/course", PatternType.DomainEndsWith, ".org"));
    }

    #endregion
}

