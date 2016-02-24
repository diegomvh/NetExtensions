namespace Stj.Utilities.Pdf
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Web.Mvc;

    using Common.Logging;

    public class PdfConverter
    {
        private readonly string HtmlToPdfExePath;
        private readonly ILog log = LogManager.GetLogger(typeof(PdfConverter));

        public PdfConverter(string htmlToPdfExePath = "wkhtmltopdf.exe") {
            this.HtmlToPdfExePath = htmlToPdfExePath;
        }

        public byte[] FromString(string source)
        {
            Process p;
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = this.HtmlToPdfExePath;
            psi.WorkingDirectory = Path.GetDirectoryName(psi.FileName);

            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            //Argumentos http://wkhtmltopdf.org/usage/wkhtmltopdf.txt
            string args = "-q -n ";
            args += "--disable-smart-shrinking ";
            args += "--outline-depth 0 ";
            args += "--page-size A4 ";
            args += " - -";

            psi.Arguments = args;

            p = Process.Start(psi);

            try
            {
                using (StreamWriter stdin = p.StandardInput)
                {
                    stdin.AutoFlush = true;
                    var b = Encoding.UTF8.GetBytes(source);
                    stdin.BaseStream.Write(b, 0, b.Length);
                }

                byte[] buffer = new byte[32768];
                byte[] file;
                using (var ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = p.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                            break;
                        ms.Write(buffer, 0, read);
                    }
                    file = ms.ToArray();
                }

                p.StandardOutput.Close();
                p.WaitForExit(60000);

                int returnCode = p.ExitCode;
                p.Close();

                if (returnCode == 0)
                    return file;
                else
                    log.Error("Could not create PDF, returnCode:" + returnCode);
            }
            catch (Exception ex)
            {
                log.Error("Could not create PDF", ex);
            }
            finally
            {
                p.Close();
                p.Dispose();
            }
            return null;
        }

        public byte[] FromRazorView(PdfRazorModel model, string viewPathRoot = "", string applicationPathRoot = "")
        {
            var directory = System.AppDomain.CurrentDomain.BaseDirectory;
            if (String.IsNullOrWhiteSpace(viewPathRoot))
                viewPathRoot = Path.GetFullPath(Path.Combine(directory, @"Views"));
            if (String.IsNullOrWhiteSpace(applicationPathRoot))
                applicationPathRoot = Path.GetFullPath(directory);

            var engine = new Stj.Utilities.RazorEngine.FileSystemRazorViewEngine(applicationPathRoot, viewPathRoot);
            var controller_context = new ControllerContext();
            var view = engine.FindView(controller_context, model.ViewName, null, true);
            using (var writer = new StringWriter())
            {
                var context = new ViewContext(controller_context, view.View,
                    model.ViewData, new TempDataDictionary(), writer);
                view.View.Render(context, writer);
                writer.Flush();
                return this.FromString(writer.ToString());
            }
        }
    }
}