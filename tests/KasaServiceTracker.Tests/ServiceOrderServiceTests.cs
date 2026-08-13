using Xunit;
using KasaServiceTracker.Models;
using KasaServiceTracker.Repositories;
using KasaServiceTracker.Services;

namespace KasaServiceTracker.Tests;

public sealed class ServiceOrderServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ServiceOrderService _service;

    public ServiceOrderServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"kasa-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        var repository = new JsonServiceOrderRepository(
            Path.Combine(_tempDirectory, "orders.json"));
        _service = new ServiceOrderService(repository);
    }

    [Fact]
    public void CreateOrder_ShouldPersistOrderWithInitialStatus()
    {
        var order = _service.CreateOrder(
            "abc-123", "Nissan", "Sentra", 2025, "Cambio de aceite", 1500m);

        Assert.StartsWith("KS-", order.Folio);
        Assert.Equal("ABC-123", order.Vehicle.Plate);
        Assert.Equal(ServiceStatus.Recibido, order.Status);
        Assert.Single(order.History);
        Assert.Single(_service.GetAllOrders());
    }

    [Fact]
    public void SearchByPlate_ShouldReturnMatchingOrders()
    {
        _service.CreateOrder("ABC-123", "Nissan", "Sentra", 2025, "Aceite", 1200m);
        _service.CreateOrder("XYZ-987", "Kia", "K3", 2024, "Frenos", 3200m);

        var result = _service.SearchByPlate("abc");

        Assert.Single(result);
        Assert.Equal("ABC-123", result[0].Vehicle.Plate);
    }

    [Fact]
    public void UpdateStatus_ShouldAppendHistory()
    {
        var order = _service.CreateOrder(
            "ABC-123", "Nissan", "Sentra", 2025, "Diagnóstico", 500m);

        var updated = _service.UpdateStatus(
            order.Folio, ServiceStatus.Diagnostico, "Diagnóstico iniciado.");

        Assert.Equal(ServiceStatus.Diagnostico, updated.Status);
        Assert.Equal(2, updated.History.Count);
    }

    [Fact]
    public void UpdateStatus_ShouldRejectBackwardTransition()
    {
        var order = _service.CreateOrder(
            "ABC-123", "Nissan", "Sentra", 2025, "Servicio", 1000m);
        _service.UpdateStatus(order.Folio, ServiceStatus.EnReparacion, null);

        Assert.Throws<InvalidOperationException>(() =>
            _service.UpdateStatus(order.Folio, ServiceStatus.Diagnostico, null));
    }

    [Fact]
    public void CreateOrder_ShouldRejectNegativeCost()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.CreateOrder(
                "ABC-123", "Nissan", "Sentra", 2025, "Servicio", -1m));
    }

    [Fact]
    public void CompleteAfterTicket_ShouldFinishDeliveredOrder()
    {
        var order = _service.CreateOrder(
            "ABC-123", "Nissan", "Sentra", 2025, "Servicio", 1000m);
        _service.UpdateStatus(order.Folio, ServiceStatus.Entregado, "Unidad entregada.");

        var completed = _service.CompleteAfterTicket(order.Folio);

        Assert.Equal(ServiceStatus.Terminado, completed.Status);
        Assert.Contains(completed.History, h => h.Status == ServiceStatus.Terminado);
    }

    [Fact]
    public void CreateAndEdit_ShouldNormalizeTextToUppercase()
    {
        var order = _service.CreateOrder("abc-123", "Nissan", "Sentra", 2025, "Cambio de aceite", 1500m);
        var edited = _service.EditOrder(order.Folio, "xyz-987", "Kia", "K3 gt", 2024, "Afinación completa", 2100m);
        Assert.Equal("XYZ-987", edited.Vehicle.Plate);
        Assert.Equal("KIA", edited.Vehicle.Brand);
        Assert.Equal("K3 GT", edited.Vehicle.Model);
        Assert.Equal("AFINACIÓN COMPLETA", edited.ServiceDescription);
        Assert.Contains(edited.History, item => item.Note.StartsWith("CORRECCIÓN"));
    }

    [Fact]
    public void ActiveWeeklyList_ShouldExcludeFinishedOrders()
    {
        var active = _service.CreateOrder("AAA-111", "Nissan", "Versa", 2025, "Servicio", 1000m);
        var finished = _service.CreateOrder("BBB-222", "Kia", "Rio", 2025, "Servicio", 1000m);
        _service.UpdateStatus(finished.Folio, ServiceStatus.Entregado, null);
        _service.CompleteAfterTicket(finished.Folio);
        var result = _service.GetActiveOrdersForCurrentWeek(DateTime.Now);
        Assert.Contains(result, item => item.Folio == active.Folio);
        Assert.DoesNotContain(result, item => item.Folio == finished.Folio);
    }

    [Fact]
    public void MonthlyExporter_ShouldCreateWorkbookWithOrdersAndHistorySheets()
    {
        var exportPath = Path.Combine(_tempDirectory, "Exportaciones");
        var repository = new JsonServiceOrderRepository(Path.Combine(_tempDirectory, "excel-orders.json"));
        var service = new ServiceOrderService(repository, new MonthlyExcelExportService(exportPath));
        var order = service.CreateOrder("abc-123", "Nissan", "Sentra", 2025, "Servicio", 1000m);
        service.UpdateStatus(order.Folio, ServiceStatus.Diagnostico, "revisión inicial");
        var file = Path.Combine(exportPath, $"KASA-Service-{DateTime.Now:yyyy-MM}.xlsx");
        Assert.True(File.Exists(file));
        using var archive = System.IO.Compression.ZipFile.OpenRead(file);
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet2.xml"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
