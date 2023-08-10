using UnityEngine;
using UnityEngine.SceneManagement;

public class DiscordManager : MonoBehaviour
{
#if !UNITY_WEBGL
    public long applicationID;
    [Space]
    public string largeImage;
    public string largeText;

    private long time;

    public Discord.Discord discord;

    private float _timeSinceLastUpdate = 0;

    void Start()
    {
        // Log in with the Application ID
        discord = new Discord.Discord(applicationID, (System.UInt64)Discord.CreateFlags.NoRequireDiscord);

        time = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();

        UpdateStatus();
    }

    void Update()
    {
        // Destroy the GameObject if Discord isn't running
        try
        {
            discord.RunCallbacks();
        }
        catch
        {
            Destroy(this);
        }
        // Update the status every now and then.
        _timeSinceLastUpdate += Time.deltaTime;
        if (_timeSinceLastUpdate > 1)
        {
            UpdateStatus();
            _timeSinceLastUpdate = 0;
        }
    }

    void UpdateStatus()
    {
        // Update Status every frame
        try
        {
            string currScene = SceneManager.GetActiveScene().name;
            string details, currState;
            if (currScene == "Title")
            {
                currState = "Can somebody send help?";
            }
            else
            {
                string mapSceneName = char.ToUpper(GameManager.GetMapScene().ToString()[0]) + GameManager.GetMapScene().ToString().Substring(1).ToLower();
                currState = GameManager.GetHeroData().characterName + " (" + GameManager.GetHeroHealth() + "/" + GameManager.GetHeroMaxHealth() + " HP) | " + mapSceneName + " Lvl. " + (GameManager.GetMapObject().currLocation.floorNumber + 1);
            }
            switch (currScene)
            {
                case "Title":
                    details = "Lost in the title screen...";
                    break;
                case "Map":
                    details = "Taking a gander at the map";
                    break;
                case "Battle":
                    details = "In a battle fighting ";
                    for (int i = 0; i < GameManager.nextBattleEnemies.Count; i++)
                    {
                        details += GameManager.nextBattleEnemies[i].characterName;
                        if (i != GameManager.nextBattleEnemies.Count - 1)
                        {
                            details += " and ";
                        }
                    }
                    break;
                case "Shop":
                    details = "Buying things at the shop";
                    break;
                case "Upgrade":
                    details = "Tinkering with upgrade machine";
                    break;
                default:
                    details = "Having a fun time";
                    break;
            }
            var activityManager = discord.GetActivityManager();
            var activity = new Discord.Activity
            {
                Details = details,
                State = currState,
                Assets =
                {
                    LargeImage = largeImage,
                    LargeText = largeText
                },
                Timestamps =
                {
                    Start = time
                }
            };

            activityManager.UpdateActivity(activity, (res) =>
            {
                if (res != Discord.Result.Ok) Debug.LogWarning("Failed connecting to Discord!");
            });
        }
        catch
        {
            // If updating the status fails, Destroy the GameObject
            Destroy(this);
        }
    }
#endif
}