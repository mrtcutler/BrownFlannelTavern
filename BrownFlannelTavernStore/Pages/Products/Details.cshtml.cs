using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services;

namespace BrownFlannelTavernStore.Pages.Products;

public class DetailsModel : PageModel
{
    private readonly StoreDbContext _context;
    private readonly CartService _cartService;

    public DetailsModel(StoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public Product? Product { get; set; }
    public List<string> AvailableSizes { get; set; } = [];
    public List<string> AvailableColors { get; set; } = [];
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public int ProductId { get; set; }

    [BindProperty]
    public string? SelectedSize { get; set; }

    [BindProperty]
    public string? SelectedColor { get; set; }

    [BindProperty]
    public int Quantity { get; set; } = 1;

    public async Task OnGetAsync(int id)
    {
        await LoadProduct(id);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadProduct(ProductId);

        if (Product == null)
            return NotFound();

        var variant = Product.Variants.FirstOrDefault(v =>
            v.Size == SelectedSize && v.Color == SelectedColor);

        if (Product.Variants.Any() && variant == null)
        {
            ErrorMessage = "Please select a valid size and color.";
            return Page();
        }

        if (variant != null && variant.StockQuantity < Quantity)
        {
            ErrorMessage = "Not enough stock available.";
            return Page();
        }

        _cartService.AddToCart(new CartItem
        {
            ProductId = Product.Id,
            ProductName = Product.Name,
            VariantId = variant?.Id,
            Size = SelectedSize,
            Color = SelectedColor,
            Price = Product.Price,
            Quantity = Quantity,
            ImageUrl = Product.ImageUrl
        });

        Message = "Added to cart!";
        return Page();
    }

    private async Task LoadProduct(int id)
    {
        Product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (Product != null)
        {
            AvailableSizes = Product.Variants.Select(v => v.Size).Distinct().ToList();
            AvailableColors = Product.Variants.Select(v => v.Color).Distinct().ToList();
        }
    }
}
