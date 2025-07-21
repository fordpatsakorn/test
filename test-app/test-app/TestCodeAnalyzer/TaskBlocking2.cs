using System.Threading.Tasks;

namespace test_app.TestCodeAnalyzer;

public class TaskBlocking2
{
    void Example()
    {
        Console.WriteLine("The error can be detected at line level");
        Task task1 = Task.CompletedTask;
        Task<int> task2 = Task.FromResult(42);
        task1.Wait(); // I touched this line, so it should have warning
        var result = task2.Result; // No warning, because i didn't touch
      
    }
}
