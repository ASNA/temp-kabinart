using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASNA.DataGateHelper;
using CommandLineUtility;
using System.Data.SqlClient;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;

namespace ExportFromDGToSQLServer
{
    class Program
    {
        // -sdb "*Public/DG NET Local" -l examples -f cmastnewl2 -tdb deleteme -sp -p -v
        // -sdb "*Public/DG NET Local" -l nutandbolts -f orders -tdb deleteme -sp -p -v

        static int Main(string[] args)
        {

            int result = (new ExportDGFile()).Run(args);
            return result;
        }
    }

    public class ExportDGFile
    {
        SqlConnection conn;
        string sql;
        string targetTableName;
        string targetSqlDatabase;
        CmdLineArgs cmdArgs;
        Stopwatch stopwatch = new Stopwatch();

        SimpleLogger logger = new SimpleLogger(append: true);

        string bar = "\u2502";
        string topcorner = "\u250c";
        string bottomcorner = "\u2514";

        public int Run(string[] args)
        {
            this.cmdArgs = new CmdLineArgs();
            CmdArgManager cam = new CmdArgManager(cmdArgs, args, "Export a DataGate file to SQL Server");
            CmdArgManager.ExitCode result = cam.ParseArgs();

            if (result == CmdArgManager.ExitCode.HelpShown)
            {
                return (int)CmdArgManager.ExitCode.Success;
            }

            if (result == CmdArgManager.ExitCode.Success)
            {
                Export();
                if (this.cmdArgs.Pause)
                {
                    Console.CursorTop += 1;

                    Console.WriteLine("Finished. Press any key to continue...");
                    Console.ReadKey();
                }
                return (int)CmdArgManager.ExitCode.Success;
            }
            else
            {
                Console.WriteLine("**ERROR**");
                Console.WriteLine(cam.ErrorMessage);
                return (int)result;
            }
        }

        public int Export()
        {
            stopwatch.Start();

            this.targetTableName = this.cmdArgs.FileName;
            this.targetSqlDatabase = this.cmdArgs.TargetDatabaseName;

            string connectionString = String.Format(ConfigurationManager.ConnectionStrings["sqlserverconnection"].ConnectionString, cmdArgs.TargetDatabaseName);

            using (conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    clearTargetTable();

                    using (ASNA.DataGate.Client.AdgConnection apiDGDB = new ASNA.DataGate.Client.AdgConnection(cmdArgs.SourceDatabaseName))
                    {
                        string msg = String.Format("Exporting from DataGate db.library.file {0}.{1}.{2} to SQL Server database.table {3}.{4}.",
                            cmdArgs.SourceDatabaseName,
                            cmdArgs.LibraryName,
                            cmdArgs.FileName,
                            cmdArgs.TargetDatabaseName,
                            cmdArgs.FileName);

                        logger.Info(topcorner + " BEGIN " + msg);

                        if (cmdArgs.ShowProgressFlag)
                        {
                            Console.WriteLine(msg);
                        }

                        ASNA.DataGateHelper.DGFileReader dgfr = new ASNA.DataGateHelper.DGFileReader(apiDGDB, 500);
                        dgfr.AfterRowRead += OnAfterRowRead;

                        try
                        {
                            dgfr.ReadEntireFile(cmdArgs.LibraryName, cmdArgs.FileName);
                            return (int)CmdArgManager.ExitCode.Success;
                        }
                        catch (Exception ex)
                        {
                            if (cmdArgs.ShowProgressFlag)
                            {
                                Console.WriteLine(ex.Message);
                            }

                            if (ex.Message.ToLower().StartsWith("file is empty"))
                            {
                                logger.Info(bar + " " + ex.Message);
                                logger.Info(String.Format(bottomcorner.ToString() + " END"));
                            }
                            else
                            {
                                logger.Error(bar + ex.Message);
                                logger.Error(String.Format(bottomcorner.ToString() + " END"));
                            }
                        }

                        return (int)CmdArgManager.ExitCode.ExceptionOccurred;
                    }
                }
                catch (Exception exouter)
                {
                    if (cmdArgs.ShowProgressFlag)
                    {
                        Console.WriteLine(exouter.Message);
                    }
                    logger.Error(String.Format("**ERROR**: " + exouter.Message));

                    return (int)CmdArgManager.ExitCode.ExceptionOccurred;
                }
            }
        }

        void OnAfterRowRead(System.Object sender, ASNA.DataGateHelper.AfterRowReadArgs e)
        {
            if (e.CurrentRowCounter == 1)
            {
                this.sql = getInsertIntoSql(e);
                if (this.cmdArgs.Verbose)
                {
                    logger.Trace(String.Format(bar + " SQL: {0}", sql));
                }
            }

            if (this.cmdArgs.Verbose)
            {
                logger.Trace(String.Format(bar + " Row values:"));
            }

            SqlCommand sqlcmd = new SqlCommand(this.sql.ToString(), conn);
            foreach (string fieldname in e.FieldNames)
            {
                sqlcmd.Parameters.AddWithValue("@" + fieldname, e.DataRow[fieldname].ToString());
                if (this.cmdArgs.Verbose)
                {
                    logger.Trace(String.Format(bar + "   {0,-20} = {1}", fieldname, e.DataRow[fieldname].ToString()));
                }
            }
            int result = sqlcmd.ExecuteNonQuery();
            sqlcmd.Dispose();

            if (this.cmdArgs.ShowProgressFlag)
            {
                showProgress(e.CurrentRowCounter, e.TotalRowsCounter);
            }

            if (e.CurrentRowCounter == e.TotalRowsCounter)
            {
                stopwatch.Stop();
                int totalSqlRows = getTotalSqlRows();

                char bottomcorner = '\u2514';


                long minutes = Math.Max(stopwatch.ElapsedMilliseconds / 60000, 1);
                string lessThanOneMinute = (stopwatch.ElapsedMilliseconds < 60000) ? "<" : String.Empty;

                string msg = String.Format("{0:#,###} total rows written (in {1}{2:###0} min) to SQL Server which matches total rows in source file.", totalSqlRows, lessThanOneMinute, minutes);

                if (totalSqlRows == e.TotalRowsCounter)
                {
                    logger.Info(bar + " " + msg);
                    logger.Info(bottomcorner + " END");
                    if (this.cmdArgs.ShowProgressFlag)
                    {
                        Console.WriteLine(msg);
                        Console.CursorTop += 1;

                    }
                }
                else
                {
                    // handle error here. 
                }
            }
        }

        private void clearTargetTable()
        {
            SqlCommand sqlcmd = new SqlCommand(String.Format("delete from {0}", this.targetTableName), this.conn);
            sqlcmd.ExecuteNonQuery();
        }

        private int getTotalSqlRows()
        {
            SqlCommand sqlcmd = new SqlCommand(String.Format("select count(*) from {0}", this.targetTableName), conn);
            return (int)sqlcmd.ExecuteScalar();
        }

        private string getInsertIntoSql(ASNA.DataGateHelper.AfterRowReadArgs e)
        {
            StringBuilder fieldnamelist = new StringBuilder();
            StringBuilder fieldnameprefixedlist = new StringBuilder();

            StringBuilder sql = new StringBuilder();
            sql.Append("Insert into {{tablename}} (");
            sql.Append("{{fieldnamelist}}");
            sql.Append(")");
            sql.Append(" values(");
            sql.Append("{{fieldnameprefixedlist}}");
            sql.Append(")");

            foreach (string field in e.FieldNames)
            {
                fieldnamelist.Append(field + ",");
                fieldnameprefixedlist.Append("@" + field + ",");
            }

            sql.Replace("{{tablename}}", this.targetTableName);
            sql.Replace("{{fieldnamelist}}", Utils.RemoveLastCharacter(fieldnamelist.ToString()));
            sql.Replace("{{fieldnameprefixedlist}}", Utils.RemoveLastCharacter(fieldnameprefixedlist.ToString()));

            return sql.ToString();
        }

        void showProgress(int currentRowCounter, long totalRowsCounter)
        {
            int cursorLeft;
            int cursorTop;

            cursorLeft = Console.CursorLeft;
            cursorTop = Console.CursorTop;
            Console.WriteLine("{0} of {1}", currentRowCounter, totalRowsCounter);
            Console.CursorLeft = cursorLeft;
            Console.CursorTop = cursorTop;
        }
    }

    public class CmdLineArgs
    {
        const bool REQUIRED = true;
        const bool OPTIONAL = false;

        [CmdArg("--sourcedatabasename", "-sdb", REQUIRED, "Source DataGate database name")]
        public string SourceDatabaseName { get; set; }

        [CmdArg("--library", "-l", REQUIRED, "DataGate library")]
        public string LibraryName { get; set; }

        [CmdArg("--file", "-f", REQUIRED, "DataGate file name (also used as new SQL Server table name)")]
        public string FileName { get; set; }

        [CmdArg("--targetdatabasename", "-tdb", REQUIRED, "Target SQL Server database")]
        public string TargetDatabaseName { get; set; }

        [CmdArg("--showprogress", "-sp", OPTIONAL, "Show export progress")]
        public bool ShowProgressFlag { get; set; } = false;

        [CmdArg("--pause", "-p", OPTIONAL, "Pause screen after export--usually for debugging purposes")]
        public bool Pause { get; set; } = false;

        [CmdArg("--verboselog", "-v", OPTIONAL, "Log SQL detail")]
        public bool Verbose { get; set; } = false;
    }
}
