using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Post;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;

    public PostController(IPostService postService)
    {
        _postService = postService;
    }

    /// <summary>
    /// [TÌM BÀI] Provider tìm bài đăng. serviceKind: design | construction | both.
    /// status: open | closed | cancelled (lọc open tự ẩn bài quá hạn nộp). search: theo title.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? projectShopOwnerId = null,
        [FromQuery] string? serviceKind = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _postService.GetAllAsync(
            pageNumber, pageSize, projectShopOwnerId, serviceKind, status, search);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _postService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// [ĐĂNG BÀI] Owner đăng bài tuyển provider. serviceKind: design | construction | both.
    /// Bài tạo với status=open; submissionDeadline (nếu có) phải ở tương lai.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        var result = await _postService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePostRequest request)
    {
        var result = await _postService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _postService.DeleteAsync(id);
        return NoContent();
    }
}
