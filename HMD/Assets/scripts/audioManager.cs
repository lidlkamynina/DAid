using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExerciseAudioClips
{
    [Tooltip("ID of the exercise to which these audio clips belong.")]
    public int exerciseID;
    [Tooltip("Dynamic demo clip for this exercise.")]
    public AudioClip Demo_1;
    [Tooltip("Dynamic instruction clip for this exercise.")]
    public AudioClip instructionClip;
    [Tooltip("Dynamic switch leg clip when switching to left leg for this exercise.")]
    public AudioClip LeftLeg;
    [Tooltip("Dynamic switch leg clip when switching to right leg for this exercise.")]
    public AudioClip RightLeg;

    
}

public class audioManager : MonoBehaviour
{
    public static audioManager Instance;

    [Header("Audio Source")]
    [Tooltip("The AudioSource component used to play all audio clips. Attach a component here.")]
    public AudioSource audioSource; // This component plays the sound.

    [Header("Primary Audio Clips (Default)")]
    [Tooltip("Default audio clip for the intro step.")]
    public AudioClip Intro;
    [Tooltip("Default audio clip for the demo step.")]
    public AudioClip Demo_1;
    [Tooltip("Default audio clip for the preparation phase.")]
    public AudioClip Prep;
    public AudioClip Hold;
    public AudioClip Noturi;
    [Tooltip("Default audio clip for the release leg phase.")]
    public AudioClip Release;
    [Tooltip("Default generic instruction audio clip.")]
    public AudioClip instructionClip;

    public AudioClip Demo2;
    // --- New fields for Exercise 3's steps ---
    [Tooltip("Voice clip for exercise 3 - Step 1: Veic pietupienu uz leju lîdz 90 grâdiem.")]
    public AudioClip Exercise3Step1;
    [Tooltip("Voice clip for exercise 3 - Step 2: Celies augðâ!")]
    public AudioClip Exercise3Step2;
    [Tooltip("Voice clip for exercise 3 - Step 3: Uz pirkstgaliem.")]
    public AudioClip Exercise3Step3;
    [Tooltip("Voice clip for exercise 3 - Step 4: Nostâjies uz abâm kâjâm.")]
    public AudioClip Exercise3Step4;

    public AudioClip Demo3;
    [Tooltip("Voice clip for exercise 3 - Step 1: Veic pietupienu, ceïi lîdz 90 grâdiem!")]
    public AudioClip Exercise4Step1;
    [Tooltip("Voice clip for exercise 3 - Step 2: Noturi pozîciju!")]
    public AudioClip Exercise4Step2;
    [Tooltip("Voice clip for exercise 3 - Step 3: Lec!")]
    public AudioClip Exercise4Step3;
    //[Tooltip("Voice clip for exercise 3 - Step 4: Uz pirkstgaliem!")]
    //public AudioClip Exercise4Step4; // Not used in Exercise 4 because audio exists in ex 3 for it
    public AudioClip Demo4;
    [Tooltip("Voice clip for exercise 3 - Step 1: Uz abâm kâjâm!")]
    public AudioClip Exercise5Step1;
    [Tooltip("Voice clip for exercise 3 - Step 2: Izklupiens ar labo kâju!")]
    public AudioClip Exercise5Step2;
    [Tooltip("Voice clip for exercise 3 - Step 3: Izklupiens ar kreiso kâju!")]
    public AudioClip Exercise5Step3;
    [Tooltip("Voice clip for exercise 3 - Step 4: Skriet atpakaï uz sâkumu!")]
    public AudioClip Exercise5Step4;

    public AudioClip Demo5;

    [Tooltip("Voice clip for exercise 3 - Step 2: Izklupiens ar labo kâju!")]
    public AudioClip Exercise6Step2;
    [Tooltip("Voice clip for exercise 3 - Step 3: Izklupiens ar kreiso kâju!")]
    public AudioClip Exercise6Step3;

    public AudioClip Demo6;

    [Tooltip("Voice clip for exercise 6 - Step 1: Lçnâm tupies lejâ!")]
    public AudioClip Exercise7Step1;

    public AudioClip Demo7;

    [Tooltip("Voice clip for exercise 8 - Uz priekðu")]
    public AudioClip Exercise8Step1;
    [Tooltip("Voice clip for exercise 8 - Uz vidu")]
    public AudioClip Exercise8Step2;
    [Tooltip("Voice clip for exercise 8 - Aizmugure")]
    public AudioClip Exercise8Step3;
    [Tooltip("Voice clip for exercise 8 - Pa labi")]
    public AudioClip Exercise8Step4;
    [Tooltip("Voice clip for exercise 8 - Pa kreisi")]
    public AudioClip Exercise8Step5;

    [Header("Switch Leg Audio Clips (Default)")]
    [Tooltip("Default audio clip for switching to left leg.")]
    public AudioClip LeftLeg;
    [Tooltip("Default audio clip for switching to right leg.")]
    public AudioClip RightLeg;

    [Header("Exercise Zone Voice Clips")]
    [Tooltip("List of voice clips for in-exercise zone feedback (zone 1 = index 0, zone 2 = index 1, etc.). Set the size and assign each clip accordingly.")]
    public List<AudioClip> exerciseZoneClips = new List<AudioClip>();
    public AudioClip beepClip;
    [Header("Dynamic Exercise Audio Clips")]
    [Tooltip("List of audio clips specific to each exercise (by ExerciseID).")]
    public List<ExerciseAudioClips> exerciseAudioClips = new List<ExerciseAudioClips>();

    void Awake()
    {
        // Singleton setup: ensures one AudioManager instance persists.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: persist between scenes.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Public Playback Methods

    /// <summary>
    /// Plays the intro audio clip.
    /// </summary>
    public void PlayIntro()
    {
        // Plays the default intro clip.
        PlayClip(Intro, stopPrevious: false);
    }

    /// <summary>
    /// Plays the demo audio clip. If an exerciseID is provided and a dynamic clip exists, that clip is used.
    /// </summary>
    /// <param name="exerciseID">Optional exercise identifier.</param>
    public void PlayDemo(int exerciseID = -1)
    {
        if (exerciseID != -1)
        {
            ExerciseAudioClips ea = exerciseAudioClips.Find(x => x.exerciseID == exerciseID);
            if (ea != null && ea.Demo_1 != null)
            {
                PlayClip(ea.Demo_1, stopPrevious: false);
                return;
            }
        }
        // Fallback to default demo clip.
        PlayClip(Demo_1, stopPrevious: false);
    }
    public void PlayDemo2()
    {
        PlayClip(Demo2, stopPrevious: false);
    }
    public void PlayDemo3()
    {
        PlayClip(Demo3, stopPrevious: false);
    }
    public void PlayDemo4()
    {
        PlayClip(Demo4, stopPrevious: false);
    }
    public void PlayDemo5()
    {
        PlayClip(Demo5, stopPrevious: false);
    }

    public void PlayDemo6()
    {
        PlayClip(Demo6, stopPrevious: false);
    }
    public void PlayDemo7()
    {
        PlayClip(Demo7, stopPrevious: false);
    }

    /// <summary>
    /// Plays the preparation phase audio clip.
    /// </summary>
    public void PlayPreparation(string leg)
    {
        if (leg == "left")
        {
            PlayClip(LeftLeg, stopPrevious: false);
        }
        else
        {
            PlayClip(RightLeg, stopPrevious: false);
        }
    }

    public void PlayNoturi()
    {
        PlayClip(Noturi, stopPrevious: false);
    }

    public void PlayHold()
    {
        PlayClip(Hold, stopPrevious: false);
    }

    /// <summary>
    /// Plays the release leg phase audio clip.
    /// </summary>
    public void PlayReleaseLeg()
    {
        PlayClip(Release, stopPrevious: false);
    }

    /// <summary>
    /// Plays the switch leg audio clip based on the current leg.
    /// If an exerciseID is provided and a dynamic clip exists, that clip is used.
    /// </summary>
    /// <param name="isLeftLeg">True if switching to left leg; otherwise false.</param>
    /// <param name="exerciseID">Optional exercise identifier.</param>
    public void PlaySwitchLeg(bool isLeftLeg, int exerciseID = -1)
    {
        if (exerciseID != -1)
        {
            ExerciseAudioClips ea = exerciseAudioClips.Find(x => x.exerciseID == exerciseID);
            if (ea != null)
            {
                if (isLeftLeg && ea.LeftLeg != null)
                {
                    PlayClip(ea.LeftLeg, stopPrevious: false);
                    return;
                }
                else if (!isLeftLeg && ea.RightLeg != null)
                {
                    PlayClip(ea.RightLeg, stopPrevious: false);
                    return;
                }
            }
        }
        // Fallback to default switch leg clips.
        if (isLeftLeg && LeftLeg != null)
        {
            PlayClip(LeftLeg, stopPrevious: false);
        }
        else if (!isLeftLeg && RightLeg != null)
        {
            PlayClip(RightLeg, stopPrevious: false);
        }
        else
        {
            Debug.LogWarning("AudioManager: No switch leg clip found for " + (isLeftLeg ? "left" : "right") + " leg.");
        }
    }

    /// <summary>
    /// Plays a generic instruction audio clip. If an exerciseID is provided and a dynamic clip exists, that clip is used.
    /// </summary>
    /// <param name="exerciseID">Optional exercise identifier.</param>
    public void PlayInstruction(int exerciseID = -1)
    {
        if (exerciseID != -1)
        {
            ExerciseAudioClips ea = exerciseAudioClips.Find(x => x.exerciseID == exerciseID);
            if (ea != null && ea.instructionClip != null)
            {
                PlayClip(ea.instructionClip, stopPrevious: false);
                return;
            }
        }
        // Fallback to default instruction clip.
        PlayClip(instructionClip, stopPrevious: false);
    }

    /// <summary>
    /// Plays the in-exercise voice clip for a given zone exclusively.
    /// This stops any previous zone voice so that audio doesn't overlap.
    /// Expects zone numbers starting at 1.
    /// </summary>
    /// <param name="zone">The zone number (e.g., 1, 2, 3...)</param>
    public void PlayExerciseZoneVoice(int zone)
    {
        // Check if we're in Exercise 3 (RepetitionID > 2) and zone is not 7.
        if (GameManager.Instance.currentExercise.RepetitionID > 2 && zone != 7)
        {
            // For Exercise 3, play the beep concurrently without stopping current narration.
            audioSource.PlayOneShot(beepClip);
        }
        else
        {
            int index = zone - 1; // Adjust for 0-indexed list.
            if (index >= 0 && index < exerciseZoneClips.Count)
            {
                PlayExclusiveClip(exerciseZoneClips[index]);
            }
            else
            {
                Debug.LogWarning("AudioManager: Invalid zone number: " + zone);
            }
        }
    }


    // --- New Methods for Exercise 3 Steps ---

    public void PlayExercise3Step1()
    {
        PlayClip(Exercise3Step1, stopPrevious: false);
    }
    public void PlayExercise3Step2()
    {
        PlayClip(Exercise3Step2, stopPrevious: false);
    }
    public void PlayExercise3Step3()
    {
        PlayClip(Exercise3Step3, stopPrevious: false);
    }
    public void PlayExercise3Step4()
    {
        PlayClip(Exercise3Step4, stopPrevious: false);
    }

    public void PlayExercise4Step1()
    {
        PlayClip(Exercise4Step1, stopPrevious: false);
    }
    public void PlayExercise4Step2()
    {
        PlayClip(Exercise4Step2, stopPrevious: false);
    }
    public void PlayExercise4Step3()
    {
        PlayClip(Exercise4Step3, stopPrevious: false);
    }
    public void PlayExercise4Step4()
    {
        PlayClip(Exercise3Step3, stopPrevious: false);
    }

    public void PlayExercise5Step1()
    {
        PlayClip(Exercise5Step1, stopPrevious: false);
    }
    public void PlayExercise5Step2()
    {
        PlayClip(Exercise5Step2, stopPrevious: false);
    }
    public void PlayExercise5Step3()
    {
        PlayClip(Exercise5Step3, stopPrevious: false);
    }
    public void PlayExercise5Step4()
    {
        PlayClip(Exercise5Step4, stopPrevious: false);
    }
    public void PlayExercise6Step1()
    {
        PlayClip(Exercise5Step1, stopPrevious: false);
    }
    public void PlayExercise6Step2()
    {
        PlayClip(Exercise6Step2, stopPrevious: false);
    }
    public void PlayExercise6Step3()
    {
        PlayClip(Exercise6Step3, stopPrevious: false);
    }
    public void PlayExercise7Step1()
    {
        PlayClip(Exercise7Step1, stopPrevious: false);
    }
    public void PlayExercise7Step2()
    {
        PlayClip(Exercise3Step2, stopPrevious: false);
    }

    public void PlayExercise8Step1()
    {
        PlayClip(Exercise8Step1, stopPrevious: false);
    }
    public void PlayExercise8Step2()
    {
        PlayClip(Exercise8Step2, stopPrevious: false);
    }
    public void PlayExercise8Step3()
    {
        PlayClip(Exercise8Step3, stopPrevious: false);
    }
    public void PlayExercise8Step4()
    {
        PlayClip(Exercise8Step4, stopPrevious: false);
    }
    public void PlayExercise8Step5()
    {
        PlayClip(Exercise8Step5, stopPrevious: false);
    }


    //public void PlayExercise3Step2()
    //{
    //    ExerciseAudioClips ea = exerciseAudioClips.Find(x => x.exerciseID == 3);
    //    if (ea != null && ea.Exercise3Step2 != null)
    //    {
    //        PlayClip(ea.Exercise3Step2, stopPrevious: false);
    //    }
    //    else
    //    {
    //        Debug.LogWarning("AudioManager: No audio clip for Exercise 3 Step 2");
    //    }
    //}

    //public void PlayExercise3Step3()
    //{
    //    ExerciseAudioClips ea = exerciseAudioClips.Find(x => x.exerciseID == 3);
    //    if (ea != null && ea.Exercise3Step3 != null)
    //    {
    //        PlayClip(ea.Exercise3Step3, stopPrevious: false);
    //    }
    //    else
    //    {
    //        Debug.LogWarning("AudioManager: No audio clip for Exercise 3 Step 3");
    //    }
    //}

    //public void PlayExercise3Step4()
    //{
    //    ExerciseAudioClips ea = exerciseAudioClips.Find(x => x.exerciseID == 3);
    //    if (ea != null && ea.Exercise3Step4 != null)
    //    {
    //        PlayClip(ea.Exercise3Step4, stopPrevious: false);
    //    }
    //    else
    //    {
    //        Debug.LogWarning("AudioManager: No audio clip for Exercise 3 Step 4");
    //    }
    //}

    #endregion

    /// <summary>
    /// Helper method to play a clip. If stopPrevious is true, stops any currently playing clip before playing.
    /// Uses PlayOneShot (which normally allows overlapping).
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="stopPrevious">If true, stops any currently playing clip before playing.</param>
    public void PlayClip(AudioClip clip, bool stopPrevious)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Tried to play a null clip.");
            return;
        }

        if (stopPrevious && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.PlayOneShot(clip);
    }

    public void StopAllAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Helper method to play a clip exclusively.
    /// Stops any currently playing audio before assigning the new clip and playing it.
    /// This method is used for zone audio so that new voice instructions cut off the previous one.
    /// </summary>
    /// <param name="clip">The clip to play exclusively.</param>
    private void PlayExclusiveClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Tried to play a null exclusive clip.");
            return;
        }
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
