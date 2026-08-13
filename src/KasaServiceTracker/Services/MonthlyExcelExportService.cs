using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using KasaServiceTracker.Models;

namespace KasaServiceTracker.Services;

public sealed class MonthlyExcelExportService : IOrderExportService
{
    private readonly string _exportDirectory;

    public MonthlyExcelExportService(string exportDirectory)
    {
        _exportDirectory = exportDirectory;
        Directory.CreateDirectory(_exportDirectory);
    }

    public void ExportAll(IEnumerable<ServiceOrder> orders)
    {
        foreach (var month in orders.GroupBy(order => new { order.CreatedAt.Year, order.CreatedAt.Month }))
        {
            var path = Path.Combine(_exportDirectory,
                $"KASA-Service-{month.Key.Year:0000}-{month.Key.Month:00}.xlsx");
            WriteWorkbook(path, month.OrderBy(order => order.CreatedAt).ToList());
        }
    }

    private static void WriteWorkbook(string path, IReadOnlyList<ServiceOrder> orders)
    {
        var temporaryPath = path + ".tmp";
        if (File.Exists(temporaryPath)) File.Delete(temporaryPath);

        using (var archive = ZipFile.Open(temporaryPath, ZipArchiveMode.Create))
        {
            Add(archive, "[Content_Types].xml", ContentTypes);
            Add(archive, "_rels/.rels", RootRelationships);
            Add(archive, "docProps/app.xml", AppProperties);
            Add(archive, "docProps/core.xml", CoreProperties);
            Add(archive, "xl/workbook.xml", Workbook);
            Add(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships);
            Add(archive, "xl/styles.xml", Styles);
            Add(archive, "xl/worksheets/sheet1.xml", BuildOrdersSheet(orders));
            Add(archive, "xl/worksheets/sheet2.xml", BuildHistorySheet(orders));
        }

        File.Move(temporaryPath, path, overwrite: true);
    }

    private static string BuildOrdersSheet(IReadOnlyList<ServiceOrder> orders)
    {
        string[] headers = ["FOLIO", "FECHA DE REGISTRO", "PLACAS", "MARCA", "MODELO", "AÑO",
            "SERVICIO", "COSTO ESTIMADO", "ESTADO", "ÚLTIMA ACTUALIZACIÓN", "NOTA MÁS RECIENTE"];
        var rows = new List<IReadOnlyList<Cell>> { headers.Select(Text).ToArray() };
        rows.AddRange(orders.Select(order => (IReadOnlyList<Cell>)[
            Text(order.Folio), Date(order.CreatedAt), Text(order.Vehicle.Plate), Text(order.Vehicle.Brand),
            Text(order.Vehicle.Model), Number(order.Vehicle.Year), Text(order.ServiceDescription),
            Money(order.EstimatedCost), Text(StatusName(order.Status)),
            Date(order.History.LastOrDefault()?.ChangedAt ?? order.CreatedAt),
            Text(order.History.LastOrDefault()?.Note ?? string.Empty)]));
        return BuildSheet(rows, [18, 20, 15, 18, 18, 10, 42, 18, 18, 22, 52], "A1:K1");
    }

    private static string BuildHistorySheet(IReadOnlyList<ServiceOrder> orders)
    {
        string[] headers = ["FOLIO", "FECHA Y HORA", "ESTADO", "NOTA"];
        var rows = new List<IReadOnlyList<Cell>> { headers.Select(Text).ToArray() };
        foreach (var order in orders)
            rows.AddRange(order.History.OrderBy(item => item.ChangedAt).Select(change =>
                (IReadOnlyList<Cell>)[Text(order.Folio), Date(change.ChangedAt), Text(StatusName(change.Status)), Text(change.Note)]));
        return BuildSheet(rows, [18, 22, 18, 70], "A1:D1");
    }

    private static string BuildSheet(IReadOnlyList<IReadOnlyList<Cell>> rows, int[] widths, string filter)
    {
        var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><cols>");
        for (var i = 0; i < widths.Length; i++) xml.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{widths[i]}\" customWidth=\"1\"/>");
        xml.Append("</cols><sheetData>");
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            xml.Append($"<row r=\"{rowIndex + 1}\" ht=\"{(rowIndex == 0 ? 24 : 20)}\" customHeight=\"1\">");
            for (var colIndex = 0; colIndex < rows[rowIndex].Count; colIndex++)
            {
                var cell = rows[rowIndex][colIndex];
                var reference = $"{ColumnName(colIndex + 1)}{rowIndex + 1}";
                var style = rowIndex == 0 ? 1 : cell.Style;
                if (cell.IsNumber) xml.Append($"<c r=\"{reference}\" s=\"{style}\"><v>{cell.Value}</v></c>");
                else xml.Append($"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(cell.Value)}</t></is></c>");
            }
            xml.Append("</row>");
        }
        xml.Append($"</sheetData><autoFilter ref=\"{filter}\"/></worksheet>");
        return xml.ToString();
    }

    private static Cell Text(string value) => new(value, false, 2);
    private static Cell Number(int value) => new(value.ToString(CultureInfo.InvariantCulture), true, 3);
    private static Cell Money(decimal value) => new(value.ToString(CultureInfo.InvariantCulture), true, 4);
    private static Cell Date(DateTime value) => new(value.ToOADate().ToString(CultureInfo.InvariantCulture), true, 5);
    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;
    private static string StatusName(ServiceStatus status) => status switch
    {
        ServiceStatus.Diagnostico => "DIAGNÓSTICO",
        ServiceStatus.EnReparacion => "EN REPARACIÓN",
        _ => status.ToString().ToUpperInvariant()
    };

    private static string ColumnName(int number)
    {
        var name = string.Empty;
        while (number > 0) { number--; name = (char)('A' + number % 26) + name; number /= 26; }
        return name;
    }

    private static void Add(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private sealed record Cell(string Value, bool IsNumber, int Style);

    private const string ContentTypes = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/><Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/></Types>";
    private const string RootRelationships = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/></Relationships>";
    private const string AppProperties = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\"><Application>KASA Service Tracker</Application></Properties>";
    private const string CoreProperties = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><dc:title>KASA Service Tracker</dc:title><dc:creator>KASA SERVICE TRACKER</dc:creator></cp:coreProperties>";
    private const string Workbook = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"ÓRDENES\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"HISTORIAL\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>";
    private const string WorkbookRelationships = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
    private const string Styles = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><color rgb=\"FFFFFFFF\"/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1F4E78\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"6\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf><xf numFmtId=\"1\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/><xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/></cellXfs><numFmts count=\"2\"><numFmt numFmtId=\"164\" formatCode=\"&quot;$&quot;#,##0.00\"/><numFmt numFmtId=\"165\" formatCode=\"dd/mm/yyyy hh:mm\"/></numFmts></styleSheet>";
}
