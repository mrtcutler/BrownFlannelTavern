using BrownFlannelTavernStore.Utilities;
using FluentAssertions;

namespace BrownFlannelTavernStore.Tests.Utilities;

public class SortableHeaderViewModelTests
{
    private static SortableHeaderViewModel Build(string columnKey, string? currentSort, string? currentDir) => new()
    {
        PageName = "/Admin/Orders/Index",
        ColumnKey = columnKey,
        DisplayName = "Column",
        CurrentSort = currentSort,
        CurrentDir = currentDir
    };

    [Fact]
    public void IsActive_WhenCurrentSortMatchesColumnKey_IsTrue()
    {
        var vm = Build("total", currentSort: "total", currentDir: "asc");

        vm.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenCurrentSortDiffers_IsFalse()
    {
        var vm = Build("total", currentSort: "date", currentDir: "asc");

        vm.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_IsCaseInsensitive()
    {
        var vm = Build("Total", currentSort: "total", currentDir: null);

        vm.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NewDirection_NotActive_IsAsc()
    {
        var vm = Build("total", currentSort: "date", currentDir: "desc");

        vm.NewDirection.Should().Be("asc");
    }

    [Fact]
    public void NewDirection_ActiveAsc_TogglesToDesc()
    {
        var vm = Build("total", currentSort: "total", currentDir: "asc");

        vm.NewDirection.Should().Be("desc");
    }

    [Fact]
    public void NewDirection_ActiveDesc_TogglesToAsc()
    {
        var vm = Build("total", currentSort: "total", currentDir: "desc");

        vm.NewDirection.Should().Be("asc");
    }

    [Fact]
    public void Arrow_NotActive_IsEmpty()
    {
        var vm = Build("total", currentSort: "date", currentDir: "asc");

        vm.Arrow.Should().Be("");
    }

    [Fact]
    public void Arrow_ActiveAsc_IsUpArrow()
    {
        var vm = Build("total", currentSort: "total", currentDir: "asc");

        vm.Arrow.Should().Be(" ▲");
    }

    [Fact]
    public void Arrow_ActiveDesc_IsDownArrow()
    {
        var vm = Build("total", currentSort: "total", currentDir: "desc");

        vm.Arrow.Should().Be(" ▼");
    }

    [Fact]
    public void SortRouteData_IncludesColumnKeyAndNewDirectionAndResetsPage()
    {
        var vm = new SortableHeaderViewModel
        {
            PageName = "/Admin/Orders/Index",
            ColumnKey = "total",
            DisplayName = "Total",
            CurrentSort = "date",
            CurrentDir = "desc",
            RouteData = new Dictionary<string, string?>
            {
                ["status"] = "Paid",
                ["page"] = "3"
            }
        };

        var data = vm.SortRouteData();

        data["sortBy"].Should().Be("total");
        data["sortDir"].Should().Be("asc");
        data["page"].Should().BeNull();
        data["status"].Should().Be("Paid");
    }
}
