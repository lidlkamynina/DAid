using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // For the gif display
using System;

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
    private ExerciseConfig currentExercise;

    // Control flags.
    private bool preparationSuccessful = false;
    private bool restartExerciseRequested = false;
    int reps = 0;
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
        FootOverlayManagerTwoFeet.Instance.setDefaultGreen();
        gifDisplay.gameObject.SetActive(false);
        yield return WaitForClientConnection();
        yield return RunIntroStep();
        yield return RunDemoStep();
        yield return RunPreparationPhase();
        yield return RunExerciseExecution();

        instructionText.text = "Test - next";

        // Now check if a new exercise (ID 3) has arrived.
        currentExercise = exerciseConfigs[2];
        instructionText.text = currentExercise.Name;

        gifDisplay.gameObject.SetActive(true);
        yield return RunSequenceForExercise3();
 
    }


    // New routine for Exercise ID 3.
    IEnumerator RunSequenceForExercise3()
    {

        yield return RunDemoStepForExercise3();
        yield return RunPreparationPhaseForExercise3();

        // Loop for each set as defined in the exercise config.
        for (int set = 0; set < currentExercise.Sets; set++)
        {
            yield return RunExerciseExecutionForExercise3();

            if (set < currentExercise.Sets - 1)
            {
                // Release phase (similar to other exercises)
                audioManager.Instance.PlayReleaseLeg();
                instructionText.text = "Nostājies uz abām kājām";
                yield return StartCountdown(currentExercise.Release);
                // Wait for an updated exercise config if needed.
                yield return WaitForExerciseConfigs();
            }
        }
        instructionText.text = "Pārejam uz nākamo vingrinājumu";
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

    IEnumerator RunPreparationPhase()
    {
        preparationSuccessful = false;
        while (!preparationSuccessful)
        {
            // Determine the correct exercise configuration based on reps
            if (reps == 0 || reps == 2)
            {
                currentExercise = exerciseConfigs.Find(e => e.RepetitionID == 1); // Right leg config
            }
            else if (reps == 1 || reps == 3)
            {
                currentExercise = exerciseConfigs.Find(e => e.RepetitionID == 2); // Left leg config
            }

            if (currentExercise == null)
            {
                Debug.LogError("Current exercise config not found!");
                yield break;
            }

            if (reps == 0 || reps == 2)
            {
                audioManager.Instance.PlayPreparation(right);
                instructionText.text = "Nostājies uz LABĀS kājas";
                UpdateActiveFootDisplay();
            }
            else if (reps == 1 || reps == 3)
            {
                audioManager.Instance.PlayPreparation(left);
                instructionText.text = "Nostājies uz KREISĀS kājas";
                UpdateActiveFootDisplay();
            }

            bool isRightLeg = (reps == 0 || reps == 2);
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
    }



    IEnumerator RunExerciseExecution()
    {
        int configIndex = 0;

        while (reps < 4)
        {
            currentExercise = exerciseConfigs[configIndex];
            instructionText.text = "Noturi līdzsvaru 30 sekundes!";
            audioManager.Instance.PlayNoturi();
            yield return StartCountdown(3);  // 3-second delay before starting the timer

            // Freeze the animation during the execution timer.
            characterAnimator.speed = 0;

            // Begin the 30-second timer loop with restart handling.
            bool timerCompleted = false;
            while (!timerCompleted)
            {
                float timer = currentExercise.TimingCop; // full 30-second duration
                while (timer > 0)
                {
                    countdownText.text = Mathf.CeilToInt(timer).ToString();
                    timer -= Time.deltaTime;
                    // Check if a restart has been triggered.
                    if (restartExerciseRequested)
                    {
                        break;
                    }
                    yield return null;
                }
                if (restartExerciseRequested)
                {
                    // A restart occurred during the timer.
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
            characterAnimator.speed = 1;
            characterAnimator.ResetTrigger("StartExercise");
            characterAnimator.SetTrigger("Idle");
            audioManager.Instance.PlayReleaseLeg();
            instructionText.text = "Nostājies uz ABĀM kājām";
            FootOverlayManagerTwoFeet.Instance.SetActiveFoot("both");

            yield return StartCountdown(currentExercise.Release);
            yield return WaitForExerciseConfigs();
            reps++;
            configIndex = (configIndex + 1) % 2;
            if (reps < 4)
            {
                yield return RunPreparationPhase();
            }
        }
    }


    IEnumerator StartCountdown(int seconds, Action onComplete = null)
    {
        float timer = 0;
        while (timer < seconds)
        {
            timer += Time.deltaTime;
            // Update UI here if needed
            yield return null;
        }
        onComplete?.Invoke();
    }

    IEnumerator RestartCheck(Action onRestart)
    {
        while (true)
        {
            if (restartExerciseRequested)
            {
                onRestart?.Invoke();
                yield break;
            }
            yield return null;
        }
    }




    // ------------------- New Methods for Exercise ID 3 -------------------


    IEnumerator RunDemoStepForExercise3()
    {
        // Play demo audio and show demo instructions.
        audioManager.Instance.PlayDemo();
        instructionText.text = "Vingrojums 2: Pietupiens ar pirkstgalu celšanu 30 sekundes";
        // Show the demo GIF sequence.
        yield return ShowDemoGifSequenceForExercise3();
    }

    IEnumerator RunPreparationPhaseForExercise3()
    {
        // For exercise 3, preparation is just a 6-second countdown with instruction.
        instructionText.text = "Nostājies uz ABĀM kājām";
        UpdateActiveFootDisplay();
        yield return StartCountdown(currentExercise.PreparationCop);
    }

    IEnumerator RunExerciseExecutionForExercise3()
    {
        // In exercise 3, the execution phase lasts for TimingCop seconds (e.g., 30 seconds)
        // Within that period, a rep cycle of 6 seconds (1+3+1+1) is repeated.
        float setDuration = currentExercise.TimingCop;
        float elapsed = 0f;
        while (elapsed < setDuration)
        {
            // If the remaining time is less than a full cycle, you could adjust the timing accordingly.
            yield return ExecuteRepCycleForExercise3();
            elapsed += 6f;
        }
    }

    IEnumerator ExecuteRepCycleForExercise3()
    {
        

        // Step 1: 3 seconds.
        instructionText.text = "Veic pietupienu uz leju līdz 90 grādiem!";
        ShowExecutionGif(1);
        yield return new WaitForSeconds(3f);

        // Step 2: 1 second.
        instructionText.text = "Nostājies uz abām kājām";
        ShowExecutionGif(0);
        yield return new WaitForSeconds(1f);

        // Step 3: 1 second.
        instructionText.text = "Celies augšā!";
        ShowExecutionGif(2);
        yield return new WaitForSeconds(1f);

        // Step 4: 1 second.
        instructionText.text = "Uz pirkstgaliem";
        ShowExecutionGif(3);
        yield return new WaitForSeconds(1f);
    }

    IEnumerator ShowDemoGifSequenceForExercise3()
    {
        // Ensure that exactly four demo GIFs have been assigned.
        if (exercise3DemoGifs != null && exercise3DemoGifs.Length >= 4)
        {
            SetGif(exercise3DemoGifs[0]);
            yield return new WaitForSeconds(1f);

            SetGif(exercise3DemoGifs[1]);
            yield return new WaitForSeconds(3f);

            SetGif(exercise3DemoGifs[2]);
            yield return new WaitForSeconds(1f);

            SetGif(exercise3DemoGifs[3]);
            yield return new WaitForSeconds(1f);
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
    }

    void UpdateActiveFootDisplay()
    {
        if (currentExercise != null && FootOverlayManagerTwoFeet.Instance != null)
        {
            FootOverlayManagerTwoFeet.Instance.SetActiveFoot(currentExercise.LegsUsed);
        }
    }

    void SetFootGradientForFoot(int zone, Transform footTransform)
    {
        if (footTransform == null)
            return;
        Renderer footRenderer = footTransform.GetComponent<Renderer>();
        if (footRenderer == null || footRenderer.material == null)
            return;

        Color defaultColor = Color.green;
        Color leftColor = defaultColor;
        Color rightColor = defaultColor;
        Color topColor = defaultColor;
        Color bottomColor = defaultColor;

        switch (zone)
        {
            case 1:
                // Correct balance; leave as default.
                break;
            case 2:
                leftColor = rightColor = topColor = bottomColor = Color.red;
                break;
            case 3:
                bottomColor = Color.red;
                break;
            case 4:
                topColor = Color.red;
                break;
            case 5:
                rightColor = Color.red;
                break;
            case 6:
                leftColor = Color.red;
                break;
            case 7:
                RequestExerciseRestart();
                leftColor = rightColor = topColor = bottomColor = Color.red;
                break;
            default:
                break;
        }

        footRenderer.material.SetColor("_LeftColor", leftColor);
        footRenderer.material.SetColor("_RightColor", rightColor);
        footRenderer.material.SetColor("_TopColor", topColor);
        footRenderer.material.SetColor("_BottomColor", bottomColor);
    }

    public string GetSingleZoneMessage(int zone)
    {
        audioManager.Instance.PlayExerciseZoneVoice(zone);
        switch (zone)
        {
            case 1:
                return "Tev lieliski izdodas!";
            case 2:
                return "Nostāties pareizi";
            case 3:
                return "Pārvirzi svaru uz priekšu!";
            case 4:
                return "Pārvirzi svaru uz aizmuguri!";
            case 5:
                return "Pavirzi svaru pa kreisi!";
            case 6:
                return "Pavirzi svaru pa labi!";
            case 7:
                RequestExerciseRestart();
                return "Līdzsvars zaudēts, sāc no sākuma!";
            default:
                return "Nezināma zona.";
        }
    }

    public void MarkPreparationSuccessful()
    {
        preparationSuccessful = true;
    }

    public void RequestExerciseRestart()
    {
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

        // Update instruction text as before...
        if (foot.ToLower() == "left")
        {
            instructionText.text = GetSingleZoneMessage(zone);
        }
        else if (foot.ToLower() == "right")
        {
            instructionText.text = GetSingleZoneMessage(zone);
        }
        else if (foot.ToLower() == "both")
        {
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

