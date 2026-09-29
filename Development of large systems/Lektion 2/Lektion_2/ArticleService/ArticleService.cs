namespace Lektion_2.ArticleService;

public class ArticleService : IArticleService
{
    private readonly List<Article> _articles = new List<Article>();

    public void AddArticle(Article article)
    {
        _articles.Add(article);
    }

    public void DeleteArticle(Article article)
    {
        _articles.Remove(article);
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