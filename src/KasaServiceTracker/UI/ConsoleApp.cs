using System.Globalization;
using KasaServiceTracker.Models;
using KasaServiceTracker.Services;

namespace KasaServiceTracker.UI;

public sealed class ConsoleApp
{
    private readonly ServiceOrderService _service;
    private readonly TicketPrintService _ticketPrinter;

    public ConsoleApp(ServiceOrderService service, TicketPrintService ticketPrinter)
    {
        _service = service;
        _ticketPrinter = ticketPrinter;
    }

    public void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (true)
        {
            DrawHeader();
            Console.WriteLine("1. Registrar orden de servicio");
            Console.WriteLine("2. Listar órdenes activas de la semana");
            Console.WriteLine("3. Buscar por folio");
            Console.WriteLine("4. Editar / corregir registro");
            Console.WriteLine("5. Buscar por placas");
            Console.WriteLine("6. Cambiar estado");
            Console.WriteLine("7. Ver historial de una orden");
            Console.WriteLine("8. Ver / reimprimir ticket");
            Console.WriteLine("0. Salir");
            Console.Write("\nSelecciona una opción: ");

            var option = Console.ReadLine()?.Trim();
            Console.WriteLine();

            try
            {
                switch (option)
                {
                    case "1": RegisterOrder(); break;
                    case "2": ListOrders(); break;
                    case "3": SearchByFolio(); break;
                    case "4": EditOrder(); break;
                    case "5": SearchByPlate(); break;
                    case "6": ChangeStatus(); break;
                    case "7": ShowHistory(); break;
                    case "8": ViewOrReprintTicket(); break;
                    case "0":
                        Console.WriteLine("KASA Service Tracker finalizado.");
                        return;
                    default:
                        Console.WriteLine("Opción no válida.");
                        break;
                }
            }
            catch (Exception ex) when (ex is ArgumentException
                                       or InvalidOperationException
                                       or KeyNotFoundException
                                       or IOException)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }

            Pause();
        }
    }

    private void RegisterOrder()
    {
        Console.WriteLine("=== NUEVA ORDEN ===");
        var plate = ReadRequired("Placas: ");
        var brand = ReadRequired("Marca: ");
        var model = ReadRequired("Modelo: ");
        var year = ReadInt("Año: ");
        var service = ReadRequired("Servicio solicitado: ");
        var cost = ReadDecimal("Costo estimado: $");

        var order = _service.CreateOrder(plate, brand, model, year, service, cost);
        Console.WriteLine($"\nOrden creada correctamente. Folio: {order.Folio}");
    }

    private void ListOrders()
    {
        Console.WriteLine("=== ÓRDENES ACTIVAS DE LA SEMANA ACTUAL ===");
        PrintOrders(_service.GetActiveOrdersForCurrentWeek());
    }

    private void EditOrder()
    {
        Console.WriteLine("=== EDITAR / CORREGIR REGISTRO ===");
        var folio = ReadRequired("Folio: ");
        var current = _service.GetOrderByFolio(folio);
        Console.WriteLine("Presiona ENTER para conservar el valor actual.\n");
        var plate = ReadOptional($"Placas [{current.Vehicle.Plate}]: ", current.Vehicle.Plate);
        var brand = ReadOptional($"Marca [{current.Vehicle.Brand}]: ", current.Vehicle.Brand);
        var model = ReadOptional($"Modelo [{current.Vehicle.Model}]: ", current.Vehicle.Model);
        var year = ReadOptionalInt($"Año [{current.Vehicle.Year}]: ", current.Vehicle.Year);
        var service = ReadOptional($"Servicio [{current.ServiceDescription}]: ", current.ServiceDescription);
        var cost = ReadOptionalDecimal($"Costo estimado [{current.EstimatedCost:C2}]: $", current.EstimatedCost);
        Console.Write("¿Guardar los cambios? (Y/N): ");
        if (!string.Equals(Console.ReadLine()?.Trim(), "Y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Cambios cancelados.");
            return;
        }

        var updated = _service.EditOrder(folio, plate, brand, model, year, service, cost);
        Console.WriteLine("\nOrden actualizada correctamente.");
        PrintOrder(updated);
    }

    private void SearchByFolio()
    {
        var folio = ReadRequired("Folio: ");
        PrintOrder(_service.GetOrderByFolio(folio));
    }

    private void SearchByPlate()
    {
        var plate = ReadRequired("Placas o fragmento: ");
        PrintOrders(_service.SearchByPlate(plate));
    }

    private void ChangeStatus()
    {
        var folio = ReadRequired("Folio: ");
        var order = _service.GetOrderByFolio(folio);
        Console.WriteLine($"Estado actual: {FormatStatus(order.Status)}");
        Console.WriteLine();

        foreach (var status in Enum.GetValues<ServiceStatus>())
        {
            Console.WriteLine($"{(int)status}. {FormatStatus(status)}");
        }

        var statusNumber = ReadInt("Nuevo estado: ");
        if (!Enum.IsDefined(typeof(ServiceStatus), statusNumber))
        {
            throw new ArgumentException("Selecciona un estado válido.");
        }

        var newStatus = (ServiceStatus)statusNumber;
        if (newStatus == ServiceStatus.Terminado)
        {
            throw new InvalidOperationException(
                "Terminado es automático. Primero cambia la orden a Entregado para imprimir el ticket.");
        }

        Console.Write("Nota (opcional): ");
        var note = Console.ReadLine();
        var updated = _service.UpdateStatus(folio, newStatus, note);
        Console.WriteLine($"\nEstado actualizado a: {FormatStatus(updated.Status)}");

        if (updated.Status == ServiceStatus.Entregado)
        {
            Console.WriteLine("\nGenerando ticket doble Empresa / Usuario...");
            ShowTicketPreview(updated);

            var files = _ticketPrinter.PrintDoubleTicket(updated);
            Console.WriteLine("\nTickets enviados a la impresora predeterminada y respaldados en:");
            foreach (var file in files)
            {
                Console.WriteLine($"- {file}");
            }

            var finished = _service.CompleteAfterTicket(updated.Folio);
            Console.WriteLine($"\nOrden finalizada automáticamente. Estado: {FormatStatus(finished.Status)}");
        }
    }

    private void ShowHistory()
    {
        var folio = ReadRequired("Folio: ");
        var order = _service.GetOrderByFolio(folio);

        Console.WriteLine($"\n=== HISTORIAL {order.Folio} ===");
        foreach (var change in order.History.OrderBy(item => item.ChangedAt))
        {
            Console.WriteLine(
                $"{change.ChangedAt:dd/MM/yyyy HH:mm} | {FormatStatus(change.Status),-14} | {change.Note}");
        }
    }

    private void ViewOrReprintTicket()
    {
        Console.WriteLine("=== CONSULTA / REIMPRESIÓN DE TICKET ===");
        var folio = ReadRequired("Folio: ");
        var order = _service.GetOrderByFolio(folio);

        if (order.Status is not (ServiceStatus.Entregado or ServiceStatus.Terminado))
        {
            throw new InvalidOperationException(
                "El ticket sólo está disponible cuando la unidad fue entregada o la orden ya está terminada.");
        }

        ShowTicketPreview(order);
        Console.Write("\n¿Imprimir nuevamente el ticket doble? (Y/N): ");
        var answer = Console.ReadLine()?.Trim();

        if (string.Equals(answer, "Y", StringComparison.OrdinalIgnoreCase))
        {
            var files = _ticketPrinter.PrintDoubleTicket(order);
            Console.WriteLine("\nReimpresión enviada. Nuevos respaldos:");
            foreach (var file in files)
            {
                Console.WriteLine($"- {file}");
            }
        }
        else
        {
            Console.WriteLine("\nReimpresión cancelada.");
        }
    }

    private void ShowTicketPreview(ServiceOrder order)
    {
        Console.WriteLine();
        Console.WriteLine(_ticketPrinter.BuildTicket(order, "Empresa"));
        Console.WriteLine(_ticketPrinter.BuildTicket(order, "Usuario"));
    }

    private static void PrintOrders(IReadOnlyCollection<ServiceOrder> orders)
    {
        if (orders.Count == 0)
        {
            Console.WriteLine("No se encontraron órdenes.");
            return;
        }

        foreach (var order in orders)
        {
            PrintOrder(order);
            Console.WriteLine(new string('-', 72));
        }
    }

    private static void PrintOrder(ServiceOrder order)
    {
        Console.WriteLine($"Folio:      {order.Folio}");
        Console.WriteLine($"Vehículo:   {order.Vehicle}");
        Console.WriteLine($"Servicio:   {order.ServiceDescription}");
        Console.WriteLine($"Estado:     {FormatStatus(order.Status)}");
        Console.WriteLine($"Costo est.: {order.EstimatedCost:C2}");
        Console.WriteLine($"Registro:   {order.CreatedAt:dd/MM/yyyy HH:mm}");
    }

    private static string ReadRequired(string prompt)
    {
        Console.Write(prompt);
        var value = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El valor es obligatorio.");
        }

        return value.Trim();
    }

    private static int ReadInt(string prompt)
    {
        Console.Write(prompt);
        var value = Console.ReadLine();
        if (!int.TryParse(value, out var number))
        {
            throw new ArgumentException("Ingresa un número entero válido.");
        }

        return number;
    }

    private static decimal ReadDecimal(string prompt)
    {
        Console.Write(prompt);
        var value = Console.ReadLine();
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            return amount;
        }

        throw new ArgumentException("Ingresa una cantidad válida.");
    }

    private static string ReadOptional(string prompt, string current)
    {
        Console.Write(prompt);
        var value = Console.ReadLine();
        return string.IsNullOrWhiteSpace(value) ? current : value.Trim();
    }

    private static int ReadOptionalInt(string prompt, int current)
    {
        Console.Write(prompt);
        var value = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(value)) return current;
        return int.TryParse(value, out var number) ? number : throw new ArgumentException("Ingresa un número entero válido.");
    }

    private static decimal ReadOptionalDecimal(string prompt, decimal current)
    {
        Console.Write(prompt);
        var value = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(value)) return current;
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)) return amount;
        throw new ArgumentException("Ingresa una cantidad válida.");
    }

    private static string FormatStatus(ServiceStatus status) => status switch
    {
        ServiceStatus.Recibido => "Recibido",
        ServiceStatus.Diagnostico => "Diagnóstico",
        ServiceStatus.EnReparacion => "En reparación",
        ServiceStatus.Entregado => "Entregado",
        ServiceStatus.Terminado => "Terminado",
        _ => status.ToString()
    };

    private static void DrawHeader()
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
        Console.WriteLine("========================================");
        Console.WriteLine("         KASA SERVICE TRACKER");
        Console.WriteLine("       Seguimiento de servicios");
        Console.WriteLine("========================================\n");
    }

    private static void Pause()
    {
        Console.WriteLine("\nPresiona ENTER para continuar...");
        Console.ReadLine();
    }
}
