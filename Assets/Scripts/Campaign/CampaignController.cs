using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

public class CampaignController : MonoBehaviour
{

    public static CampaignController Instance;
    [Header("Prefab Assignments")]
    [SerializeField] private GameObject _mapOptionPrefab;
    [Header("Object Assignments")]
    [SerializeField] private Transform _playerIconTransform;
    [SerializeField] private ParticleSystem _playerParticleSystem;
    [SerializeField] private TextMeshPro _introBannerText;
    [SerializeField] private Transform _firstMapLocationTransform;
    [Header("Audio Assignments")]
    [SerializeField] private AudioClip _footstepsSFX;

    private CampaignSave _currCampaignSave;
    public void RegisterVisitedLevel(Vector3 levelPosition) => _currCampaignSave.visitedLevels.Add(levelPosition);
    private List<CampaignOptionController> _levelOptions;

    private void FindAndStoreAllLevelOptions() => _levelOptions = new List<CampaignOptionController>(GameObject.FindObjectsOfType<CampaignOptionController>());

    // Make singleton instance of this class.
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        Instance = this;
    }

    private void Start()
    {
        // Initialize the UI.
        GlobalUIController.Instance.InitializeUI();
        // Make the game fade from black to clear.
        TransitionManager.Instance.ShowScreen(1.25f);
        // Play game music!
        SoundManager.Instance.PlayOnLoop(MusicType.MAP_MUSIC);
        // Store and initialize all levels.
        FindAndStoreAllLevelOptions();
        // Initialize any save information.
        InitializeSaveInfo();
        // Move the player to the current hero location.
        CampaignCameraController.Instance.MoveCameraToPosition(_playerIconTransform.position);
        // Save the game.
        GameManager.SaveGame();
        // Initialize information for current level.
        // We do this AFTER the save so that dialogue will replay if the player leaves.
        InitializeCurrentLevelInfo();
    }

    ///<summary>
    /// If we already have save data, load the information into this script.
    /// Or else, create that information and save it as new.
    ///</summary>
    private void InitializeSaveInfo()
    {
        if (GameManager.GetCampaignSave() == null)
        {
            // If we didn't find a saved campaign, create a new campaign.
            _currCampaignSave = new CampaignSave();
            _currCampaignSave.currScene = GameManager.GetGameScene();
            _playerIconTransform.position = _firstMapLocationTransform.position;
            _currCampaignSave.heroMapPosition = _playerIconTransform.position;
            GameManager.SetCampaignSave(_currCampaignSave);
            // Set all levels to be non-visited.
            foreach (CampaignOptionController coc in _levelOptions)
            {
                coc.Initialize(false);
            }
        }
        else
        {
            // If we found a saved campaign, load it.
            _currCampaignSave = GameManager.GetCampaignSave();
            _playerIconTransform.transform.position = _currCampaignSave.heroMapPosition;
            // Set visited states for all level options.
            List<Vector3> visitedLevels = _currCampaignSave.visitedLevels;
            foreach (CampaignOptionController coc in _levelOptions)
            {
                coc.Initialize(visitedLevels.Contains(coc.transform.position));
            }
        }
    }

    ///<summary>
    /// Find the current level. If it doesn't exist, set the choosable options to
    /// be the start options. Or else, set the choosable options.
    ///</summary>
    private void InitializeCurrentLevelInfo()
    {
        // Try to find the current level controller that the player is at.
        CampaignOptionController currentLevel = _levelOptions.Find((coc) => coc.transform.position == _playerIconTransform.position);
        // If we didn't find it, error!
        if (currentLevel == null) Debug.LogError("Could not find the current level!", gameObject);
        // Invoke the current level's on load event.
        currentLevel.SetAsCurrentLevel();
        // If we found it, then set the available connected levels.
        SetAvailableLevelOptions(currentLevel.ConnectedLevels);
    }

    private void SetAvailableLevelOptions(List<CampaignOptionController> availableLevelOptions)
    {
        // Initially disable all level options.
        foreach (CampaignOptionController coc in _levelOptions)
        {
            coc.SetInteractable(false);
        }
        // Then, re-enable all the ones that are valid.
        foreach (CampaignOptionController coc in availableLevelOptions)
        {
            coc.SetInteractable(true);
        }
    }

    // Select an option given a CampaignOptionController.
    public void ChooseOption(CampaignOptionController loc, bool shouldInvokeFirstTime = false)
    {
        StartCoroutine(ChooseOptionCoroutine(loc, shouldInvokeFirstTime));
    }

    private IEnumerator ChooseOptionCoroutine(CampaignOptionController loc, bool shouldInvokeFirstTime)
    {
        // Prevent the player from selecting another option.
        foreach (CampaignOptionController option in _levelOptions)
        {
            option.SetInteractable(false, false);
        }
        // Make the character animate towards the next thing.
        CampaignCameraController.Instance.LerpCameraToPosition(loc.transform.position, 1.2f);
        StartCoroutine(MoveHeroToPositionCoroutine(loc.transform.position, 0.8f));
        yield return CampaignCameraController.Instance.LerpCameraToPositionCoroutine(loc.transform.position, 1.3f);
        // Serialize current choice.
        _currCampaignSave.heroMapPosition = _playerIconTransform.position;
        GameManager.SetCampaignSave(_currCampaignSave);
        // Render first-time load if necessary.
        if (shouldInvokeFirstTime)
        {
            loc.OnSelectFirstTime.Invoke();
        }
        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => !DialogueUIController.Instance.IsPlaying());
        // Render the appropriate actions based on the location.
        LocationChoice locationChoice = loc.LocationChoice;
        switch (locationChoice)
        {
            case LocationChoice.SHOP:
                TransitionManager.Instance.HideScreen("Shop", 0.75f);
                break;
            case LocationChoice.TREASURE:
                TreasureController.Instance.ShowChest();
                break;
            case LocationChoice.BASIC_ENCOUNTER:
            case LocationChoice.MINIBOSS_ENCOUNTER:
            case LocationChoice.BOSS_ENCOUNTER:
                Encounter newEncounter = new Encounter();
                newEncounter.enemies = loc.EnemiesToRender;
                GameManager.AddSeenEnemies(newEncounter);
                GameManager.nextBattleEnemies = newEncounter.enemies;
                TransitionManager.Instance.HideScreen("Battle", 0.75f);
                break;
            case LocationChoice.UPGRADE_MACHINE:
                TransitionManager.Instance.HideScreen("Upgrade", 0.75f);
                break;
            case LocationChoice.NONE:
                // If we're at a random path, just initialize the path from the
                // current position.
                InitializeCurrentLevelInfo();
                break;
        }
    }

    // Makes the player icon move towards a certain position.
    private IEnumerator MoveHeroToPositionCoroutine(Vector3 targetPosition, float timeToWait)
    {
        Vector3 initialPosition = _playerIconTransform.localPosition;
        float currTime = 0;
        float timeSinceLastParticle = 0;
        float particleCooldown = 0.15f;
        SoundManager.Instance.PlayOneShot(_footstepsSFX, 0.22f);
        while (currTime < timeToWait)
        {
            currTime += Time.deltaTime;
            _playerIconTransform.localPosition = Vector3.Lerp(initialPosition, targetPosition, currTime / timeToWait);
            timeSinceLastParticle += Time.deltaTime;
            if (timeSinceLastParticle > particleCooldown)
            {
                _playerParticleSystem.Emit(1);
                timeSinceLastParticle = 0;
            }
            yield return null;
        }
        _playerIconTransform.localPosition = targetPosition;
        yield return new WaitForSeconds(0.5f);
    }

}