﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xenial.Commentator.Model;
using System.Collections.Concurrent;
using System.Net.Http;
using Xenial.Commentator.Helpers;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Xenial.Commentator.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ILogger<CommentsController> _logger;
        private readonly ConcurrentQueue<Page> _queue;
        private readonly IHttpClientFactory _httpClientFactory;

        public CommentsController(ILogger<CommentsController> logger, ConcurrentQueue<Page> queue, IHttpClientFactory httpClientFactory)
            => (_logger, _queue, _httpClientFactory) = (logger, queue, httpClientFactory);

        [HttpPost]
        [ProducesResponseType(typeof(Page), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] PageInputModel pageInput)
        {
            if (string.IsNullOrEmpty(pageInput.Operation) || (pageInput.Operation != "+" && pageInput.Operation != "-"))
            {
                return BadRequest("Wrong captcha operation");
            }

            var answer = pageInput.Operation == "+"
                ? pageInput.A + pageInput.B
                : pageInput.A - pageInput.B;

            if (answer != pageInput.Answer)
            {
                return BadRequest("Captcha is wrong");
            }

            _queue.Enqueue(new Page
            {
                Id = pageInput.Id,
                Comments =
                {
                    new Comment
                    {
                        Content = StringHelper.StripMarkdownTags(pageInput.Content),
                        GithubOrEmail = pageInput.GithubOrEmail,
                        Name = string.IsNullOrWhiteSpace(pageInput.Name) ? null : pageInput.Name.Trim(),
                        Homepage = string.IsNullOrWhiteSpace(pageInput.Homepage) ? null : pageInput.Homepage.Trim(),
                        Date = DateTime.Now,
                    }
                }
            });

            return await Preview(pageInput);
        }

        [HttpPost]
        [ProducesResponseType(typeof(Page), StatusCodes.Status200OK)]
        [Route("preview")]
        public async Task<IActionResult> Preview([FromBody] PageInputModel pageInput)
        {
            var client = _httpClientFactory.CreateClient(nameof(CommentsController));

            var page = new Page
            {
                Id = pageInput.Id
            };

            var avatarUrl = await client.FetchAvatarFromGithub(_logger, pageInput.GithubOrEmail);

            page.Comments.Add(new Comment
            {
                AvatarUrl = avatarUrl,
                Content = CommonMark.CommonMarkConverter.Convert(StringHelper.StripMarkdownTags(pageInput.Content)),
                Name = string.IsNullOrWhiteSpace(pageInput.Name) ? null : pageInput.Name.Trim(),
                Homepage = string.IsNullOrWhiteSpace(pageInput.Homepage) ? null : pageInput.Homepage.Trim(),
                Date = DateTime.Now,
            });

            return Ok(page);
        }

        [HttpGet]
        [Route("captcha")]
        public CaptchaModel GetCaptcha()
        {
            var random = new Random();

            var a = random.Next(10);
            var b = random.Next(10);
            a = a >= b ? a : b;
            b = a >= b ? b : a;

            var operation = random.Next(1000) > 500 ? "+" : "-";

            return new CaptchaModel
            {
                A = a,
                B = b,
                Operation = operation,
                Text = $"{a} {operation} {b} = ?"
            };
        }
    }

    public class CaptchaModel
    {
        [Required]
        public int A { get; set; }
        [Required]
        public int B { get; set; }
        [Required]
        public string Operation { get; set; }
        [Required]
        public string Text { get; set; }
    }

    public class PageInputModel
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
        public int A { get; set; }
        [Required]
        public int B { get; set; }
        [Required]
        public string Operation { get; set; }
        [Required]
        public int Answer { get; set; }
    }
}
