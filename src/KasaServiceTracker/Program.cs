using KasaServiceTracker.Repositories;
using KasaServiceTracker.Services;
using KasaServiceTracker.UI;

var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "service-orders.json");
var ticketsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Tickets");
var exportsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Exportaciones");

var repository = new JsonServiceOrderRepository(dataPath);
var exporter = new MonthlyExcelExportService(exportsPath);
var service = new ServiceOrderService(repository, exporter);
var ticketPrinter = new TicketPrintService(ticketsPath);
var app = new ConsoleApp(service, ticketPrinter);

app.Run();
