using System;
using System.Collections.Generic;

public class ExerciseData
{
    // Properties
    public string Name { get; set; }
    public (double Min, double Max) GreenZoneX { get; private set; }
    public (double Min, double Max) GreenZoneY { get; private set; }
    public ((double Min, double Max) Range1, (double Min, double Max) Range2) RedZoneX { get; private set; }
    public ((double Min, double Max) Range1, (double Min, double Max) Range2) RedZoneY { get; private set; }
    public int Timing { get; set; }
    public List<string> LegsUsed { get; set; }

    // Constructor
    public ExerciseData(string name, 
                        (double Min, double Max) greenZoneX, 
                        (double Min, double Max) greenZoneY,
                        ((double Min, double Max) Range1, (double Min, double Max) Range2) redZoneX, 
                        ((double Min, double Max) Range1, (double Min, double Max) Range2) redZoneY,
                        int timing, 
                        List<string> legsUsed)
    {
        Name = name;
        GreenZoneX = greenZoneX;
        GreenZoneY = greenZoneY;
        RedZoneX = redZoneX;
        RedZoneY = redZoneY;
        Timing = timing;
        LegsUsed = legsUsed;
    }

    // Methods
    public bool IsInGreenZone(double x, double y)
    {
        return x > GreenZoneX.Min && x < GreenZoneX.Max &&
               y > GreenZoneY.Min && y < GreenZoneY.Max;
    }

    public bool IsInRedZone(double x, double y)
    {
        return (x >= RedZoneX.Range1.Min && x <= RedZoneX.Range1.Max || 
                x >= RedZoneX.Range2.Min && x <= RedZoneX.Range2.Max) &&
               (y >= RedZoneY.Range1.Min && y <= RedZoneY.Range1.Max || 
                y >= RedZoneY.Range2.Min && y <= RedZoneY.Range2.Max);
    }

    public void PrintDetails()
    {
        Console.WriteLine($"Exercise: {Name}");
        Console.WriteLine($"Green Zone (X): {GreenZoneX.Min} to {GreenZoneX.Max}");
        Console.WriteLine($"Green Zone (Y): {GreenZoneY.Min} to {GreenZoneY.Max}");
        Console.WriteLine($"Red Zone (X): ({RedZoneX.Range1.Min}, {RedZoneX.Range1.Max}) and ({RedZoneX.Range2.Min}, {RedZoneX.Range2.Max})");
        Console.WriteLine($"Red Zone (Y): ({RedZoneY.Range1.Min}, {RedZoneY.Range1.Max}) and ({RedZoneY.Range2.Min}, {RedZoneY.Range2.Max})");
        Console.WriteLine($"Timing: {Timing} seconds");
        Console.WriteLine($"Legs Used: {string.Join(", ", LegsUsed)}");
    }


    /* public void StartTimer()
    {
        Console.WriteLine($"Starting {Name} exercise. Timer set for {Timing} seconds.");

        for (int i = Timing; i > 0; i--)
        {
            Console.WriteLine($"Time remaining: {i} seconds...");
            System.Threading.Thread.Sleep(1000);
        }

        Console.WriteLine("pause for 15 secs");
    } */

ExerciseData singleLegStanceRight = new ExerciseData(
            name: "Single-Leg Stance - Right Leg",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((-6.0, -1.0), (1.0, 6.0)),  
            redZoneY: ((-6.0, -1.0), (1.0, 6.0)),  
            timing: 30,
            legsUsed: new List<string> { "right" }
        );
}
