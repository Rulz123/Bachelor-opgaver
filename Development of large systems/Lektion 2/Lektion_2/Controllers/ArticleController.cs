using Microsoft.AspNetCore.Mvc;
using Lektion_2.ArticleService;
using Microsoft.EntityFrameworkCore;
namespace Lektion_2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticleController : ControllerBase
{
    private static HttpClient sharedClient = new()
    {
        BaseAddress = new Uri("https://jsonplaceholder.typicode.com"),
    };
    private readonly ArticleDBContext _context;

    public ArticleController(ArticleDBContext context)
    {
        _context = context;
    }

    [HttpGet(Name = "GetArticles")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IEnumerable<Article> GetArticles()
    {
        return _context.Articles.ToList();
    }

    [HttpPost(Name = "AddArticle")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public void AddArticle(Article article)
    {
        _context.Add(article);
        _context.SaveChanges();
    }

    [HttpDelete("{id}", Name = "DeleteArticle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteArticle(int id)
    {
        var article = _context.Articles.Find(id);

        if (article == null)
            return NotFound();

        _context.Articles.Remove(article);
        _context.SaveChanges();

        return NoContent();
    }

    [HttpPut(Name = "UpdateArticle")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public void UpdateArticle(Article article)
    {
        _context.Update(article);
        _context.SaveChanges();
    }
}