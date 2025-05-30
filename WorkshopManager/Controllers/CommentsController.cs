using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly IServiceOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CommentMapper _mapper;

    public CommentsController(
        IServiceOrderService orderService,
        UserManager<ApplicationUser> userManager,
        CommentMapper mapper)
    {
        _orderService = orderService;
        _userManager = userManager;
        _mapper = mapper;
    }

    // POST: Comments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommentDto commentDto)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);

            var comment = new Comment
            {
                Content = commentDto.Content,
                CreatedAt = DateTime.Now,
                ServiceOrderId = commentDto.ServiceOrderId,
                AuthorId = user.Id
            };

            await _orderService.AddCommentToOrderAsync(commentDto.ServiceOrderId, comment);

            return RedirectToAction("Details", "ServiceOrders", new { id = commentDto.ServiceOrderId });
        }

        return RedirectToAction("Details", "ServiceOrders", new { id = commentDto.ServiceOrderId });
    }
}