using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Post;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

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
    /// Provider chỉ nhận về bài đúng capability của mình (designer → design,
    /// constructor → construction, both → cả ba); owner/admin thấy tất cả.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? projectShopOwnerId = null,
        [FromQuery] string? serviceKind = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _postService.GetAllAsync(
            pageNumber, pageSize, projectShopOwnerId, serviceKind, status, search,
            User.GetAccountId());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _postService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// [ĐĂNG BÀI] Owner đăng bài tuyển provider. serviceKind: design | construction | both.
    /// Bài tạo với status=open; submissionDeadline (nếu có) phải ở tương lai.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        var result = await _postService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostRequest request)
    {
        var result = await _postService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _postService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
