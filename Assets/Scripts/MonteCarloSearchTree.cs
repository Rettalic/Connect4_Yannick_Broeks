using System.Threading;

public class MonteCarloSearchTree
{
    public readonly Field currentStateField;

    public Field simulatedStateField;

    public int iterationCount;

    // ManualResetEvent used for parallel processing with ThreadPool.
    // Source: https://learn.microsoft.com/en-us/dotnet/api/system.threading.manualresetevent?view=net-8.0
    // Source: https://learn.microsoft.com/en-us/dotnet/api/system.threading?view=net-8.0 
    public ManualResetEvent doneEvent;

    // The root node of the Monte Carlo Tree.
    public Node rootNode;

    public MonteCarloSearchTree(Field _field, ManualResetEvent _doneEvent, int _iterationCount)
    {
        doneEvent = _doneEvent;
        currentStateField = _field;
        iterationCount = _iterationCount;
    }
}
