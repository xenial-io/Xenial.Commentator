using System;
using System.Collections.Generic;

namespace Xenial.Commentator.Model
{
    public class Page
    {
        public string Id { get; set; }

        public IList<Comment> Comments { get; } = new List<Comment>();
    }

    public class Comment
    {
        public string Name { get; set; }
        public string GithubOrEmail { get; set; }
        public string Homepage { get; set; }
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public string AvatarUrl { get; set; }
    }
}