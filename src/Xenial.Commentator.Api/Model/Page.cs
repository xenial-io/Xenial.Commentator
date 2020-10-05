using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Xenial.Commentator.Model
{
    public class PageWorkModel
    {
        [Required]
        public string Id { get; set; }

        public Comment Comment { get; set; }

        public string InReplyTo { get; set; }
    }

    public class Page
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public IList<Comment> Comments { get; } = new List<Comment>();
    }

    public class Comment
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string GithubOrEmail { get; set; }
        public string Homepage { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public string AvatarUrl { get; set; }

        [Required]
        public IList<Comment> Comments { get; } = new List<Comment>();
    }
}