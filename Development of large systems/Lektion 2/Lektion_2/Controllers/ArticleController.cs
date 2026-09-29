using Microsoft.AspNetCore.Mvc;
using Lektion_2.ArticleService;
namespace Lektion_2.Controllers;

[ApiController]
[Route("[controller]")]
public class ArticleController : ControllerBase
{
    ArticleContext db =new ArticleContext();

    [HttpGet(Name = "GetArticles")]
    public IEnumerable<Article> GetArticles()
    {
        var articleService = new ArticleService.ArticleService();
        db.Find<Article>(1);
        return articleService.GetAllArticles();
    }

    [HttpPost(Name = "AddArticle")]
    public void AddArticle(Article article)
    {
        var articleService = new ArticleService.ArticleService();
        articleService.AddArticle(article);
        db.Add(article);
        db.SaveChanges();
    }

    [HttpDelete(Name = "DeleteArticle")]
    public void DeleteArticle(Article article)
    {
        var articleService = new ArticleService.ArticleService();
        articleService.DeleteArticle(article);
        db.Remove(article);
        db.SaveChanges();
    }

    [HttpPut(Name = "UpdateArticle")]
    public void UpdateArticle(Article article)
    {
        var articleService = new ArticleService.ArticleService();
        articleService.UpdateArticle(article);
        db.Update(article);
        db.SaveChanges();
    }
}