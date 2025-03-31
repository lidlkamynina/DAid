using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // For the gif display
using System;
using UnityEngine.TextCore.Text;

#region Data Structures
// This local ExerciseConfig mirrors the structure sent by the client.
// (It uses the same ZoneSequenceItem type as defined in HMDDataReceiver.)
[System.Serializable]
public class ExerciseConfig
{
    public int RepetitionID;
    public string Name;
    public string LegsUsed;
    public int Intro;
    public int Demo;
    public int PreparationCop;
    public int TimingCop;
    public int Release;
    public int Switch;
    public int Sets;
    public HMDDataReceiver.ZoneSequenceItem[] ZoneSequence;
}
#endregion

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Test Settings")]
    public bool bypassClientConnect = true;

    [Header("UI Elements")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI leftFootInstructionText;
    public TextMeshProUGUI rightFootInstructionText;
    public TextMeshProUGUI reptestText;
    [Header("Audio/Animation")]
    public AudioSource audioSource;
    public Animator characterAnimator;

    [Header("Visual Indicator")]
    public GameObject indicatorSphere; // (May be unused)

    [Header("GIF Display Settings (Exercise ID 3)")]
    // This Image should be a window above your avatar.
    public Image gifDisplay;
    // The demo sequence expects 4 GIFs in this order:
    // step1 (1 sec), step2 (3 sec), step3 (1 sec), step4 (1 sec).
    public Sprite[] exercise3DemoGifs; // assign 4 sprites in the Inspector
    // The execution guidance GIFs for the rep cycle.
    // Expected order: step1, step2, step3, step4.
    public Sprite[] exercise3ExecutionGifs; // assign 4 sprites in the Inspector

    // List of exercise configurations received from the client.
    public List<ExerciseConfig> exerciseConfigs = new List<ExerciseConfig>();

    // Reference to the currently active exercise configuration.
    public ExerciseConfig currentExercise;

    // Control flags.
    private bool preparationSuccessful = false;
    private bool restartExerciseRequested = false;
    int reps = 1;
    int repss = 0; // ex 6 logic
    string right = "right";
    string left = "left";

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (bypassClientConnect)
        {
            CreateDummyExercises();
        }
        // In production, the client will send ExerciseConfig messages,
        // and UpdateExerciseConfiguration() will add them to exerciseConfigs.

        // Check the current exercise configuration to decide which sequence to run.
        if (exerciseConfigs.Count > 0)
        {
            currentExercise = exerciseConfigs[0];
            if (currentExercise.RepetitionID == 3)
            {
                StartCoroutine(RunSequenceForExercise3());
                return;
            }
        }
        // Otherwise run the default routine.
        StartCoroutine(RunSequence());
    }

    // ------------------- Main Exercise Flow -------------------

    IEnumerator RunSequence()
    {

        reptestText.gameObject.SetActive(false);
        FootOverlayManagerTwoFeet.Instance.setDefaultGreen();
        FootOverlayManagerTwoFeet.Instance.SetActiveFoot("none");
        gifDisplay.gameObject.SetActive(false);
        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(false);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(false);
        yield return WaitForClientConnection();
        yield return RunIntroStep();
        yield return RunDemoStep();


        yield return RunPreparationPhase();
        yield return RunExerciseExecution();
        reptestText.gameObject.SetActive(false);

        yield return new WaitForSeconds(2f); // undo this when done

        currentExercise = exerciseConfigs[2]; // change to 2 when done
        //gifDisplay.gameObject.SetActive(true);
        yield return RunSequenceForExercise3();

        gifDisplay.gameObject.SetActive(false);
        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(false);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(false);

        reptestText.gameObject.SetActive(false);
        yield return new WaitForSeconds(2f);

        currentExercise = exerciseConfigs[3]; // change to 3 when done
        //gifDisplay.gameObject.SetActive(true);
        yield return RunSequenceForExercise4();
        FootOverlayManagerTwoFeet.Instance.setDefaultGreen();
        gifDisplay.gameObject.SetActive(false);
        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(false);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(false);
        characterAnimator.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        
        currentExercise = exerciseConfigs[4];
        yield return StartCoroutine(AnimateRotation(characterAnimator.transform, Quaternion.Euler(0, -45, 0), Quaternion.Euler(0, -90, 0), 0.25f));
        yield return RunSequenceForExercise5();
        FootOverlayManagerTwoFeet.Instance.setDefaultGreen();
        gifDisplay.gameObject.SetActive(false);
        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(false);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(false);
        yield return new WaitForSeconds(2f);
        //// Now check if a new exercise (ID 3) has arrived.
        currentExercise = exerciseConfigs[5]; // 5 real id
        yield return StartCoroutine(AnimateRotation(characterAnimator.transform, Quaternion.Euler(0, -90, 0), Quaternion.Euler(0, -45, 0), 0.25f));
        yield return RunSequenceForExercise6();

        FootOverlayManagerTwoFeet.Instance.setDefaultGreen();

        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(false);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(false);
        reptestText.gameObject.SetActive(false);
        //yield return WaitForClientConnection();
        yield return new WaitForSeconds(2f);
        // Now check if a new exercise (ID 3) has arrived.
        currentExercise = exerciseConfigs[6];

        yield return RunSequenceForExercise7();

        FootOverlayManagerTwoFeet.Instance.setDefaultGreen();

        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(false);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(false);
        reptestText.gameObject.SetActive(false);
        //yield return WaitForClientConnection();
        yield return new WaitForSeconds(2f);
       
        currentExercise = exerciseConfigs[8];

        yield return RunSequenceForExercise8();
    }
    //IEnumerator DemonstrateExercise5()
    //{
    //    instructionText.text = "Vingrojums: Stāvēšana uz vienas kājas 30 sekundes";
    //    yield return new WaitForEndOfFrame();

    //    Debug.Log("Setting trigger ExTest");
    //    characterAnimator.SetTrigger("ExTest");


    //}

    // New routine for Exercise ID 3.
    IEnumerator RunSequenceForExercise3()
    {
        yield return RunDemoStepForExercise3();
        yield return RunPreparationPhaseForExercise3();

        // Loop for each set as defined in the exercise config.
        for (int set = 0; set < currentExercise.Sets; set++)
        {
            // Run the execution phase (30-sec timer with rep cycles and restart handling)
            yield return RunExerciseExecutionForExercise3();

            // Release Phase
            audioManager.Instance.StopAllAudio();
            audioManager.Instance.PlayReleaseLeg();
            instructionText.color = Color.cyan;
            characterAnimator.SetTrigger("Idle");
            FootOverlayManagerTwoFeet.Instance.setDefaultGreen();
            instructionText.text = "Nostājies uz ABĀM kājām";
            yield return StartCountdown(currentExercise.Release);
            yield return WaitForExerciseConfigs();

            // If not the final set, run preparation again before the next set.
            if (set < currentExercise.Sets - 1)
            {
                yield return RunPreparationPhaseForExercise3();
            }
        }
        instructionText.text = "";
    }
    IEnumerator RunSequenceForExercise4()
    {
        yield return RunDemoStepForExercise4();
        yield return RunPreparationPhaseForExercise4();

        // Loop for each set as defined in the exercise config.
        for (int set = 0; set < currentExercise.Sets; set++)
        {
            // Run the execution phase (30-sec timer with rep cycles and restart handling)
            yield return RunExerciseExecutionForExercise4();

            // Release Phase
            audioManager.Instance.StopAllAudio();
            audioManager.Instance.PlayReleaseLeg();
            instructionText.color = Color.cyan;
            characterAnimator.SetTrigger("Idle");
            FootOverlayManagerTwoFeet.Instance.setDefaultGreen();
            instructionText.text = "Nostājies uz ABĀM kājām";
            yield return StartCountdown(currentExercise.Release);
            yield return WaitForExerciseConfigs();

            // If not the final set, run preparation again before the next set.
            if (set < currentExercise.Sets - 1)
            {
                yield return RunPreparationPhaseForExercise4();
            }
        }
        instructionText.text = ""; // Demo has ended, close the application. for non full app ver
    }
    IEnumerator RunSequenceForExercise5()
    {
        yield return RunDemoStepForExercise5();
        yield return RunPreparationPhaseForExercise5();

        // Loop for each set as defined in the exercise config.
        for (int set = 0; set < currentExercise.Sets; set++)
        {
            // Run the execution phase (30-sec timer with rep cycles and restart handling)
            yield return RunExerciseExecutionForExercise5();

            // Release Phase
            audioManager.Instance.StopAllAudio();
            audioManager.Instance.PlayReleaseLeg();
            instructionText.color = Color.cyan;
            SetGif(exercise3DemoGifs[8]);
            FootOverlayManagerTwoFeet.Instance.SetActiveFoot("both");
            FootOverlayManagerTwoFeet.Instance.setDefaultGreen();
            instructionText.text = "Nostājies uz ABĀM kājām";
            yield return StartCountdown(currentExercise.Release);
            instructionText.text = "";
            //yield return WaitForExerciseConfigs();

            // If not the final set, run preparation again before the next set.
            if (set < currentExercise.Sets - 1)
            {
                yield return RunPreparationPhaseForExercise5();
            }
        }
        instructionText.text = "";
    }

    IEnumerator RunSequenceForExercise6()
    {
        yield return RunDemoStepForExercise6();
        yield return RunPreparationPhaseForExercise6();

        // Loop for each set as defined in the exercise config.
        for (int set = 0; set < currentExercise.Sets; set++)
        {
            // Run the execution phase (30-sec timer with rep cycles and restart handling)
            yield return RunExerciseExecutionForExercise6();

            // Release Phase
            if (set == 0)
            {
                reptestText.text = $"\n Set <color=#00FFFF>{sets2}</color> / 2";
            }
            else if (set == 1)
            {
                reptestText.text = "";
            }
            audioManager.Instance.StopAllAudio();
            audioManager.Instance.PlayReleaseLeg();
            instructionText.color = Color.cyan;
            FootOverlayManagerTwoFeet.Instance.SetActiveFoot("both");
            FootOverlayManagerTwoFeet.Instance.setDefaultGreen();
            SetGif(exercise3DemoGifs[8]);
            instructionText.text = "Nostājies uz ABĀM kājām";
            characterAnimator.ResetTrigger("Ex5");
            characterAnimator.SetTrigger("Idle");
            yield return StartCountdown(currentExercise.Release);
            instructionText.text = "";
            //yield return WaitForExerciseConfigs();

            // If not the final set, run preparation again before the next set.
            if (set < currentExercise.Sets - 1)
            {
                yield return RunPreparationPhaseForExercise6();
            }
        }
        instructionText.text = "";
    }
    IEnumerator RunSequenceForExercise7()
    {
        yield return RunDemoStepForExercise7();
        yield return RunPreparationPhaseForExercise7();

        // Loop for each set as defined in the exercise config.
        for (int set = 0; set < 4; set++)
        {
            // Run the execution phase (30-sec timer with rep cycles and restart handling)
            yield return RunExerciseExecutionForExercise7();


            // Release Phase
            if (repss < 2)
            {
                reptestText.text = $"Set <color=#00FFFF>1</color> / 2";
            }
            else
            {
                reptestText.text = $"Set <color=#00FFFF>2</color> / 2";
            }
            audioManager.Instance.StopAllAudio();
            instructionText.text = "";
            audioManager.Instance.PlayReleaseLeg();
            SetGif(exercise3DemoGifs[8]);
            FootOverlayManagerTwoFeet.Instance.setDefaultGreen();
            FootOverlayManagerTwoFeet.Instance.SetActiveFoot("both");
            instructionText.color = Color.cyan;
            characterAnimator.Play("Idle", 0, 0f);
            instructionText.text = "Nostājies uz ABĀM kājām";

            yield return StartCountdown(currentExercise.Release);
            instructionText.text = "";
            yield return WaitForExerciseConfigs();

            // If not the final set, run preparation again before the next set.
            if (set < 3)
            {
                yield return RunPreparationPhaseForExercise7();
            }
        }
        instructionText.text = "";
    }

    IEnumerator RunSequenceForExercise8()
    {
        yield return RunDemoStepForExercise8();
        yield return RunPreparationPhaseForExercise5();

        // Loop for each set as defined in the exercise config.
        for (int set = 0; set < currentExercise.Sets; set++)
        {
            // Run the execution phase (30-sec timer with rep cycles and restart handling)
            yield return RunExerciseExecutionForExercise8();

            // Release Phase
            if (set == 0)
            {
                reptestText.text = $"\n Set <color=#00FFFF>{sets4}</color> / 2";
            }
            else if (set == 1)
            {
                reptestText.text = "";
            }
            audioManager.Instance.StopAllAudio();
            audioManager.Instance.PlayReleaseLeg();
            SetGif(exercise3DemoGifs[8]);
            instructionText.color = Color.cyan;
            characterAnimator.Play("Idle", 0, 0f);
            // FootOverlayManagerTwoFeet.Instance.SetActiveFoot("both");
            instructionText.text = "Nostājies uz ABĀM kājām";
            yield return StartCountdown(currentExercise.Release);
            instructionText.text = "";
            
            //yield return WaitForExerciseConfigs();

            // If not the final set, run preparation again before the next set.
            if (set < currentExercise.Sets - 1)
            {
                yield return RunPreparationPhaseForExercise5();
            }
        }
        instructionText.text = "Demonstration has ended, please close the application.";
    }

    IEnumerator WaitForClientConnection() // wait for client to send tcp connection establish
    {
        if (bypassClientConnect)
        {
            instructionText.text = "Test režīms: klienta savienojums izlaists.";
            yield return new WaitForSeconds(1f);
        }
        else
        {
            instructionText.text = "Gaida savienojumu ar klientu...";
            while (HMDDataReceiver.Instance == null || !HMDDataReceiver.Instance.IsClientConnected)
            {
                yield return new WaitForSeconds(1f);
            }
            instructionText.text = "Klients savienots!";
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator RunIntroStep()
    {
        // use the Intro timing from the first exercise 
        FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
        if (exerciseConfigs.Count > 0)
        {
            audioManager.Instance.PlayIntro();
            instructionText.text = "Esi sveicināts FIFA11+ treniņu programmā";
            yield return StartCountdown(exerciseConfigs[0].Intro);
        }
        else
        {
            yield return StartCountdown(50); // fallback in case something goes wrong
        }
    }

    // ------------------- Default Demo, Preparation & Execution (for ID 1 & 2) -------------------

    IEnumerator RunDemoStep()
    {
        // Run demo animation using the first exercise as reference.
        FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
        if (exerciseConfigs.Count > 0)
        {
            currentExercise = exerciseConfigs[0];
            yield return DemonstrateExercise();
        }
        else
        {
            yield return StartCountdown(50);
        }
    }
    public int sets5 = -1;

    public int sets13 = -1;
    IEnumerator RunPreparationPhase()
    {
        
        reptestText.gameObject.SetActive(true);
        preparationSuccessful = false;
        if (sets13 < 1)
        {
            reptestText.text = $"Set <color=#00FFFF>1</color> / 2";
        }
        else
        {
            reptestText.text = $"Set <color=#00FFFF>2</color> / 2";
        }

        while (!preparationSuccessful)
        {
            // Determine the correct exercise configuration based on reps
            if (reps == 1 || reps == 3)
            {
                currentExercise = exerciseConfigs.Find(e => e.RepetitionID == 1); // Right leg config
            }
            else if (reps == 2 || reps == 4)
            {
                currentExercise = exerciseConfigs.Find(e => e.RepetitionID == 2); // Left leg config
            }

            if (currentExercise == null)
            {
                Debug.LogError("Current exercise config not found!");
                yield break;
            }

            if (reps == 1 || reps == 3)
            {
                audioManager.Instance.PlayPreparation(right);
                instructionText.text = "GATAVOTIES! Nostājies uz LABĀS kājas";
                UpdateActiveFootDisplay();
                instructionText.color = Color.cyan;
            }
            else if (reps == 2 || reps == 4)
            {
                audioManager.Instance.PlayPreparation(left);
                instructionText.text = "GATAVOTIES! Nostājies uz KREISĀS kājas";
                instructionText.color = Color.cyan;
                UpdateActiveFootDisplay();
            }

            bool isRightLeg = (reps == 1 || reps == 3);
            characterAnimator.SetBool("IsLeftLeg", !isRightLeg);
            yield return new WaitForEndOfFrame();
            characterAnimator.SetTrigger("StartExercise");
            // Freeze the animation without resuming
            yield return FreezeAtLastFrame(currentExercise.PreparationCop, false);
            characterAnimator.ResetTrigger("StartExercise");
            

            if (restartExerciseRequested)
            {
                restartExerciseRequested = false;
                instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
                yield return new WaitForSeconds(1f); // wait before restarting
            }
            else
            {
                
                preparationSuccessful = true;
            }
        }
        sets13++;
    }


    
    IEnumerator RunExerciseExecution()
    {
        int configIndex = 0;

        while (reps < 5)
        {
            instructionText.color = Color.white;
            
                
            
            if(sets13 < 1 && reps < 3)
            {
                reptestText.text = $"Rep <color=#00FFFF>{reps}</color> / 2 \nSet <color=#00FFFF>1</color> / 2";
            }
            else if ( reps == 3)
            {
                reptestText.text = $"Rep <color=#00FFFF>1</color> / 2 \nSet <color=#00FFFF>2</color> / 2";
            }
            else if (reps == 4)
            {
                reptestText.text = $"Rep <color=#00FFFF>2</color> / 2 \nSet <color=#00FFFF>2</color> / 2";
            }

            currentExercise = exerciseConfigs[configIndex];
            
            //if(sets13 < 3)
            //{
            //    instructionText.text = "STARTS! Turi līdzsvaru 30 sekundes!";
            //    audioManager.Instance.PlayNoturi();
            //}
            //else
            //{
            //    instructionText.text = "";
               
            //}
            
           // yield return StartCountdown(4);  // 4-second delay before starting the timer

            // Freeze the animation during the execution timer.
            characterAnimator.speed = 0;
            float initialDuration = 4f;
            float exerciseDuration = currentExercise.TimingCop; // e.g., 6 seconds if you want a total of 10 seconds
            float totalTime = initialDuration + exerciseDuration;
            bool audioPlayed = false;
            // Begin the 30-second timer loop with restart handling.
            bool timerCompleted = false;
            while (!timerCompleted)
            {
                float timer = currentExercise.TimingCop; // full 30-second duration
                while (totalTime > 0)
                {
                    countdownText.text = Mathf.CeilToInt(totalTime).ToString();
                    totalTime -= Time.deltaTime;
                    if (totalTime > exerciseDuration)
                    {
                        // Only trigger this once.
                        if (!audioPlayed)
                        {
                            if (sets13 < 2)
                            {
                                instructionText.text = "STARTS! Turi līdzsvaru 30 sekundes!";
                                audioManager.Instance.PlayNoturi();
                                audioPlayed = true;
                            }
                            else
                            {
                                instructionText.text = "";
                            }
                            
                        }
                    }
                    // Check if a restart has been triggered.
                    if (restartExerciseRequested)
                    {
                        break;
                    }
                    yield return null;

                }
                if (restartExerciseRequested)
                {
                    audioManager.Instance.StopAllAudio();
                    audioManager.Instance.PlayExerciseZoneVoice(7);// A restart occurred during the timer.
                    instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
                    // (Optionally, play restart audio here.)
                    restartExerciseRequested = false;  // Reset the flag.
                    yield return StartCountdown(5);       // 5-second restart countdown.
                                                          // Then restart the 30-second timer from scratch.
                    continue;
                }
                else
                {
                    timerCompleted = true; // The timer completed normally.
                }
            }

            // --- Release Phase ---
            // Unfreeze the animation and transition to idle.
            audioManager.Instance.StopAllAudio();
            characterAnimator.speed = 1;
  
            

            characterAnimator.ResetTrigger("StartExercise");
            characterAnimator.SetTrigger("Idle");
            audioManager.Instance.StopAllAudio();
            audioManager.Instance.PlayReleaseLeg();
            instructionText.text = "Nostājies uz ABĀM kājām";
            instructionText.color = Color.cyan;
            FootOverlayManagerTwoFeet.Instance.SetActiveFoot("both");

            yield return StartCountdown(currentExercise.Release);
            yield return WaitForExerciseConfigs();
            reps++;
            sets5++;
            configIndex = (configIndex + 1) % 2;
            if (reps < 5)
            {
                yield return RunPreparationPhase();
            }
        }
    }




    // ------------------- New Methods for Exercise ID 3 -------------------


    // ------------------- New Methods for Exercise ID 3 -------------------


    IEnumerator RunDemoStepForExercise3()
    {
        FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.speed = 1;
        characterAnimator.SetFloat("SpeedMultiplier", 0.9667f); // 2.5f for regular last one
        // Play demo audio and show demo instructions.
        audioManager.Instance.PlayDemo2();
        instructionText.color = Color.cyan;
        instructionText.text = "Vingrojums 2: Pietupiens ar pirkstgalu celšanu 30 sekundes";

        // Trigger the demo animation (assumes characterAnimator is your Animator reference)



        // Update instructions according to the rep cycle timing (total 6 seconds)
        // Step 1: 0-3 seconds
        // instructionText.text = "Veic pietupienu uz leju līdz 90 grādiem!";
        characterAnimator.SetTrigger("ExTest2");
        yield return StartCountdown(6);

        // Step 2: 3-4 seconds
        //instructionText.text = "Celies augšā!";
        // yield return StartCountdown(1);

        // Step 3: 4-5 seconds
        // instructionText.text = "Uz pirkstgaliem!";
        // yield return StartCountdown(1);

        // Step 4: 5-6 seconds
        // instructionText.text = "Uz abām kājām!";
        // yield return StartCountdown(1);
        characterAnimator.ResetTrigger("ExTest2");
    }

    IEnumerator RunPreparationPhaseForExercise3()
    {
        reptestText.gameObject.SetActive(true);
        preparationSuccessful = false;
        while (!preparationSuccessful)
        {

            reptestText.text = $"Set {sets6} / 2";
            characterAnimator.SetTrigger("Idle");
            audioManager.Instance.PlayReleaseLeg();
            instructionText.text = "Nostājies uz ABĀM kājām";
            instructionText.color = Color.cyan;
            UpdateActiveFootDisplay();

            // Instead of showing a GIF, trigger the idle state for the avatar.


            yield return StartCountdown(currentExercise.PreparationCop);

            if (restartExerciseRequested)
            {
                restartExerciseRequested = false;
                instructionText.color = Color.white;
                instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
                yield return new WaitForSeconds(1f);
                yield return RunPreparationPhaseForExercise3();
                yield break;
            }
            else
            {
                preparationSuccessful = true;
            }
        }
    }
    int sets6 = 1;
    int repar = 0;
    IEnumerator RunExerciseExecutionForExercise3()
    {
        instructionText.text = "";
        repar = 1;
        instructionText.color = Color.white;
        UpdateActiveFootDisplay();
        float globalTimer = currentExercise.TimingCop; // Total execution time (e.g., 30 seconds)
        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(true);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(true);
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.SetTrigger("ExTest2");
        // Repeat the rep cycle as long as there’s remaining time.
        while (globalTimer > 0)
        {

            reptestText.text = $"Rep <color=#00FFFF>{repar}</color> / 5 \nSet <color=#00FFFF>{sets6}</color> / 2";
            if (globalTimer > 0)
            {
                // --- Trigger the rep cycle animation ---



                yield return null;
                // --- Step 1: 3 seconds ---
                float stepDuration = 3f;
                float stepTime = 0f;
                if(sets6 == 1)
                {
                    audioManager.Instance.PlayExercise3Step1();
                    instructionText.text = "Lēnām tupies 3, 2, 1";
                }
            
                while (stepTime < stepDuration)
                {
                    if (restartExerciseRequested)
                        break;
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                if (restartExerciseRequested)
                    break;

                // --- Step 2: 1 second ---
                stepDuration = 1f;
                stepTime = 0f;
                if (sets6 == 1)
                {
                    audioManager.Instance.PlayExercise3Step2();
                    instructionText.text = "Celies augšā!";
                }
                   
                while (stepTime < stepDuration)
                {
                    if (restartExerciseRequested)
                        break;
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                if (restartExerciseRequested)
                    break;

                // --- Step 3: 1 second ---
                stepDuration = 1f;
                stepTime = 0f;
                if (sets6 == 1)
                {
                    audioManager.Instance.PlayExercise3Step3();
                    instructionText.text = "Uz pirkstgaliem!";
                }
                    
                while (stepTime < stepDuration)
                {
                    if (restartExerciseRequested)
                        break;
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                if (restartExerciseRequested)
                    break;

                // --- Step 4: 1 second ---
                stepDuration = 1f;
                stepTime = 0f;
                if (sets6 == 1)
                {
                    audioManager.Instance.PlayExercise3Step4();
                    instructionText.text = "Uz abām kājām!";
                }
                    
                while (stepTime < stepDuration)
                {
                    if (restartExerciseRequested)
                        break;
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                if (restartExerciseRequested)
                    break;



                repar++;
            }
            else
            {
                // If less than a full rep cycle remains, count down the remaining time.
                float remaining = globalTimer;
                while (remaining > 0)
                {
                    if (restartExerciseRequested)
                        break;
                    remaining -= Time.deltaTime;
                    globalTimer = remaining;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                break;
            }
        }

        // --- Restart Handling ---
        if (restartExerciseRequested)
        {
            characterAnimator.speed = 1; // Unfreeze animation.
            audioManager.Instance.StopAllAudio();
            characterAnimator.ResetTrigger("ExTest2");
            characterAnimator.SetTrigger("Idle");
            // Inform the user about the restart.
            audioManager.Instance.PlayExerciseZoneVoice(7);
            instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
            restartExerciseRequested = false;
            yield return StartCountdown(5); // 5-second restart countdown.
            repar = 1;
            // Restart the execution phase.
            yield return RunExerciseExecutionForExercise3();
            yield break;
        }
        characterAnimator.ResetTrigger("ExTest2");
        sets6++;
        yield break;
    }


    IEnumerator HandleRestart3()
    {
        // Immediately stop any audio.
        audioManager.Instance.StopAllAudio();

        // Inform the user about the restart.
        audioManager.Instance.PlayExerciseZoneVoice(7);
        instructionText.color = Color.white;
        instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
        restartExerciseRequested = false;
        repar = 1;
        // Transition to idle to stop the current rep cycle.
        characterAnimator.SetTrigger("Idle");

        // Initiate a 5-second restart countdown.
        yield return StartCountdown(5);

        // Restart the execution phase.
        yield return RunExerciseExecutionForExercise3();
    }

    IEnumerator ExecuteRepCycleForExercise3()
    {


        // Step 1: 3 seconds.
        instructionText.text = "Veic pietupienu uz leju līdz 90 grādiem!";
        ShowExecutionGif(1);
        yield return new WaitForSeconds(3f);

        // Step 2: 1 second.
        instructionText.text = "Celies augšā!";
        ShowExecutionGif(2);
        yield return new WaitForSeconds(1f);

        // Step 3: 1 second.
        instructionText.text = "Uz pirkstgaliem";
        ShowExecutionGif(3);
        yield return new WaitForSeconds(1f);

        // Step 4: 1 second.
        instructionText.text = "Nostājies uz abām kājām";
        ShowExecutionGif(0);
        yield return new WaitForSeconds(1f);

    }

    IEnumerator ShowDemoGifSequenceForExercise3()
    {
        // Ensure that exactly four demo GIFs have been assigned.
        if (exercise3DemoGifs != null)
        {

            SetGif(exercise3DemoGifs[1]);
            yield return StartCountdown(3);

            SetGif(exercise3DemoGifs[2]);
            yield return StartCountdown(1);

            SetGif(exercise3DemoGifs[3]);
            yield return StartCountdown(1);

            SetGif(exercise3DemoGifs[0]);
            yield return StartCountdown(1);
        }
        HideGifDisplay();
    }

    void ShowExecutionGif(int index)
    {
        if (exercise3ExecutionGifs != null && exercise3ExecutionGifs.Length > index)
        {
            SetGif(exercise3ExecutionGifs[index]);
        }
    }

    // Helper to set the sprite of the gif display.
    void SetGif(Sprite gifSprite)
    {
        if (gifDisplay != null)
        {
            gifDisplay.sprite = gifSprite;
            gifDisplay.gameObject.SetActive(true);
        }
    }

    // Helper to hide the gif display.
    void HideGifDisplay()
    {
        if (gifDisplay != null)
        {
            gifDisplay.gameObject.SetActive(false);
        }
    }


    // ------------------- End New Methods for Exercise ID 3 -------------------

    // ex ID 4

    IEnumerator RunDemoStepForExercise4()
    {
        // Play demo audio and show demo instructions.
        FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
        instructionText.color = Color.cyan;
        audioManager.Instance.PlayDemo3();
        instructionText.text = "Vingrojums 3: Vertikālie lēcieni";
        // Show the demo GIF sequence.
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.speed = 1;
        characterAnimator.SetFloat("SpeedMultiplier", 0.6667f);

        characterAnimator.SetTrigger("Ex3");
        yield return StartCountdown(6);
        characterAnimator.ResetTrigger("Ex3");
    }

    IEnumerator RunPreparationPhaseForExercise4()
    {
        preparationSuccessful = false;
        while (!preparationSuccessful)
        {
            characterAnimator.SetTrigger("Idle");
            reptestText.gameObject.SetActive(true);
          
            reptestText.text = $"Set <color=#00FFFF>{sets7}</color> / 2";
            audioManager.Instance.PlayReleaseLeg();
            instructionText.color = Color.cyan;
            instructionText.text = "Nostājies uz ABĀM kājām";
            UpdateActiveFootDisplay();

            yield return StartCountdown(currentExercise.PreparationCop);

            if (restartExerciseRequested)
            {
                restartExerciseRequested = false;
                instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
                yield return new WaitForSeconds(1f);
            }
            else
            {
                preparationSuccessful = true;
            }
        }
    }
    int sets7 = 1;
    int repars = 1;

    IEnumerator RunExerciseExecutionForExercise4()
    {
        instructionText.text = "";
        instructionText.color = Color.white;
        UpdateActiveFootDisplay();
        float globalTimer = currentExercise.TimingCop; // Total execution time (e.g., 30 seconds)
        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(true);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(true);
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.SetTrigger("Ex3");
        repars = 1;
        while (globalTimer > 0)
        {

            reptestText.text = $"Rep <color=#00FFFF>{repars}</color> / 5 \nSet <color=#00FFFF>{sets7}</color> / 2";

            // If there is time for a full rep cycle (6 sec), execute it step by step.
            if (globalTimer != 0)

            {
                // --- Step 1: 2 seconds ---
                float stepDuration = 2f;
                float stepTime = 0f;
                if (sets7 == 1)
                {
                    audioManager.Instance.PlayExercise4Step1();
                    instructionText.text = "Veic pietupienu uz leju!";
                }
                while (stepTime < stepDuration)
                {
                    if (restartExerciseRequested)
                        break;
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                if (restartExerciseRequested)
                    break;

                // --- Step 2: 2 second ---
                stepDuration = 2f;
                stepTime = 0f;
                if (sets7 == 1)
                {
                    audioManager.Instance.PlayExercise4Step2();
                    instructionText.text = "Noturi pozīciju!";
                }
                while (stepTime < stepDuration)
                {
                    if (restartExerciseRequested)
                        break;
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                if (restartExerciseRequested)
                    break;

                // --- Step 3: 1 second ---
                stepDuration = 1f;
                stepTime = 0f;
                if (sets7 == 1)
                {
                    audioManager.Instance.PlayExercise4Step3();
                    instructionText.text = "Lec augšā!";
                }
                while (stepTime < stepDuration)
                {
                    if (restartExerciseRequested)
                        break;
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                if (restartExerciseRequested)
                    break;

                // --- Step 4: 1 second ---
                stepDuration = 1f;
                stepTime = 0f;
                if (sets7 == 1) 
                {
                    audioManager.Instance.PlayExercise4Step4();
                    instructionText.text = "Uz pirkstgaliem!";
                }
                while (stepTime < stepDuration)
                {
                    if (restartExerciseRequested)
                        break;
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                if (restartExerciseRequested)
                    break;
                repars++;

            }
            else
            {
                // If less than 6 seconds remain, simply count down the remaining time.
                float remaining = globalTimer;
                while (remaining > 0)
                {
                    if (restartExerciseRequested)
                        break;
                    remaining -= Time.deltaTime;
                    globalTimer = remaining;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
                break;
            }

            // If a restart was requested during the cycle, break out to handle it.
            if (restartExerciseRequested)
                break;
        }

        // --- Restart Handling ---
        if (restartExerciseRequested)
        {
            characterAnimator.speed = 1; // Unfreeze animation.
            audioManager.Instance.StopAllAudio();
            characterAnimator.ResetTrigger("Ex3");
            characterAnimator.SetTrigger("Idle");
            // Inform the user about the restart.
            audioManager.Instance.PlayExerciseZoneVoice(7);
            instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
            restartExerciseRequested = false;
            yield return StartCountdown(5); // 5-second restart countdown.
            repars = 1;
            // Restart the execution phase.
            yield return RunExerciseExecutionForExercise3();
            yield break;
        }
        characterAnimator.ResetTrigger("Ex3");
        sets7++;
        yield break;
    }


    IEnumerator ExecuteRepCycleForExercise4()
    {


        // Step 1: 3 seconds.
        instructionText.text = "Veic pietupienu uz leju līdz 90 grādiem!";
        SetGif(exercise3DemoGifs[4]);
        yield return new WaitForSeconds(2f);

        // Step 2: 1 second.
        instructionText.text = "Celies augšā!";
        SetGif(exercise3DemoGifs[5]);
        yield return new WaitForSeconds(2f);

        // Step 3: 1 second.
        instructionText.text = "Uz pirkstgaliem";
        SetGif(exercise3DemoGifs[6]);
        yield return new WaitForSeconds(1f);

        // Step 4: 1 second.
        instructionText.text = "Nostājies uz abām kājām";
        SetGif(exercise3DemoGifs[7]);
        yield return new WaitForSeconds(1f);

    }

    IEnumerator ShowDemoGifSequenceForExercise4()
    {
        // Ensure that exactly four demo GIFs have been assigned.


        characterAnimator.ResetTrigger("Idle");
        characterAnimator.speed = 1;
        characterAnimator.SetTrigger("Ex3");
        yield return StartCountdown(6);
        characterAnimator.ResetTrigger("Ex3");


    }

    // end ex ID 4

    // ex ID 5

    IEnumerator RunDemoStepForExercise5()
    {
        
        // Play demo audio and show demo instructions.
        FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
        audioManager.Instance.PlayDemo4();
        instructionText.color = Color.cyan;
        instructionText.text = "Vingrojums 4: Izklupiens uz priekšu";
        // Show the demo GIF sequence.
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.speed = 1;
        characterAnimator.SetFloat("SpeedMultiplier", 1.1917f);

        characterAnimator.SetTrigger("Ex4");
        yield return StartCountdown(4);
        characterAnimator.ResetTrigger("Ex4");
    }

    IEnumerator RunPreparationPhaseForExercise5()
    {
        preparationSuccessful = false;
        while (!preparationSuccessful)
        {
            characterAnimator.SetTrigger("Idle");
            audioManager.Instance.PlayReleaseLeg();
            instructionText.text = "Nostājies uz ABĀM kājām";
            reptestText.text = $"Set <color=#00FFFF>{sets}</color> / 2";
            UpdateActiveFootDisplay();
            SetGif(exercise3DemoGifs[8]);
            yield return StartCountdown(currentExercise.PreparationCop);

            if (restartExerciseRequested)
            {
                restartExerciseRequested = false;
                instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
                yield return new WaitForSeconds(1f);
                yield return RunPreparationPhaseForExercise5();

            }
            else
            {
                preparationSuccessful = true;
            }
        }
    }

    int repa = 0;
    int sets = 1;
    IEnumerator RunExerciseExecutionForExercise5()
    {
        instructionText.text = "";
        instructionText.color = Color.white;
        UpdateActiveFootDisplay();
        float globalTimer = currentExercise.TimingCop; // Total execution time (e.g., 30 seconds)
        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(true);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(true);
        reptestText.gameObject.SetActive(true);
        reptestText.text = $"Set <color=#00FFFF>{sets}</color> / 2";

        // --- Step 1: Execute once (1 second) ---
        if (globalTimer > 0)
        {
            float stepDuration = 1f;
            float stepTime = 0f;
            if (sets == 1)
            {
                audioManager.Instance.PlayExercise5Step1();
                instructionText.text = "Uz abām kājām!";
            }
            else { instructionText.text = ""; }
            SetGif(exercise3DemoGifs[8]);
            while (stepTime < stepDuration && globalTimer > 0)
            {
                if (restartExerciseRequested)
                {
                    yield return HandleRestart5();
                    yield break;
                }
                float delta = Time.deltaTime;
                stepTime += delta;
                globalTimer -= delta;
                countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                yield return null;
            }
        }
        else
        {
            yield break;
        }

        // --- Steps 2 & 3: Repeat 10 times ---
        for (int i = 0; i < 10; i++)
        {
            characterAnimator.ResetTrigger("Idle");
            characterAnimator.SetTrigger("Ex4");

            // reptestText.text = $"Rep {i+1}/10\nSet {sets}/2";
            reptestText.text = $"Rep <color=#00FFFF>{i + 1}</color> / 10 \nSet <color=#00FFFF>{sets}</color> / 2";
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 2f;
                float stepTime = 0f;
                if (sets == 1)
                {
                    audioManager.Instance.PlayExercise5Step2();
                    instructionText.text = "Izklupiens ar labo kāju!";
                }
                SetGif(exercise3DemoGifs[9]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart5();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }

            if (globalTimer <= 0)
                break;

            // Step 3: Izklupiens ar kreiso kāju! (2 seconds)
            {
                float stepDuration = 2f;
                float stepTime = 0f;
                if (sets == 1)
                {
                    audioManager.Instance.PlayExercise5Step3();
                    instructionText.text = "Izklupiens ar kreiso kāju!";
                }
                    SetGif(exercise3DemoGifs[10]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart5();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }

        }
        characterAnimator.ResetTrigger("Ex4");
        characterAnimator.SetTrigger("Idle");
        // --- Step 4: Execute once (8 seconds) ---
        if (globalTimer > 0)
        {
            float stepDuration = 8f;
            float stepTime = 0f;
           
            if (leftFootInstructionText != null)
                leftFootInstructionText.gameObject.SetActive(false);
            if (rightFootInstructionText != null)
                rightFootInstructionText.gameObject.SetActive(false);
            FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
            reptestText.text = $"Set <color=#00FFFF>{sets}</color> / 2";
            if (sets == 1)
            {
                audioManager.Instance.PlayExercise5Step4();
                instructionText.text = "Skriet atpakaļ uz sākumu!";
            }
            SetGif(exercise3DemoGifs[11]);
            while (stepTime < stepDuration && globalTimer > 0)
            {
                if (restartExerciseRequested)
                {
                    yield return HandleRestart5();
                    yield break;
                }
                float delta = Time.deltaTime;
                stepTime += delta;
                globalTimer -= delta;
                countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                yield return null;
            }
        }

        // Final restart check (just in case)
        if (restartExerciseRequested)
        {
            yield return HandleRestart5();
            yield break;
        }
        characterAnimator.ResetTrigger("Ex4");
        repa = 1;
        sets++;
        yield break;
    }

    IEnumerator HandleRestart5()
    {
        // Immediately stop any audio.
        audioManager.Instance.StopAllAudio();
        characterAnimator.ResetTrigger("Ex4");
        characterAnimator.SetTrigger("Idle");
        // Inform the user about the restart.
        // Inform the user about the restart.
        audioManager.Instance.PlayExerciseZoneVoice(7);
        instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
        restartExerciseRequested = false;

        // Initiate a 5-second restart countdown.
        yield return StartCountdown(5);

        // Restart the execution phase.
        yield return RunExerciseExecutionForExercise5();
    }





    IEnumerator ExecuteRepCycleForExercise5()
    {


        // Step 1: 3 seconds.
        instructionText.text = "Uz abām kājām!";
        SetGif(exercise3DemoGifs[8]);
        yield return new WaitForSeconds(1f);

        // Step 2: 1 second.
        instructionText.text = "Izklupiens ar labo kāju!";
        SetGif(exercise3DemoGifs[9]);
        yield return new WaitForSeconds(1f);

        // Step 3: 1 second.
        instructionText.text = "izklupiens ar kreiso kāju!";
        SetGif(exercise3DemoGifs[10]);
        yield return new WaitForSeconds(1f);

        // Step 4: 1 second.
        instructionText.text = "Skriet atpakaļ uz sākumu!";
        SetGif(exercise3DemoGifs[11]);
        yield return new WaitForSeconds(1f);

    }

    IEnumerator ShowDemoGifSequenceForExercise5()
    {
        // Ensure that exactly four demo GIFs have been assigned.
        if (exercise3DemoGifs != null)
        {

            SetGif(exercise3DemoGifs[8]);
            yield return StartCountdown(1);

            SetGif(exercise3DemoGifs[9]);
            yield return StartCountdown(2);

            SetGif(exercise3DemoGifs[10]);
            yield return StartCountdown(2);

            SetGif(exercise3DemoGifs[11]);
            yield return StartCountdown(1);
        }
        HideGifDisplay();
    }

    // end ex ID 5

    // ex ID 6 

    IEnumerator RunDemoStepForExercise6()
    {
        reptestText.text = "";
        // Play demo audio and show demo instructions.
        FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
        audioManager.Instance.PlayDemo5();
        instructionText.color = Color.cyan;
        instructionText.text = "Vingrojums 5: sānu lecieni";
        // Show the demo GIF sequence.
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.speed = 1;
        characterAnimator.SetFloat("SpeedMultiplier", 0.5f);

        characterAnimator.SetTrigger("Ex5");
        yield return StartCountdown(5);
        characterAnimator.ResetTrigger("Ex5");
    }

    IEnumerator RunPreparationPhaseForExercise6()
    {
        preparationSuccessful = false;
        while (!preparationSuccessful)
        {
            characterAnimator.SetTrigger("Idle");
            audioManager.Instance.PlayReleaseLeg();
            instructionText.text = "Nostājies uz ABĀM kājām";
            reptestText.text = $"Set <color=#00FFFF>{sets2}</color> / 2";
            UpdateActiveFootDisplay();
            
            yield return StartCountdown(currentExercise.PreparationCop);

            if (restartExerciseRequested)
            {
                restartExerciseRequested = false;
                instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
                yield return new WaitForSeconds(1f);
                yield return RunPreparationPhaseForExercise6();

            }
            else
            {
                preparationSuccessful = true;
            }
        }
    }

    int sets2 = 1;
    IEnumerator RunExerciseExecutionForExercise6()
    {
        UpdateActiveFootDisplay();
        float globalTimer = currentExercise.TimingCop; // Total execution time (e.g., 30 seconds)
        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(true);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(true);
        reptestText.gameObject.SetActive(true);
        reptestText.text = $"Set <color=#00FFFF>{sets2}</color> / 2";


        // --- Step 1: Execute once (1 second) ---
        if (globalTimer > 0)
        {
            float stepDuration = 1f;
            float stepTime = 0f;
            if (sets2 == 1)
            {
                audioManager.Instance.PlayExercise6Step1();
                instructionText.text = "Uz abām kājām!";
            }
            SetGif(exercise3DemoGifs[8]);
            while (stepTime < stepDuration && globalTimer > 0)
            {
                if (restartExerciseRequested)
                {
                    yield return HandleRestart6();
                    yield break;
                }
                float delta = Time.deltaTime;
                stepTime += delta;
                globalTimer -= delta;
                countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                yield return null;
            }
        }
        else
        {
            yield break;
        }
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.SetTrigger("Ex5");
        // --- Steps 2 & 3: Repeat 7 times ---
        for (int i = 0; i < 7; i++)
        {
            
            //reptestText.text = $"Rep {i + 1}/7\nSet {sets2}/2";
            reptestText.text = $"Rep <color=#00FFFF>{i + 1}</color> / 7 \nSet <color=#00FFFF>{sets2}</color> / 2";
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 2f;
                float stepTime = 0f;
                if (sets2 == 1)
                {
                    audioManager.Instance.PlayExercise6Step2();
                    instructionText.text = "Lec pa labi!";
                }
                    FootOverlayManagerTwoFeet.Instance.SetActiveFoot("right");
                if (leftFootInstructionText != null)
                    leftFootInstructionText.gameObject.SetActive(false);
                if (rightFootInstructionText != null)
                    rightFootInstructionText.gameObject.SetActive(true);
                SetGif(exercise3DemoGifs[12]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart6();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }

            if (globalTimer <= 0)
                break;

            // Step 3: Izklupiens ar kreiso kāju! (2 seconds)
            {
                float stepDuration = 2f;
                float stepTime = 0f;
                if (sets2 == 1)
                {
                    audioManager.Instance.PlayExercise6Step3();
                    instructionText.text = "Lec pa kreisi!";
                }
                SetGif(exercise3DemoGifs[13]);
                FootOverlayManagerTwoFeet.Instance.SetActiveFoot("left");
                if (leftFootInstructionText != null)
                    leftFootInstructionText.gameObject.SetActive(true);
                if (rightFootInstructionText != null)
                    rightFootInstructionText.gameObject.SetActive(false);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart6();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }



        }

        // Final restart check (just in case)
        if (restartExerciseRequested)
        {
            yield return HandleRestart6();
            yield break;
        }
        characterAnimator.ResetTrigger("Ex5");
        sets2++;
        yield break;
    }

    IEnumerator HandleRestart6()
    {
        characterAnimator.speed = 1; // Unfreeze animation.
        audioManager.Instance.StopAllAudio();
        characterAnimator.ResetTrigger("Ex5");
        characterAnimator.SetTrigger("Idle");
        // Inform the user about the restart.
        audioManager.Instance.PlayExerciseZoneVoice(7);
        instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
        restartExerciseRequested = false;

        // Initiate a 5-second restart countdown.
        yield return StartCountdown(5);

        // Restart the execution phase.
        yield return RunExerciseExecutionForExercise6();
    }


    IEnumerator ExecuteRepCycleForExercise6()
    {


        // Step 1: 1 seconds.
        instructionText.text = "Uz abām kājām!";
        SetGif(exercise3DemoGifs[12]);
        yield return new WaitForSeconds(1f);

        // Step 2: 2 second.
        instructionText.text = "Lec pa labi!";
        SetGif(exercise3DemoGifs[13]);
        yield return new WaitForSeconds(2f);

        // Step 3: 2 second.
        instructionText.text = "Lec pa kreisi!";
        SetGif(exercise3DemoGifs[14]);
        yield return new WaitForSeconds(2f);


    }

    IEnumerator ShowDemoGifSequenceForExercise6()
    {
        // Ensure that exactly four demo GIFs have been assigned.
        if (exercise3DemoGifs != null)
        {

            SetGif(exercise3DemoGifs[8]);
            yield return StartCountdown(1);

            SetGif(exercise3DemoGifs[12]);
            yield return StartCountdown(2);

            SetGif(exercise3DemoGifs[13]);
            yield return StartCountdown(2);

        }
        HideGifDisplay();
    }

    // end ex ID 6

    // ex ID 7

    IEnumerator RunDemoStepForExercise7()
    {
        // Play demo audio and show demo instructions.
        FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
        audioManager.Instance.PlayDemo6();
        instructionText.color = Color.cyan;
        instructionText.text = "Vingrojums 6: Stāvēšana uz vienas kājas ar biedru";
        // Show the demo GIF sequence.

        characterAnimator.ResetTrigger("Idle");
        characterAnimator.speed = 1;
        characterAnimator.SetFloat("SpeedMultiplier", 1f); // CHANGE THIS
         
        characterAnimator.SetTrigger("Ex6");
        yield return StartCountdown(5);
        characterAnimator.ResetTrigger("Ex6");
        characterAnimator.SetTrigger("Idle");
    }
    public int sets3 = -1;
    IEnumerator RunPreparationPhaseForExercise7()
    {
       
        preparationSuccessful = false;
        if (repss < 2)
        {
            reptestText.text = $"Set <color=#00FFFF>1</color> / 2";
        }
        else
        {
            reptestText.text = $"Set <color=#00FFFF>2</color> / 2";
        }
        while (!preparationSuccessful)
        {
            
            // Determine the correct exercise configuration based on reps
            if (repss == 0 || repss == 2)
            {
                currentExercise = exerciseConfigs.Find(e => e.RepetitionID == 7); // Right leg config
            }
            else if (repss == 1 || repss == 3)
            {
                currentExercise = exerciseConfigs.Find(e => e.RepetitionID == 8); // Left leg config
            }

            if (currentExercise == null)
            {
                Debug.LogError("Current exercise config not found!");
                yield break;
            }

            if (repss == 0 || repss == 2)
            {
                audioManager.Instance.PlayPreparation(right);
                instructionText.text = "GATAVOTIES! Nostājies uz LABĀS kājas";
                UpdateActiveFootDisplay();
                bool isRightLeg = (repss == 0 || repss == 2);
                characterAnimator.ResetTrigger("Idle");
                
                characterAnimator.SetBool("IsLeftLeg", !isRightLeg);
                yield return new WaitForEndOfFrame();
                characterAnimator.SetTrigger("Ex6");
                
                yield return null;
                yield return FreezeAtLastFrame(3, false);
                yield return null;
               
                characterAnimator.ResetTrigger("Ex6");


                characterAnimator.speed = 1;

                characterAnimator.Play("Idle", 0, 0f);
                //yield return StartCountdown(3);
                instructionText.text = "";
                SetGif(exercise3DemoGifs[16]);
               
               
            }
            else if (repss == 1 || repss == 3)
            {
                audioManager.Instance.PlayPreparation(left);
                instructionText.text = "GATAVOTIES! Nostājies uz KREISĀS kājas";
                UpdateActiveFootDisplay();
                
                bool isRightLeg = (repss == 1 || repss == 3);
                characterAnimator.ResetTrigger("Idle");

                characterAnimator.SetBool("IsLeftLeg", !isRightLeg);
                yield return new WaitForEndOfFrame();
                float clipLengthInSeconds = 300f; // total clip length
                float startTimeInSeconds = 150f;    // desired start time (2:30)
                float normalizedStartTime = startTimeInSeconds / clipLengthInSeconds;

                
                characterAnimator.SetTrigger("Ex6L");

                yield return null;
                yield return FreezeAtLastFrame(3, false);
                yield return null;
                
                characterAnimator.ResetTrigger("Ex6L");


                characterAnimator.speed = 1;
                characterAnimator.Play("Idle", 0, 0f);
                //yield return StartCountdown(3);
                instructionText.text = "";
                SetGif(exercise3DemoGifs[14]);
                
                
            }

            if (restartExerciseRequested)
            {
                restartExerciseRequested = false;
                instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
                yield return new WaitForSeconds(1f);
                yield return RunPreparationPhaseForExercise7();

            }
            else
            {
                preparationSuccessful = true;
            }
            
        }
    }

    
    IEnumerator RunExerciseExecutionForExercise7()
    {
        float clipLengthInSeconds = 300f; // total clip length
        float startTimeInSeconds = 150f;    // desired start time (2:30)
        float normalizedStartTime = startTimeInSeconds / clipLengthInSeconds;
        instructionText.text = "";
        instructionText.color = Color.white;
        UpdateActiveFootDisplay();
        characterAnimator.SetTrigger("Idle");
        float globalTimer = currentExercise.TimingCop; // Total execution time (e.g., 30 seconds)
        if (currentExercise.LegsUsed == "right")
        {
            if (rightFootInstructionText != null)
                rightFootInstructionText.gameObject.SetActive(true);
            leftFootInstructionText.gameObject.SetActive(false);
        }
        else
        {
            if (leftFootInstructionText != null)
                leftFootInstructionText.gameObject.SetActive(true);
            rightFootInstructionText.gameObject.SetActive(false);
        }


        reptestText.gameObject.SetActive(true);

        


        // --- Steps 1 & 2: Repeat 10 times ---
        for (int i = 0; i < 10; i++)
        {
            //if (sets3 < 1 && repss < 2)
            //{
            //    reptestText.text = $"Rep <color=#00FFFF>{repss}</color> / 10 \nSet <color=#00FFFF>1</color> / 2";
            //}
            //else if (repss > 3)
            //{
            //    reptestText.text = $"Rep <color=#00FFFF>{repss}</color> / 10 \nSet <color=#00FFFF>2</color> / 2";
            //}
            if (repss < 2)
            {
                reptestText.text = $"Rep <color=#00FFFF>{i + 1}</color> / 10 \nSet <color=#00FFFF>1</color> / 2";
            }
            else
            {
                reptestText.text = $"Rep <color=#00FFFF>{i + 1}</color> / 10 \nSet <color=#00FFFF>2</color> / 2";
            }
            //if (sets3 < 1)
            //{
            //    reptestText.text = $"Rep <color=#00FFFF>{i+1}</color> / 10 \nSet <color=#00FFFF>1</color> / 2";
            //}
            //else if (sets3 >= 1)
            //{
            //    reptestText.text = $"Rep <color=#00FFFF>{i+1}</color> / 10 \nSet <color=#00FFFF>2</color> / 2";
            //}
            
            characterAnimator.ResetTrigger("Idle");

            if (repss == 1 || repss == 3)
            {
                characterAnimator.SetTrigger("Ex6L");
            }
            else if (repss == 0 || repss == 2)
            {
                characterAnimator.SetTrigger("Ex6");
            }

            

            //reptestText.text = $"Rep {i + 1}/10\nSet {sets3}/4";
           // reptestText.text = $"Rep <color=#00FFFF>{i + 1}</color> / 10 \nSet <color=#00FFFF>{sets3}</color> / 2";
            if (globalTimer <= 0)
                break;

            // Step 2: Lēnām tupies lejā (2 seconds)
            {
                float stepDuration = 3f;
                float stepTime = 0f;
                if (sets3 == 1)
                {
                    audioManager.Instance.PlayExercise7Step1();
                    instructionText.text = "Lēnām tupies lejā!";
                }
                if (repss == 0 || repss == 2)
                {
                    SetGif(exercise3DemoGifs[16]);
                }
                else { SetGif(exercise3DemoGifs[14]); }


                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart7();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }

            if (globalTimer <= 0)
                break;

            // Step 3: celies augšā! (2 seconds)
            {
                float stepDuration = 2f;
                float stepTime = 0f;
                if (sets3 == 1)
                {
                    audioManager.Instance.PlayExercise7Step2();
                    instructionText.text = "Celies augšā!";
                }
                    if (repss == 0 || repss == 2)
                {
                    SetGif(exercise3DemoGifs[17]);
                }
                else { SetGif(exercise3DemoGifs[15]); }


                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart7();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }



        }

        // Final restart check (just in case)
        if (restartExerciseRequested)
        {
            yield return HandleRestart7();
            yield break;
        }
        
       
        if (repss == 1 || repss == 3)
        {
            characterAnimator.ResetTrigger("Ex6");
            characterAnimator.SetTrigger("Idle");
        }
        else if (repss == 0 || repss == 2)
        {
            characterAnimator.ResetTrigger("Ex6L");
            characterAnimator.SetTrigger("Idle");
        }
        repss++;
        sets3++;
        yield break;
    }

    IEnumerator HandleRestart7()
    {
        // Immediately stop any audio.
        characterAnimator.speed = 1; // Unfreeze animation.
        audioManager.Instance.StopAllAudio();
        characterAnimator.ResetTrigger("Ex6");
        characterAnimator.ResetTrigger("Ex6L");
        characterAnimator.SetTrigger("Idle");

        // Inform the user about the restart.
        audioManager.Instance.PlayExerciseZoneVoice(7);
        instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
        restartExerciseRequested = false;

        // Initiate a 5-second restart countdown.
        yield return StartCountdown(5);

        // Restart the execution phase.
        yield return RunExerciseExecutionForExercise7();
    }


    IEnumerator ExecuteRepCycleForExercise7()
    {


        // Step 1: 1 seconds.
        instructionText.text = "Uz abām kājām!";
        SetGif(exercise3DemoGifs[12]);
        yield return new WaitForSeconds(1f);

        // Step 2: 2 second.
        instructionText.text = "Lec pa labi!";
        SetGif(exercise3DemoGifs[13]);
        yield return new WaitForSeconds(2f);

        // Step 3: 2 second.
        instructionText.text = "Lec pa kreisi!";
        SetGif(exercise3DemoGifs[14]);
        yield return new WaitForSeconds(2f);


    }

    IEnumerator ShowDemoGifSequenceForExercise7()
    {
        // Ensure that exactly four demo GIFs have been assigned.
        if (exercise3DemoGifs != null)
        {

            SetGif(exercise3DemoGifs[14]);
            yield return StartCountdown(3);

            SetGif(exercise3DemoGifs[15]);
            yield return StartCountdown(2);



        }
        HideGifDisplay();
    }

    // ex id 7 end

    // ex id 8 start

    IEnumerator RunDemoStepForExercise8()
    {
        // Play demo audio and show demo instructions.
        audioManager.Instance.PlayDemo7();
        instructionText.color = Color.cyan;
        instructionText.text = "Vingrojums 7: kastes lecieni";
        // Show the demo GIF sequence.
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.speed = 1;
        characterAnimator.SetFloat("SpeedMultiplier", 1.5f); // CHANGE THIS
        characterAnimator.SetTrigger("Ex7");
        yield return StartCountdown(10);
        characterAnimator.ResetTrigger("Ex7");
        characterAnimator.Play("Idle", 0, 0f);
    }



    int sets4 = 1;
    IEnumerator RunExerciseExecutionForExercise8()
    {
        instructionText.text = "";
        instructionText.color = Color.white;
        FootOverlayManagerTwoFeet.Instance.SetActiveFoot("none");
        float globalTimer = currentExercise.TimingCop; // Total execution time (e.g., 30 seconds)
        if (leftFootInstructionText != null)
            leftFootInstructionText.gameObject.SetActive(false);
        if (rightFootInstructionText != null)
            rightFootInstructionText.gameObject.SetActive(false);
        reptestText.gameObject.SetActive(true);
        reptestText.text = $"Set <color=#00FFFF>{sets4}</color> / 2";


        // --- Steps 1-8 Repeat 10 times ---
        for (int i = 0; i < 10; i++)
        {
            characterAnimator.ResetTrigger("Idle");
            characterAnimator.SetTrigger("Ex7");
            // reptestText.text = $"Rep {i + 1}/3\nSet {sets4}/2";
            reptestText.text = $"Rep <color=#00FFFF>{i + 1}</color> / 3 \nSet <color=#00FFFF>{sets4}</color> / 2";

            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step1();
                    instructionText.text = "Uz priekšu";
                }
                SetGif(exercise3DemoGifs[18]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step2();
                    instructionText.text = "Uz vidu";

                }
                    SetGif(exercise3DemoGifs[19]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step3();
                    instructionText.text = "Aizmugure";
                }
                    SetGif(exercise3DemoGifs[19]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step2();
                    instructionText.text = "Uz vidu";
                }
                    SetGif(exercise3DemoGifs[19]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step4();
                    instructionText.text = "Pa labi";
                }
                SetGif(exercise3DemoGifs[20]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step2();
                    instructionText.text = "Uz vidu";
                }
                    SetGif(exercise3DemoGifs[19]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }

            if (globalTimer <= 0)
                break;

            // Step 3: Izklupiens ar kreiso kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step5();
                    instructionText.text = "Pa kreisi";
                }
                SetGif(exercise3DemoGifs[21]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step2();
                    instructionText.text = "Uz vidu";
                }
                    SetGif(exercise3DemoGifs[19]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step1();
                    instructionText.text = "Uz priekšu";
                }
                    SetGif(exercise3DemoGifs[18]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }
            if (globalTimer <= 0)
                break;

            // Step 2: Izklupiens ar labo kāju! (2 seconds)
            {
                float stepDuration = 1f;
                float stepTime = 0f;
                if (sets4 == 1)
                {
                    audioManager.Instance.PlayExercise8Step2();
                    instructionText.text = "Uz vidu";
                }
                    SetGif(exercise3DemoGifs[19]);

                while (stepTime < stepDuration && globalTimer > 0)
                {
                    if (restartExerciseRequested)
                    {
                        yield return HandleRestart8();
                        yield break;
                    }
                    float delta = Time.deltaTime;
                    stepTime += delta;
                    globalTimer -= delta;
                    countdownText.text = Mathf.CeilToInt(globalTimer).ToString();
                    yield return null;
                }
            }



        }

        // Final restart check (just in case)
        if (restartExerciseRequested)
        {
            yield return HandleRestart8();
            yield break;
        }
        characterAnimator.ResetTrigger("Ex7");
        sets4++;
        yield break;
    }

    IEnumerator HandleRestart8()
    {
        // Immediately stop any audio.
        characterAnimator.speed = 1; // Unfreeze animation.
        audioManager.Instance.StopAllAudio();
        characterAnimator.ResetTrigger("Ex7");
        characterAnimator.SetTrigger("Idle");

        // Inform the user about the restart.
        audioManager.Instance.PlayExerciseZoneVoice(7);
        instructionText.text = "Līdzsvars zaudēts, sāc no sākuma!";
        restartExerciseRequested = false;

        // Initiate a 5-second restart countdown.
        yield return StartCountdown(5);

        // Restart the execution phase.
        yield return RunExerciseExecutionForExercise8();
    }


    IEnumerator ExecuteRepCycleForExercise8()
    {


        // Step 1: 1 seconds.
        instructionText.text = "Uz priekšu";
        SetGif(exercise3DemoGifs[18]);
        yield return new WaitForSeconds(1f);

        // Step 2: 1 second.
        instructionText.text = "Uz vidu";
        SetGif(exercise3DemoGifs[19]);
        yield return new WaitForSeconds(1f);

        // Step 3: 1 second.
        instructionText.text = "Aizmugure";
        SetGif(exercise3DemoGifs[19]);
        yield return new WaitForSeconds(1f);
        // Step 3: 1 second.
        instructionText.text = "Uz vidu";
        SetGif(exercise3DemoGifs[19]);
        yield return new WaitForSeconds(1f);
        // Step 3: 1 second.
        instructionText.text = "Pa labi";
        SetGif(exercise3DemoGifs[20]);
        yield return new WaitForSeconds(1f);
        // Step 3: 1 second.
        instructionText.text = "Uz vidu";
        SetGif(exercise3DemoGifs[19]);
        yield return new WaitForSeconds(1f);
        // Step 3: 1 second.
        instructionText.text = "Pa kreisi";
        SetGif(exercise3DemoGifs[21]);
        yield return new WaitForSeconds(1f);

        instructionText.text = "Uz vidu";
        SetGif(exercise3DemoGifs[19]);
        yield return new WaitForSeconds(1f);
        instructionText.text = "Uz priekšu";
        SetGif(exercise3DemoGifs[18]);
        yield return new WaitForSeconds(1f);

        // Step 2: 2 second.
        instructionText.text = "Uz vidu";
        SetGif(exercise3DemoGifs[19]);
        yield return new WaitForSeconds(1f);


    }

    IEnumerator ShowDemoGifSequenceForExercise8()
    {
        // Ensure that exactly four demo GIFs have been assigned.
        if (exercise3DemoGifs != null)
        {

            // Step 1: 1 seconds.

            SetGif(exercise3DemoGifs[18]);
            yield return StartCountdown(1);

            // Step 2: 1 second.

            SetGif(exercise3DemoGifs[19]);
            yield return StartCountdown(1);

            // Step 3: 1 second.

            SetGif(exercise3DemoGifs[19]);
            yield return StartCountdown(1);
            // Step 3: 1 second.

            SetGif(exercise3DemoGifs[19]);
            yield return StartCountdown(1);
            // Step 3: 1 second.

            SetGif(exercise3DemoGifs[20]);
            yield return StartCountdown(1);
            // Step 3: 1 second.

            SetGif(exercise3DemoGifs[19]);
            yield return StartCountdown(1);
            // Step 3: 1 second.

            SetGif(exercise3DemoGifs[21]);
            yield return StartCountdown(1);


            SetGif(exercise3DemoGifs[19]);
            yield return StartCountdown(1);

            SetGif(exercise3DemoGifs[18]);
            yield return StartCountdown(1);

            // Step 2: 2 second.

            SetGif(exercise3DemoGifs[19]);
            yield return StartCountdown(1);

        }
        HideGifDisplay();
    }

    // ex id 8 end
    IEnumerator ExecuteExerciseAnimation(bool isLeftLeg, int time)
    {
        characterAnimator.SetBool("IsLeftLeg", isLeftLeg);
        yield return new WaitForEndOfFrame();
        characterAnimator.SetTrigger("StartExercise");
        yield return FreezeAtLastFrame(time);

        // check if restart was requested during the countdown
        if (restartExerciseRequested)
            yield break;

        ReturnToIdle();
    }
    IEnumerator AnimateRotation(Transform target, Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            target.rotation = Quaternion.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.rotation = to; // Ensure the final rotation is set
    }
    IEnumerator FreezeAtLastFrame(int holdSeconds, bool resumeAfter = true)
    {
        float waitTime = 0f;
        while (!IsInCorrectAnimationState() && waitTime < 2f)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }
        if (waitTime < 2f)
        {
            yield return new WaitWhile(() =>
                characterAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f
            );
        }

        characterAnimator.speed = 0; // Freeze animation

        float timer = holdSeconds;
        while (timer > 0f && !restartExerciseRequested)
        {
            countdownText.text = Mathf.CeilToInt(timer).ToString();
            timer -= Time.deltaTime;
            yield return null;
        }
        countdownText.text = "0";

        if (resumeAfter)
        {
            characterAnimator.speed = 1; // Resume animation if desired
        }
    }


    bool IsInCorrectAnimationState()
    {
        if (currentExercise == null) return false;
        string targetState = currentExercise.LegsUsed.ToLower() == "right" ? "OneStand_Right" : "OneStand_Left";
        return characterAnimator.GetCurrentAnimatorStateInfo(0).IsName(targetState);
    }

    void ReturnToIdle()
    {
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.SetTrigger("Idle");
    }

    IEnumerator StartCountdown(int seconds)
    {
        float timer = seconds;
        while (timer > 0f)
        {
            countdownText.text = Mathf.CeilToInt(timer).ToString();
            timer -= Time.deltaTime;
            yield return null;
        }
        countdownText.text = "0";
    }

    IEnumerator DemonstrateExercise()
    {
        if (currentExercise.RepetitionID == 1)
        {
            audioManager.Instance.PlayDemo();
            instructionText.color = Color.cyan;
            instructionText.text = "Vingrojums: Stāvēšana uz vienas kājas 30 sekundes";
            bool isRightLeg = currentExercise.LegsUsed.ToLower() == "right";
            characterAnimator.SetBool("IsLeftLeg", !isRightLeg);
            yield return new WaitForEndOfFrame();
            characterAnimator.SetTrigger("StartExercise");
            yield return FreezeAtLastFrame(currentExercise.Demo);
            characterAnimator.ResetTrigger("StartExercise");
            ReturnToIdle();
        }
        else if (currentExercise.RepetitionID == 3)
        {
            // Fallback if by any chance the default demo is called.
            audioManager.Instance.PlayDemo();
            instructionText.text = "Vingrojums: Pietupiens ar pirkstgalu celšanu 30 sekundes";
            yield return FreezeAtLastFrame(currentExercise.Demo);
        }
    }

    IEnumerator WaitForExerciseConfigs()
    {
        // Wait until at least one new config is received.
        while (exerciseConfigs.Count == 1)
        {
            yield return new WaitForSeconds(0.5f);
        }
        if (currentExercise.RepetitionID == 7)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }

    void UpdateActiveFootDisplay()
    {
        if (currentExercise != null && FootOverlayManagerTwoFeet.Instance != null)
        {
            FootOverlayManagerTwoFeet.Instance.SetActiveFoot(currentExercise.LegsUsed);
        }
    }



    public string GetSingleZoneMessage(int zone)
    {
        
        if (sets5 < 3)
        {
            audioManager.Instance.PlayExerciseZoneVoice(zone);
            switch (zone)
            {
                case 1:
                    return "Tev lieliski izdodas!"; // Tev lieliski izdodas!
                case 2:
                    return "Nostāties pareizi"; // Nostāties pareizi
                case 3:
                    return "Uz priekšu!"; // Uz priekšu!
                case 4:
                    return "Uz aizmuguri!"; //Uz aizmuguri!
                case 5:
                    return "Pa kreisi!"; // Pa kreisi!
                case 6:
                    return "Pa labi!"; // Pa labi!
                case 7:
                    RequestExerciseRestart();
                    return "Līdzsvars zaudēts, sāc no sākuma!";
                default:
                    return "Nezināma zona.";
            }
        }
        else
        {
            switch (zone)
            {
                case 1:
                    return ""; // Tev lieliski izdodas!
                case 2:
                    return ""; // Nostāties pareizi
                case 3:
                    return ""; // Uz priekšu!
                case 4:
                    return ""; //Uz aizmuguri!
                case 5:
                    return ""; // Pa kreisi!
                case 6:
                    return ""; // Pa labi!
                case 7:
                    RequestExerciseRestart();
                    return "Līdzsvars zaudēts, sāc no sākuma!";
                default:
                    return "Nezināma zona.";
            }
        }
        
       
    }

    public void MarkPreparationSuccessful()
    {
        preparationSuccessful = true;
    }

    public void RequestExerciseRestart()
    {
        Debug.Log("RequestExerciseRestart called. Setting restartExerciseRequested flag to true.");
        restartExerciseRequested = true;
    }


    // ------------------- Dummy/Default Exercise Creation -------------------
    void CreateDummyExercises()
    {
        // Create two dummy exercise configurations: one for right leg and one for left leg.
        HMDDataReceiver.ExerciseConfigMessage configRight = new HMDDataReceiver.ExerciseConfigMessage
        {
            MessageType = "ExerciseConfig",
            RepetitionID = 1,
            Name = "Single-Leg Stance - Right Leg",
            LegsUsed = "right",
            Intro = 1,
            Demo = 3,
            PreparationCop = 3,
            TimingCop = 3,
            Release = 3,
            Switch = 3,
            Sets = 2,
            ZoneSequence = new HMDDataReceiver.ZoneSequenceItem[]
            {
                new HMDDataReceiver.ZoneSequenceItem
                {
                    Duration = 30,
                    GreenZoneX = new Vector2(-1f, 1f),
                    GreenZoneY = new Vector2(-1f, 1f),
                    RedZoneX = new Vector2(-2f, -1f),
                    RedZoneY = new Vector2(-6f, -1.1f)
                }
            }
        };

        HMDDataReceiver.ExerciseConfigMessage configLeft = new HMDDataReceiver.ExerciseConfigMessage
        {
            MessageType = "ExerciseConfig",
            RepetitionID = 2,
            Name = "Single-Leg Stance - Left Leg",
            LegsUsed = "left",
            Intro = 1,
            Demo = 3,
            PreparationCop = 3,
            TimingCop = 3,
            Release = 3,
            Switch = 3,
            Sets = 2,
            ZoneSequence = new HMDDataReceiver.ZoneSequenceItem[]
            {
                new HMDDataReceiver.ZoneSequenceItem
                {
                    Duration = 30,
                    GreenZoneX = new Vector2(-1f, 1f),
                    GreenZoneY = new Vector2(-1f, 1f),
                    RedZoneX = new Vector2(1f, 2f),
                    RedZoneY = new Vector2(-6f, -1.1f)
                }
            }
        };

        // Note: No dummy config is created for RepetitionID 3.
        UpdateExerciseConfiguration(configRight);
        UpdateExerciseConfiguration(configLeft);
    }

    // ------------------- Methods Called from HMDDataReceiver -------------------

    /// <summary>
    /// Called when an exercise configuration message is received from the client.
    /// </summary>
    /// <param name="config">The exercise configuration message.</param>
    public void UpdateExerciseConfiguration(HMDDataReceiver.ExerciseConfigMessage config)
    {
        Debug.Log($"Saņemta exercise config: RepetitionID {config.RepetitionID}, LegsUsed: {config.LegsUsed}");
        // Convert the received message into our local ExerciseConfig structure.
        ExerciseConfig newConfig = new ExerciseConfig
        {
            RepetitionID = config.RepetitionID,
            Name = config.Name,
            LegsUsed = config.LegsUsed,
            Intro = config.Intro,
            Demo = config.Demo,
            PreparationCop = config.PreparationCop,
            TimingCop = config.TimingCop,
            Release = config.Release,
            Switch = config.Switch,
            Sets = config.Sets,
            ZoneSequence = config.ZoneSequence
        };
        // Add to the list if not already present.
        if (!exerciseConfigs.Exists(e => e.RepetitionID == newConfig.RepetitionID))
        {
            exerciseConfigs.Add(newConfig);
        }
    }

    /// <summary>
    /// Called when a feedback message is received from the client.
    /// </summary>
    /// <param name="zone">The zone code.</param>
    /// <param name="foot">Which foot ("Left", "Right", or "Both")</param>
    public void UpdateFootStatusForFoot(int zone, string foot)
    {
        if (FootOverlayManagerTwoFeet.Instance != null)
        {
            FootOverlayManagerTwoFeet.Instance.UpdateOverlayForZone(zone, foot);
        }

        // If exercise 3 is active (which uses both feet), update the respective text windows.
        if (currentExercise != null && currentExercise.RepetitionID > 2)
        {
            if (foot.ToLower() == "left")
            {
                if (leftFootInstructionText != null)
                    leftFootInstructionText.text = GetSingleZoneMessage(zone);
            }
            else if (foot.ToLower() == "right")
            {
                if (rightFootInstructionText != null)
                    rightFootInstructionText.text = GetSingleZoneMessage(zone);
            }
            else if (foot.ToLower() == "both")
            {
                // Update both if feedback is for both feet.
                if (leftFootInstructionText != null)
                    leftFootInstructionText.text = GetSingleZoneMessage(zone);
                if (rightFootInstructionText != null)
                    rightFootInstructionText.text = GetSingleZoneMessage(zone);
            }
        }
        else
        {
            // For exercises 1 and 2, use the single instructionText.
            instructionText.text = GetSingleZoneMessage(zone);
        }
    }
    //public void UpdateFootStatusForCombinationZones(int[] zones, string foot)
    //{
    //    // Update the overlay for each zone.
    //    if (FootOverlayManagerTwoFeet.Instance != null)
    //    {
    //        foreach (int zone in zones)
    //        {
    //            FootOverlayManagerTwoFeet.Instance.UpdateOverlayForZone(zone, foot);
    //        }
    //    }

    //    // Example: set instruction text based on combination zones.
    //    if (zones[0] == 3 && zones[1] == 5)
    //    {
    //        instructionText.text = "Pārvirzi svaru uz priekšu un pa kreisi!";
    //    }
    //    if (zones[0] == 3 && zones[1] == 6)
    //    {
    //        instructionText.text = "Pārvirzi svaru uz priekšu un pa labi!";
    //    }
    //    if (zones[0] == 4 && zones[1] == 5)
    //    {
    //        instructionText.text = "Pārvirzi svaru uz aizmuguri un pa kreisi!";
    //    }
    //    if (zones[0] == 4 && zones[1] == 6)
    //    {
    //        instructionText.text = "Pārvirzi svaru uz aizmuguri un pa labi!";
    //    }

    //    else
    //    {
    //        // Fallback: leave blank or set a default message.
    //        instructionText.text = "";
    //    }

    //    // Placeholder for playing combination zone audio.
    //    if (zones.Length == 2)
    //    {
    //        audioManager.Instance.PlayCombinationZoneVoice(zones[0], zones[1]);
    //    }
    //}


}

