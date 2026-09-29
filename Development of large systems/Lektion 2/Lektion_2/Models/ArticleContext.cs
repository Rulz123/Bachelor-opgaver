using Lektion_2.ArticleService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

public class ArticleDBContext : DbContext
{

    public ArticleDBContext(DbContextOptions<ArticleDBContext> options) : base(options)
    {
        DbSet<Article> Articles = Set<Article>();
    }

    public DbSet<Article> Articles { get; set; }
}