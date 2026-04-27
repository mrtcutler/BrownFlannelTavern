using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services;

namespace BrownFlannelTavernStore.Pages.Cart;

public class IndexModel : PageModel
{
    private readonly CartService _cartService;

    public IndexModel(CartService cartService)
    {
        _cartService = cartService;
    }

    public List<CartItem> CartItems { get; set; } = [];
    public decimal Total { get; set; }

    public void OnGet()
    {
        CartItems = _cartService.GetCart();
        Total = _cartService.GetTotal();
    }

    public IActionResult OnPostRemove(int productId, int? variantId)
    {
        _cartService.RemoveFromCart(productId, variantId);
        return RedirectToPage();
    }

    public IActionResult OnPostUpdateQuantity(int productId, int? variantId, int quantity)
    {
        _cartService.UpdateQuantity(productId, variantId, quantity);
        return RedirectToPage();
    }
}
