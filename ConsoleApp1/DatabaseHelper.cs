using Core.Data;
using Test.Helper;

namespace ConsoleApp1;

class DatabaseHelper : DatabaseSetupHelper
{
    public new readonly DataContext Context;

    public DatabaseHelper()
    {
        Context = base.Context;
        
    }

    public void printSqlLog()
    {
        _sqlLog.ForEach(Console.WriteLine);
    }
    
}