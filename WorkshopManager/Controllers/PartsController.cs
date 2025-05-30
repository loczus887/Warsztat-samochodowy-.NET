using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Controllers;

[Authorize(Roles = "Admin,Receptionist")]
public class PartsController : Controller
{
    private readonly IPartService _partService;
    private readonly PartMapper _mapper;

    public PartsController(
        IPartService partService,
        PartMapper mapper)
    {
        _partService = partService;
        _mapper = mapper;
    }

    // GET: Parts
    public async Task<IActionResult> Index(string searchString)
    {
        var parts = string.IsNullOrEmpty(searchString)
            ? await _partService.GetAllPartsAsync()
            : await _partService.SearchPartsByNameAsync(searchString);

        var partDtos = _mapper.PartsToDto(parts);

        ViewData["CurrentFilter"] = searchString;

        return View(partDtos);
    }

    // GET: Parts/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var part = await _partService.GetPartByIdAsync(id.Value);
        if (part == null)
        {
            return NotFound();
        }

        var partDto = _mapper.PartToDto(part);
        return View(partDto);
    }

    // GET: Parts/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Parts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PartDto partDto)
    {
        if (ModelState.IsValid)
        {
            var part = _mapper.DtoToPart(partDto);
            await _partService.CreatePartAsync(part);
            return RedirectToAction(nameof(Index));
        }
        return View(partDto);
    }

    // GET: Parts/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var part = await _partService.GetPartByIdAsync(id.Value);
        if (part == null)
        {
            return NotFound();
        }

        var partDto = _mapper.PartToDto(part);
        return View(partDto);
    }

    // POST: Parts/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PartDto partDto)
    {
        if (id != partDto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var part = _mapper.DtoToPart(partDto);
                await _partService.UpdatePartAsync(part);
            }
            catch (Exception)
            {
                if (!await PartExists(partDto.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(partDto);
    }

    // GET: Parts/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var part = await _partService.GetPartByIdAsync(id.Value);
        if (part == null)
        {
            return NotFound();
        }

        var partDto = _mapper.PartToDto(part);
        return View(partDto);
    }

    // POST: Parts/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _partService.DeletePartAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> PartExists(int id)
    {
        var part = await _partService.GetPartByIdAsync(id);
        return part != null;
    }
}