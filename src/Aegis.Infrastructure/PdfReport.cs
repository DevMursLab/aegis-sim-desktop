using Aegis.Simulation;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Aegis.Infrastructure;

public static class PdfReport
{
    static PdfReport() { GlobalFontSettings.UseWindowsFontsUnderWindows = true; }
    public static void Save(string path, RunDocument document)
    {
        var result = RunHistory.Replay(document);
        var metrics = RunMetrics.Calculate(document);
        using var pdf = new PdfDocument();
        pdf.Info.Title = "AEGIS-SIM / Verified exercise report";
        pdf.Info.Author = "AEGIS-SIM";
        var title = new XFont("Arial", 23, XFontStyleEx.Bold);
        var body = new XFont("Arial", 11);
        var small = new XFont("Arial", 9);
        var ink = new XSolidBrush(XColor.FromArgb(23, 60, 79));
        var green = new XSolidBrush(XColor.FromArgb(39, 123, 101));
        int pageNumber = 0;
        for (int start = 0; start < Math.Max(1, result.Session.Shelters.Count); start += 18)
        {
            var page = pdf.AddPage(); page.Size = PdfSharp.PageSize.A4;
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.DrawString("AEGIS-SIM", title, ink, 44, 55);
            graphics.DrawString("VERIFIED EXERCISE REPORT / SYNTHETIC CIVILIAN DATA", small, ink, 44, 78);
            graphics.DrawString(Ascii(document.Scenario.Name), body, ink, 44, 111);
            graphics.DrawString($"Run: {document.Id}", small, ink, 44, 132);
            graphics.DrawString($"Created: {document.CreatedAt:yyyy-MM-dd HH:mm} UTC | Engine: {document.EngineVersion}", small, ink, 44, 149);
            graphics.DrawString($"{result.Session.Status}  /  {result.Session.Tick} ticks  /  {result.Session.DeliveredTotal} of {result.Session.InitialDemand} packets", body, ink, 44, 180);
            graphics.DrawString($"Mean service tick: {metrics.MeanServiceTick:0.00} | Active ticks: {metrics.ActiveTickPercent:0.0}% | Waiting ticks: {metrics.WaitingTicks}", small, ink, 44, 199);
            int offset = result.Session.UsesLogistics ? 22 : 0;
            if (result.Session.UsesLogistics)
                graphics.DrawString($"Depot stock: {metrics.DepotStockRemaining} | In transit: {metrics.InTransit} | Restocked: {metrics.RestockedTotal} | Return ticks: {metrics.ReturnTicks}", small, ink, 44, 218);
            graphics.DrawLine(new XPen(XColors.LightGray), 44, 214 + offset, 550, 214 + offset);
            graphics.DrawString("SHELTER", small, ink, 44, 235 + offset);
            graphics.DrawString("DELIVERED / REQUESTED", small, ink, 290, 235 + offset);
            int y = 260 + offset;
            foreach (var shelter in result.Session.Shelters.Skip(start).Take(18))
            {
                graphics.DrawString(Ascii(shelter.Name), body, ink, 44, y);
                graphics.DrawString($"{shelter.InitialDemand - shelter.Remaining} / {shelter.InitialDemand}", body, ink, 290, y);
                double percent = shelter.InitialDemand == 0 ? 1 : (double)(shelter.InitialDemand - shelter.Remaining) / shelter.InitialDemand;
                graphics.DrawRectangle(XBrushes.LightGray, 400, y - 10, 145, 8);
                if (percent > 0) graphics.DrawRectangle(green, 400, y - 10, 145 * percent, 8);
                y += 22;
            }
            string hash = RunHistory.Hash(result.Session);
            graphics.DrawString($"Verified ordered commands: {document.Commands.Length}. Final state SHA-256:", small, ink, 44, 680);
            graphics.DrawString(hash[..32], small, ink, 44, 696);
            graphics.DrawString(hash[32..], small, ink, 44, 710);
            graphics.DrawString(result.Session.UsesLogistics ? "Model: finite stock and configured travel ticks; no distance/speed physics or efficacy claim." : "Model: unlimited reload and instant visits; no real-world efficacy claim.", small, ink, 44, 742);
            graphics.DrawString("Route closure restricts a shelter for logical ticks. Full Unicode labels: UTF-8 HTML/CSV.", small, ink, 44, 757);
            graphics.DrawString($"Page {++pageNumber} / {Math.Max(1, (result.Session.Shelters.Count + 17) / 18)}", small, ink, 44, 790);
        }
        pdf.Save(path);
    }
    private static string Ascii(string text) => new(text.Take(80).Select(c => c is >= ' ' and <= '~' ? c : '?').ToArray());
}
