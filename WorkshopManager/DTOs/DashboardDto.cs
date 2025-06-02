namespace WorkshopManager.DTOs;

public class DashboardDto
{
    // Statystyki wspólne dla wszystkich ról
    public int TotalOrdersCount { get; set; }
    public int ActiveOrdersCount { get; set; }
    public int CompletedOrdersCount { get; set; }
    public int NewOrdersCount { get; set; }

    // Statystyki dla Admina i Recepcjonisty
    public int CustomersCount { get; set; }
    public int VehiclesCount { get; set; }
    public decimal TotalRevenueThisMonth { get; set; }
    public int NewOrdersThisWeek { get; set; }

    // Statystyki dla Mechanika
    public int AssignedOrdersCount { get; set; }
    public int CompletedOrdersThisMonth { get; set; }

    // Dane do wykresów
    public Dictionary<string, int> OrdersByStatus { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> OrdersByMonth { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> TopVehicleMakes { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, decimal> RevenueByMonth { get; set; } = new Dictionary<string, decimal>();
}