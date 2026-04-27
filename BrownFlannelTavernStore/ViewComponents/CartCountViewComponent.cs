using Microsoft.AspNetCore.Mvc;
using BrownFlannelTavernStore.Services;

namespace BrownFlannelTavernStore.ViewComponents;

public class CartCountViewComponent : ViewComponent
{
    private readonly CartService _cartService;

    public CartCountViewComponent(CartService cartService)
    {
        _cartService = cartService;
    }

    public IViewComponentResult Invoke()
    {
        var count = _cartService.GetItemCount();
        return View(count);
    }
}
