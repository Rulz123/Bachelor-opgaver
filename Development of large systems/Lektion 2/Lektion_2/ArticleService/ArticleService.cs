namespace Lektion_2.ArticleService;

public class ArticleService : IArticleService
{
    private readonly List<Article> _articles = new List<Article>();

    public void AddArticle(Article article)
    {
        _articles.Add(article);
    }

    public Task DeleteArticle(int id)
    {
        var article = _articles.FirstOrDefault(a => a.Id == id);

        if (article != null)
        {
            _articles.Remove(article);
        }

        return Task.CompletedTask;
    }

    public void UpdateArticle(Article article)
    {
        var existingArticle = _articles.FirstOrDefault(a => a.ArticleTitle == article.ArticleTitle);
        if (existingArticle != null)
        {
            existingArticle.ArticleContent = article.ArticleContent;
        }
    }

    public IEnumerable<Article> GetAllArticles()
    {
        return _articles;
    }
}