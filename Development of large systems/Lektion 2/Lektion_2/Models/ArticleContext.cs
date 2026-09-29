using Lektion_2.ArticleService;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

public class ArticleContext : DbContext
{
     public string DbPath { get; }
     public ArticleContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "ArticleServiceDb.db");
        Console.WriteLine(DbPath);
    }

     protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    public DbSet<Article> Articles { get; set; }
}