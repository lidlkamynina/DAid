using System;
using System.Collections.Generic;


public class ExerciseData
{
    public int ExerciseID { get; set; }
    public string Name { get; set; }
    public int Timing { get; set; }
    public string LegsUsed { get; set; }
    public int Sets { get; private set; }
    public int DemoTime { get; private set; } 

    public List<(int duration, (double Min, double Max) GreenZoneX, (double Min, double Max) GreenZoneY, 
                 (double Min, double Max) RedZoneX, (double Min, double Max) RedZoneY)> ZoneSequence { get; set; }

    public ExerciseData(int exerciseID,
                        string name,
                        int timing,
                        int demoTime, 
                        string legsUsed,
                        int sets,
                        List<(int, (double, double), (double, double), (double, double), (double, double))> zoneSequence)
    {
        ExerciseID = exerciseID;
        Name = name;
        Timing = timing;
        DemoTime = demoTime; 
        LegsUsed = legsUsed;
        Sets = sets > 0 ? sets : 1;
        ZoneSequence = zoneSequence ?? throw new ArgumentNullException(nameof(zoneSequence), "Each exercise must have a zone sequence.");
    }
}
public static class ExerciseList
{
    public static List<ExerciseData> Exercises = new List<ExerciseData>
    {
        new ExerciseData( 
            exerciseID: 1,
            name: "Single-Leg Stance - Right Leg",
            timing: 30,
            demoTime: 3,
            legsUsed: "right",
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (30, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.0), (-6.0, -1.1)),
            }
        ),
        new ExerciseData( 
            exerciseID: 2,
            name: "Single-Leg Stance - Left Leg",
            timing: 30,
            demoTime: 3,
            legsUsed: "left",
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (30, (-1.0, 1.0), (-1.0, 1.0), (1.0, 2.0), (-6.0, -1.1))
            }
        ),
        new ExerciseData( 
            exerciseID: 3,
            name: "Squats With Toe Rise",
            timing: 30,
            demoTime: 6,
            legsUsed: "both",
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.0), (-4.0, -2.0)), 
                (1, (-1.0, 1.0), (-1.0, 1.0), (1.0, 2.0), (1.0, 4.0)),
                (2, (0.0, 1.5), (-4.5, 4.5), (-1.0, 0.0), (-6.0, -4.5)),
                (2, (0.0, 1.5), (-4.5, 4.5), (1.5, 2.0), (4.5, 6.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 4,
            name: "Vertical Jumps",
            timing: 30,
            demoTime: 6,
            legsUsed: "both",
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.0), (-5.0, 5.0), (-2.0, 1.5), (-6.0, 6.0)),
                (2, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 5,
            name: "Squats Walking Lunges - Right Leg",
            timing: 50,
            demoTime: 5,
            legsUsed: "right",
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.0, 1.0), (-4.0, 4.0), (-1.0, 1.5), (-5.0, 5.0)),
                (8, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0)) //no COP check!!
            }
        ),
        new ExerciseData( 
            exerciseID: 6,
            name: "Squats Walking Lunges - Left Leg",
            timing: 50,
            demoTime: 5,
            legsUsed: "left",
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
               (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (1.0, 2.0), (-1.5, 1.5), (0.0, 1.0), (-2.0, 2.0)),
                (8, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 7,
            name: "Jumping - Lateral Jumps Right",
            timing: 60,
            demoTime: 5,
            legsUsed: "right",
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-1.8, 1.2), (-6.0, 2.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-1.2, 1.8), (-6.0, 2.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 8,
            name: "Jumping - Lateral Jumps Left",
            timing: 60,
            demoTime: 5,
            legsUsed: "left",
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0)),
                (2, (-1.5, 1.0), (-5.0, 5.0), (-2.0, 1.5), (-6.0, 6.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 9,
            name: "Squats - One-leg Squats Right",
            timing: 60,
            demoTime: 5,
            legsUsed: "right",
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.0), (-3.5, 3.5), (-2.0, 1.5), (-6.0, 6.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.5), (-6.0, 6.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 10,
            name: "Squats - One-leg Squats Left",
            timing: 60,
            demoTime: 5,
            legsUsed: "left",
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.0), (-3.5, 3.5), (-2.0, 1.5), (-6.0, 6.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.5), (-6.0, 6.0))
            }
        ),
    new ExerciseData( 
            exerciseID: 11,
            name: "Jumping - Box Jumps",
            timing: 30,
            demoTime: 5,
            legsUsed: "both",
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (28, (-1.5, 1.5), (-3.0, 3.0), (-2.0, 2.0), (-4.5, 4.5))
            }
        )
    };
}
