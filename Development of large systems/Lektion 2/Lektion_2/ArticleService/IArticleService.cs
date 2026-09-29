namespace Lektion_2.ArticleService;
public interface IArticleService
{
    void AddArticle(Article article);
    void DeleteArticle(Article article);
    void UpdateArticle(Article article);
    IEnumerable<Article> GetAllArticles();
}