// ============================================================================
// TUTORIAL CONTROLLER - Tutorial flow'u yönetir
// Turn 1: He-Man highlight, Turn 2: Toy Soldier highlight, Turn 3: Free choice
// Mini Battle → Full Battle → Tutorial Chest → Complete
// ============================================================================

using System.Collections;
using UnityEngine;
using Zenject;

public class TutorialController : MonoBehaviour
{
    [Inject] GameManager gameManager;
    [Inject] UIManager uiManager;
    [Inject] DraftCardManager draftManager;
    [Inject] BattleManager battleManager;
    [Inject] ChestSystem chestSystem;
    [Inject] AIController aiController;
    [Inject] UnlockSystem unlockSystem;
    
    [Header("Tutorial State")]
    [SerializeField] int tutorialStep = 0;
    [SerializeField] bool isTutorialActive = false;
    
    [Header("Tutorial Units")]
    [SerializeField] ToyUnitData heManUnit;
    [SerializeField] ToyUnitData toySoldierUnit;
    
    // ===== START TUTORIAL =====
    
    public void StartTutorial()
    {
        isTutorialActive = true;
        tutorialStep = 0;
        
        // Set AI to tutorial mode
        aiController.SetDifficulty(BotDifficulty.Tutorial);
        
        uiManager.ShowTutorialPanel();
        
        StartCoroutine(TutorialSequence());
    }
    
    // ===== TUTORIAL SEQUENCE =====
    
    private IEnumerator TutorialSequence()
    {
        yield return new WaitForSeconds(2f);
        
        // Step 1: Draft Turn 1 - He-Man highlight
        yield return StartCoroutine(TutorialDraft1());
        
        yield return new WaitForSeconds(1f);
        
        // Step 2: Draft Turn 2 - Toy Soldier highlight
        yield return StartCoroutine(TutorialDraft2());
        
        yield return new WaitForSeconds(1f);
        
        // Step 3: Draft Turn 3 - Free choice
        yield return StartCoroutine(TutorialDraft3());
        
        yield return new WaitForSeconds(1f);
        
        // Step 4: Mini Battle (Scripted win)
        yield return StartCoroutine(TutorialMiniBattle());
        
        yield return new WaitForSeconds(2f);
        
        // Step 5: Full Battle
        yield return StartCoroutine(TutorialFullBattle());
        
        yield return new WaitForSeconds(2f);
        
        // Step 6: Tutorial Chest (Guaranteed rare)
        ShowTutorialChest();
        
        yield return new WaitForSeconds(3f);
        
        // Step 7: Unlock Assassin
        UnlockAssassin();
        
        yield return new WaitForSeconds(1f);
        
        // Complete tutorial
        CompleteTutorial();
    }
    
    // ===== TUTORIAL DRAFT 1 =====
    
    private IEnumerator TutorialDraft1()
    {
        Debug.Log("Tutorial: Draft Turn 1 - Select He-Man");
        
        // Show highlight on He-Man card
        // In actual implementation, you'd highlight the He-Man card in UI
        
        // Wait for player to select He-Man
        bool selected = false;
        while (!selected)
        {
            // Check if He-Man was selected
            yield return null;
            
            // Temporary: Auto-select after 3 seconds for testing
            if (Time.time > 3f)
            {
                selected = true;
            }
        }
        
        tutorialStep++;
    }
    
    // ===== TUTORIAL DRAFT 2 =====
    
    private IEnumerator TutorialDraft2()
    {
        Debug.Log("Tutorial: Draft Turn 2 - Select Toy Soldier");
        
        // Show highlight on Toy Soldier card
        
        // Wait for selection
        bool selected = false;
        while (!selected)
        {
            yield return null;
            
            // Temporary: Auto-select
            if (Time.time > 6f)
            {
                selected = true;
            }
        }
        
        tutorialStep++;
    }
    
    // ===== TUTORIAL DRAFT 3 =====
    
    private IEnumerator TutorialDraft3()
    {
        Debug.Log("Tutorial: Draft Turn 3 - Free choice");
        
        // Normal draft, no highlight
        
        // Wait for selection
        bool selected = false;
        while (!selected)
        {
            yield return null;
            
            // Temporary: Auto-select
            if (Time.time > 9f)
            {
                selected = true;
            }
        }
        
        tutorialStep++;
    }
    
    // ===== MINI BATTLE (Scripted) =====
    
    private IEnumerator TutorialMiniBattle()
    {
        Debug.Log("Tutorial: Mini Battle - Scripted Win");
        
        // This is a fake battle - player always wins
        // Skip combat loop
        
        yield return new WaitForSeconds(2f);
        
        // Show victory
        Debug.Log("Tutorial: You won the mini battle!");
        
        tutorialStep++;
    }
    
    // ===== FULL BATTLE =====
    
    private IEnumerator TutorialFullBattle()
    {
        Debug.Log("Tutorial: Full Battle vs Tutorial Bot");
        
        // Start real battle with tutorial bot (always lose AI)
        battleManager.StartBattle();
        
        // Wait for battle to complete
        bool battleComplete = false;
        while (!battleComplete)
        {
            // Check battle status
            yield return null;
            
            // Temporary: Auto-complete
            if (Time.time > 15f)
            {
                battleComplete = true;
            }
        }
        
        tutorialStep++;
    }
    
    // ===== TUTORIAL CHEST =====
    
    private void ShowTutorialChest()
    {
        Debug.Log("Tutorial: Opening tutorial chest (guaranteed rare)");
        chestSystem.ShowChest(tutorial: true);
    }
    
    // ===== UNLOCK ASSASSIN =====
    
    private void UnlockAssassin()
    {
        Debug.Log("Tutorial: Assassin unit unlocked!");
        
        // Unlock TMNT Assassin
        PlayerPrefs.SetInt("Unlock_tmnt", 1);
        PlayerPrefs.Save();
    }
    
    // ===== COMPLETE TUTORIAL =====
    
    private void CompleteTutorial()
    {
        isTutorialActive = false;
        
        // Mark tutorial as complete
        PlayerPrefs.SetInt("TutorialComplete", 1);
        PlayerPrefs.Save();
        
        // Reset AI difficulty
        aiController.SetDifficulty(BotDifficulty.Normal);
        
        // Notify
        EventManager.OnTutorialComplete();
        
        Debug.Log("Tutorial Complete!");
        
        // Go to main menu
        gameManager.isTutorial = false;
        gameManager.ChangeState(GameState.MainMenu);
    }
    
    // ===== HELPERS =====
    
    public bool IsTutorialActive()
    {
        return isTutorialActive;
    }
    
    public int GetTutorialStep()
    {
        return tutorialStep;
    }
}