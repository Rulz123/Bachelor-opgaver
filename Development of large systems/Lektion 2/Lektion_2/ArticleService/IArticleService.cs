namespace Lektion_2.ArticleService;
public interface IArticleService
{
    void AddArticle(Article article);
    public Task DeleteArticle(int id);
    void UpdateArticle(Article article);
    IEnumerable<Article> GetAllArticles();
}