using System;
using System.ComponentModel.DataAnnotations;

namespace Lektion_2.ArticleService;

public class Article
{
    [Key]
    public int Id {get;set;}
    public string ArticleTitle {get;set;}
    public string ArticleContent {get;set;}
    public string Continent {get;set;}
    public DateTime CreatedAt {get;set;} = DateTime.Now;   
}
