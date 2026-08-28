using System;
using System.Reflection;
using CucumberExpressions;

class Program
{
    static void Main()
    {
        var expr = new CucumberExpression("I have {int} cukes", null);
        Console.WriteLine("CucumberExpression methods:");
        foreach (var m in expr.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (m.DeclaringType != typeof(object))
                Console.WriteLine($"  {m.Name}");
        }
    }
}
