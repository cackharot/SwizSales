using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Xps.Packaging;
using System.IO;
using System.IO.Packaging;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Printing;
using System.Windows.Xps;
using System.Xml;
using SwizSales.Core.Model;
using SwizSales.Core.Library;
using System.Windows.Xps.Serialization;
using SwizSales.Library;

namespace SwizSales
{
    public static class PrintHelper
    {
        private static string PrintTicketTemplate = ApplicationSettings.PrintTicketTemplate;

        public static FlowDocument GetPrintDocument(string templateXml, Order order)
        {
            var doc = new XmlDocument();
            doc.LoadXml(templateXml);

            PopulateTemplate(doc, order);

            var flowDocument = (FlowDocument)XamlReader.Load(new XmlTextReader(new StringReader(doc.OuterXml)));

            flowDocument.DataContext = order;
            flowDocument.PageHeight = order.OrderDetails.Count * ApplicationSettings.LineHeight + ApplicationSettings.ExtraHeight;
            flowDocument.PageWidth = ApplicationSettings.PageWidth;

            // we need to give the binding infrastructure a push as we
            // are operating outside of the intended use of WPF
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.Thread.CurrentCulture = SwizConfigurationManager.GetCulture();
            LanguageManipulator.SetXmlLanguage(dispatcher.Thread.CurrentCulture);
            dispatcher.Invoke(DispatcherPriority.SystemIdle, new DispatcherOperationCallback(delegate { return null; }), null);

            return flowDocument;
        }

        public static XmlDocument GetPrintTicketTemplate(Order order)
        {
            XmlDocument doc = new XmlDocument();

            using (FileStream fs = File.Open(PrintTicketTemplate, FileMode.Open, FileAccess.ReadWrite))
            {
                doc.Load(fs);
            }

            PopulateTemplate(doc, order);

            return doc;
        }

        private static void PopulateTemplate(XmlDocument doc, Order order)
        {
            var tableRows = doc.GetElementsByTagName("TableRow");
            XmlElement itemRow = null;

            foreach (XmlElement row in tableRows)
            {
                if (row.HasAttribute("Name")
                    && row.Attributes["Name"].Value.ToString().Equals("itemRow", StringComparison.OrdinalIgnoreCase))
                {
                    itemRow = row;
                    break;
                }
            }

            if (itemRow != null)
            {
                XmlElement rowGroup = (XmlElement)itemRow.ParentNode;
                XmlElement rowTemplate = (XmlElement)itemRow.CloneNode(true);
                rowGroup.RemoveChild(itemRow);

                foreach (var orderItem in order.OrderDetails)
                {
                    XmlElement newRow = (XmlElement)rowTemplate.CloneNode(true);
                    newRow.RemoveAttribute("Name");

                    var tableCells = newRow.GetElementsByTagName("TableCell");

                    foreach (XmlElement cell in tableCells)
                    {
                        if (cell.HasAttribute("Name"))
                        {
                            switch (cell.Attributes["Name"].Value.ToString())
                            {
                                case "ItemName":
                                    string iname = orderItem.ItemName;
                                    cell.FirstChild.InnerText = iname;
                                    break;
                                case "MRP":
                                    cell.FirstChild.InnerText = orderItem.MRP.ToString("F2");
                                    break;
                                case "Price":
                                    cell.FirstChild.InnerText = orderItem.Price.ToString("F2");
                                    break;
                                case "Quantity":
                                    cell.FirstChild.InnerText = orderItem.Quantity.ToString("F1");
                                    break;
                                case "Discount":
                                    cell.FirstChild.InnerText = orderItem.Discount.ToString("P");
                                    break;
                                case "Amount":
                                    cell.FirstChild.InnerText = orderItem.LineTotal.ToString("F2");
                                    break;
                            }

                            cell.RemoveAttribute("Name");
                        }
                    }

                    rowGroup.PrependChild(newRow);
                }
            }
        }

        public static XpsDocument GetXpsDocument(FlowDocument document)
        {
            //Uri DocumentUri = new Uri("pack://currentTicket_" + new Random().Next(1000).ToString() + ".xps");
            Uri docUri = new Uri("pack://tempTicket.xps");

            var ms = new MemoryStream();
            {
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);

                PackageStore.RemovePackage(docUri);
                PackageStore.AddPackage(docUri, package);

                XpsDocument xpsDocument = new XpsDocument(package, CompressionOption.SuperFast, docUri.AbsoluteUri);

                XpsSerializationManager rsm = new XpsSerializationManager(new XpsPackagingPolicy(xpsDocument), false);

                DocumentPaginator docPage = ((IDocumentPaginatorSource)document).DocumentPaginator;

                //docPage.PageSize = new System.Windows.Size(PageWidth, PageHeight);
                //docPage.PageSize = new System.Windows.Size(document.PageWidth, document.PageHeight);

                rsm.SaveAsXaml(docPage);

                return xpsDocument;
            }
        }

        public static void PrintXps(FlowDocument document, bool printDirect = true)
        {
            try
            {
                Uri DocumentUri = new Uri("pack://currentTicket.xps");
                var ms = new MemoryStream();
                {
                    using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                    {
                        PackageStore.AddPackage(DocumentUri, package);
                        XpsDocument xpsDocument = new XpsDocument(package, CompressionOption.SuperFast, DocumentUri.AbsoluteUri);
                        XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);

                        writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);

                        if (printDirect)
                        {
                            PrintXpsToPrinter(xpsDocument);
                        }

                        PackageStore.RemovePackage(DocumentUri);
                        xpsDocument.Close();
                        package.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // log the exceptions
                LogService.Error("Print Error", ex);
            }
        }

        public static void PrintXpsToPrinter(XpsDocument xpsDocument, string printQueueName = null)
        {
            PrintTicket ticket = new PrintTicket();
            PrintQueue queue = LocalPrintServer.GetDefaultPrintQueue();

            if (!string.IsNullOrEmpty(printQueueName))
            {
                try
                {
                    queue = new LocalPrintServer().GetPrintQueue(printQueueName);
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while getting print queue " + printQueueName, ex);
                }
            }

            ticket.PageMediaSize = new PageMediaSize(PageWidth, PageHeight);
            queue.UserPrintTicket = ticket;

            XpsDocumentWriter pwriter = PrintQueue.CreateXpsDocumentWriter(queue);

            pwriter.WritingCompleted += (s, e) =>
            {

            };

            pwriter.WriteAsync(xpsDocument.GetFixedDocumentSequence(), ticket);
        }

        public static double PageHeight
        {
            get
            {
                PrintQueue pq = LocalPrintServer.GetDefaultPrintQueue();
                PrintTicket ticket = pq.UserPrintTicket;
                return ticket.PageMediaSize.Height ?? SwizSales.Properties.Settings.Default.TicketHeight;
            }
        }

        public static double PageWidth
        {
            get
            {
                PrintQueue pq = LocalPrintServer.GetDefaultPrintQueue();
                PrintTicket ticket = pq.UserPrintTicket;
                return ticket.PageMediaSize.Width ?? SwizSales.Properties.Settings.Default.TicketWidth;
            }
        }
    }
}
