using System;
using System.ComponentModel.DataAnnotations;

namespace Lektion_2.ArticleService;

public class Article
{
    [Key]
    public int Id {get;set;}
    public string ArticleTitle {get;set;}
    public string ArticleContent {get;set;}
    [Timestamp]
    public byte[] RowVersion {get;set;}
}
