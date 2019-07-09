using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace SQLScripterCore
{
    class Program
    {
        static List<string> tables = new List<string>();

        static string outputDir = "reference";

        static void Main(string[] args)
        {
            var server = new Server(args[0]);
            var db = server.Databases[args[1]];

            if (args.Length > 2)
            {
                tables = File.ReadAllLines(args[2]).ToList();
            }

            var scripter = new Scripter(server);

            scripter.Options.EnforceScriptingOptions = true;
            scripter.Options.WithDependencies = false;
            scripter.Options.IncludeHeaders = true;
            scripter.Options.ScriptDrops = false;
            scripter.Options.ScriptSchema = false;
            scripter.Options.ScriptData = true;
            scripter.Options.Indexes = false;

            Directory.CreateDirectory(outputDir);

            StringBuilder script = new StringBuilder();
            StringBuilder tableList = new StringBuilder();

            int lines = 0;
            File.Delete(@"reference\insert_reference_data.sql");

            var tablesSubDirName = "tables";
            var tablesDir = Path.Join(outputDir, tablesSubDirName);
            var mainScriptPath = Path.Join(outputDir, "insert_reference_data.sql");


            if (Directory.Exists(tablesDir))
            {
                Directory.Delete(tablesDir, true);
            }
           

            // Disable foreign key constraint checks
            File.AppendAllText(mainScriptPath, "EXEC sp_MSforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"\r\n");
            foreach (Table table in db.Tables)
            {
                if (tables.Count==0 || tables.Any(x=>x.Equals(table.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var sc = scripter.EnumScript(new Urn[] { table.Urn });
                    foreach (var s in sc)
                    {
                        script.AppendLine(s);
                        lines++;
                    }
                }

                if (script.Length > 0)
                {
                    Directory.CreateDirectory(tablesDir);
                    tableList.AppendLine(@":r .\" + tablesSubDirName + $"\\{table.Name}.sql");
                    File.AppendAllText(Path.Join(tablesDir, $"{table.Name}.sql"), script.ToString(), Encoding.UTF8);
                    script.Clear();
                    lines = 0;
                }
                else
                {
                    Debug.WriteLine($"{table.Name} no data.");
                }
            }

            File.AppendAllText(mainScriptPath, tableList.ToString());

            // Re-enable foreign key constraint checks
            File.AppendAllText(mainScriptPath, "EXEC sp_MSforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"");
        }
    }
}
