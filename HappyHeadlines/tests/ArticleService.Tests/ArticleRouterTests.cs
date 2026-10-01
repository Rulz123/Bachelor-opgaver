using ArticleService;
using Xunit;

namespace ArticleService.Tests;

public class ArticleRouterTests
{
    [Theory]
    [InlineData("Europe", "europe")]
    [InlineData("Asia", "asia")]
    [InlineData("Africa", "africa")]
    [InlineData("North America", "north-america")]
    [InlineData("South America", "south-america")]
    [InlineData("Australia/Oceania", "australia-oceania")]
    [InlineData("Antarctica", "antarctica")]
    public void Routes_continent_article_to_matching_database(string continent, string expected)
    {
        Assert.Equal(expected, ArticleRouter.GetDatabaseKey(false, continent));
    }

    [Fact]
    public void Routes_global_article_to_global_database()
    {
        Assert.Equal("global", ArticleRouter.GetDatabaseKey(true, null));
    }

    [Fact]
    public void Rejects_unknown_continent()
    {
        Assert.Throws<ArgumentException>(() => ArticleRouter.GetDatabaseKey(false, "Atlantis"));
    }
}
