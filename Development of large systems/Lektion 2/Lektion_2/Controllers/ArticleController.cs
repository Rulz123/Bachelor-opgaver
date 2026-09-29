using Microsoft.AspNetCore.Mvc;
using Lektion_2.ArticleService;
namespace Lektion_2.Controllers;

[ApiController]
[Route("[controller]")]
public class ArticleController : ControllerBase
{
    private readonly ArticleDBContext _context;

    public ArticleController(ArticleDBContext context)
    {
        _context = context;
    }

    [HttpGet(Name = "GetArticles")]
    public IEnumerable<Article> GetArticles()
    {
        var articleService = new ArticleService.ArticleService();
        articleService.GetAllArticles().ToList()
            .ForEach(article => _context.Add(article));
        return articleService.GetAllArticles();
    }

    [HttpPost(Name = "AddArticle")]
    public void AddArticle(Article article)
    {
        var articleService = new ArticleService.ArticleService();
        articleService.AddArticle(article);
        _context.Add(article);
        _context.SaveChanges();
    }

    [HttpDelete(Name = "DeleteArticle")]
    public void DeleteArticle(Article article)
    {
        var articleService = new ArticleService.ArticleService();
        articleService.DeleteArticle(article);
        _context.Remove(article);
        _context.SaveChanges();
    }

    [HttpPut(Name = "UpdateArticle")]
    public void UpdateArticle(Article article)
    {
        var articleService = new ArticleService.ArticleService();
        articleService.UpdateArticle(article);
        _context.Update(article);
        _context.SaveChanges();
    }
}