using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSystem;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Zenject;

public class Leaderboard_UIManager : MonoBehaviour
{
	private InterfaceManager interfaceManager;

	[Inject] private GameEvents gameEvents;
	[Inject] private HighScoreStorage highScoreStorage;

	[SerializeField] private GameObject leaderboardRowTemplate;
	[SerializeField] private GameObject difficultyDisplay;

	[SerializeField] private Sprite rank1Sprite;
	[SerializeField] private Sprite rank2Sprite;
	[SerializeField] private Sprite rank3Sprite;

	//Newer elements
	[SerializeField] private Image profileRankPic;
	[SerializeField] private TextMeshProUGUI profileRankTxt;
	[SerializeField] private Sprite defaultProfilePic;

	private ManagerRoot managerRoot;
	private int pageIndex;

	private CanvasGroup canv;

	/// <summary>
	///     Holds the instances of the generated leaderboard UI elements so that they can be deleted later in the program
	/// </summary>
	public List<GameObject> leaderboardRowsList = new List<GameObject>();

	public const int numberOfRows = 100;
	public int currentDifficultySetting; // Default: 0 = easy

	private int round; // round++ when leaderboards are refreshed


	private void Awake()
	{
		interfaceManager = GameObject.FindWithTag("InterfaceTag").GetComponent<InterfaceManager>();
		managerRoot = GameObject.FindWithTag("ManagerRootTag").GetComponent<ManagerRoot>();
	}

	private void OnEnable()
	{
		gameEvents.SetDifficultySetting += SetActiveDifficulty;
	}

	private void OnDisable()
	{
		gameEvents.SetDifficultySetting -= SetActiveDifficulty;
	}

	/// <summary>
	///     Sets the active difficulty & loads the appropriate high score dictionary from the storage
	/// </summary>
	/// <param name="difficulty"></param>
	private void SetActiveDifficulty(int difficulty)
	{
		currentDifficultySetting = difficulty;
		interfaceManager.mainMenuInterface.SetFirebaseDifficulty(difficulty);
		highScoreStorage.UpdateHighscoreDisplayOrder(difficulty);
	}

	/// <summary>
	///     Populates the leaderboard row UI elements by dynamically generating them & resizing the scroll rect area
	///     appropriately
	/// </summary>
	/// <param name="highScoreDictionary"></param>
	public void PopulateLeaderboardRows(int pageIndex = 0)
	{
		round++;
		
		// Clear leaderboard UI rows before re-populating them
		ClearLeaderboardRows();

		// Instantiate each leaderboardRow template, reposition them properly & populate their text fields based on the page index & number of rows
		for (var iii = pageIndex * numberOfRows; iii < numberOfRows * (pageIndex + 1); ++iii)
		{
			var leaderboardRowObj = Instantiate(leaderboardRowTemplate,
				leaderboardRowTemplate.GetComponent<Transform>().parent);
			leaderboardRowsList.Add(leaderboardRowObj);

			// Set each of the LeaderboardRowUI's fields to the corresponding one in the highScoreStorage
			var leaderboardRowContents = leaderboardRowObj.GetComponent<LeaderboardRowUI>();
			leaderboardRowContents.rankText.text = (iii + 1).ToString();

			// Prevents null reference errors via out of dictionary size bounds
			if (iii < highScoreStorage.highScoreUsers.Count)
			{
				leaderboardRowContents.userText.text = highScoreStorage.highScoreUsers.ElementAt(iii).DisplayName;
				switch (currentDifficultySetting)
				{
					case 0: // Easy
						leaderboardRowContents.scoreText.text =
							highScoreStorage.highScoreUsers.ElementAt(iii).EasyScore.ToString();
						break;

					case 1: // Medium
						leaderboardRowContents.scoreText.text =
							highScoreStorage.highScoreUsers.ElementAt(iii).MediumScore.ToString();
						break;

					case 2: // Hard
						leaderboardRowContents.scoreText.text =
							highScoreStorage.highScoreUsers.ElementAt(iii).HardScore.ToString();
						break;

					default: // Easy
						leaderboardRowContents.scoreText.text =
							highScoreStorage.highScoreUsers.ElementAt(iii).EasyScore.ToString();
						break;
				}

				// Set Facebook Profile picture
				var facebookPictureURL = highScoreStorage.highScoreUsers.ElementAt(iii).PictureURL;
				PopulateDisplayPicture(facebookPictureURL, iii, round);
			}
			else
			{
				leaderboardRowContents.userText.text = "-";
				leaderboardRowContents.scoreText.text = "-";
			}

			// Set the special Rank graphics for 1, 2 and 3
			if (pageIndex == 0 && iii <= 2)
			{
				leaderboardRowContents.rankText.text = ""; // Hide rank text
				var tempGameObj = new GameObject();
				var rankSpriteObj = Instantiate(tempGameObj, leaderboardRowContents.rankText.gameObject.transform);
				Destroy(tempGameObj); // Clean up empty GameObject that was created
				var rankSprite = rankSpriteObj.AddComponent<Image>();

				switch (iii)
				{
					case 0:
						rankSprite.sprite = rank1Sprite;
						rankSpriteObj.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
						break;

					case 1:
						rankSprite.sprite = rank2Sprite;
						rankSpriteObj.transform.localScale = new Vector3(2.6f, 2.6f, 2.6f);
						break;

					case 2:
						rankSprite.sprite = rank3Sprite;
						rankSpriteObj.transform.localScale = new Vector3(2.8f, 2.8f, 2.8f);
						break;
				}
			}

			leaderboardRowObj.SetActive(true);
			UpdateDifficultyDisplay();
		}

		SetUserRank();
	}

	/// <summary>
	///     Async task to grab a display picture via URL
	/// </summary>
	/// <param name="url"></param>
	/// <returns></returns>
	public static async Task<Texture2D> GetDisplayPictureFromURL(string url)
	{
		using (var www = UnityWebRequestTexture.GetTexture(url))
		{
			// Begin request:
			var asyncOp = www.SendWebRequest();

			// Await until it's done: 
			while (asyncOp.isDone == false) await Task.Delay(1000 / 30); //30 hertz

			// Read results:
			if (www.isNetworkError || www.isHttpError)
//#if DEBUG
//				Debug.Log($"{ www.error }, URL:{ www.url }");
//#endif

//				//nothing to return on error:
				return null;
			return DownloadHandlerTexture.GetContent(www);
		}
	}

	/// <summary>
	///     Async task to populate a user's display picture on the leaderboard
	/// </summary>
	/// <param name="imageURL"></param>
	/// <param name="leaderboardListIndex"></param>
	private async void PopulateDisplayPicture(string imageURL, int leaderboardListIndex, int currentRound)
	{
		var result = await GetDisplayPictureFromURL(imageURL);
		var displayPicture = leaderboardRowsList[leaderboardListIndex].GetComponent<LeaderboardRowUI>().displayPicture;

		if (round != currentRound) return;

		if (result != null)
		{
			var rect = new Rect(0, 0, result.width, result.height);
			var sprite = Sprite.Create(result, rect, Vector2.zero);
			displayPicture.sprite = sprite;
			displayPicture.color = Color.white;
		}
		else
		{
			displayPicture.color = Color.clear;
		}
	}

	/// <summary>
	///     Function that updates the text and appearance of the difficulty UI element
	/// </summary>
	private void UpdateDifficultyDisplay()
	{
		var difficultyDisplayText = difficultyDisplay.GetComponentInChildren<TextMeshProUGUI>();

		switch (currentDifficultySetting)
		{
			case 0: // Easy
				difficultyDisplayText.text = "Easy";
				break;
			case 1: // Medium
				difficultyDisplayText.text = "Medium";
				break;
			case 2: // Hard
				difficultyDisplayText.text = "Hard";
				break;
			default:
				difficultyDisplayText.text = "Easy";
				break;
		}
	}

	/// <summary>
	///     Function that clears each element in the leaderboard rows list
	/// </summary>
	public void ClearLeaderboardRows()
	{
		foreach (var item in leaderboardRowsList) Destroy(item);

		// Reinstantiate the list to prevent a missing reference exception
		leaderboardRowsList = new List<GameObject>();
	}

	/// <summary>
	/// Obtains the high scores based on the current difficulty when the difficulty in the leaderboard is selected
	/// </summary>
	/// <param name="difficulty"></param>
	public void GetHighScores(int difficulty)
	{
		currentDifficultySetting = difficulty;
		pageIndex = 0;
		gameEvents.SetDifficultySetting.Invoke(currentDifficultySetting); // Re-update the active display dictionary with the current data in the highScoreUsers list by invoking the difficulty settings event
		PopulateLeaderboardRows();
	}

	/// <summary>
	/// Async task to set the display picture of each user
	/// </summary>
	/// <param name="imageURL"></param>
	public async void SetDisplayPicture(string imageURL)
	{
		if (imageURL == "")
		{
			profileRankPic.sprite = defaultProfilePic;
			return;
		}

		var texture = await GetDisplayPictureFromURL(imageURL);
		profileRankPic.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
		;
	}

	/// <summary>
	/// Sets the text of the current user's rank in the leaderboard
	/// </summary>
	private void SetUserRank()
	{
		if (FirebaseAuth.DefaultInstance.CurrentUser == null)
		{
			profileRankTxt.text = "Login to compare your highscore.";
			return;
		}

		var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
		for (var i = 0; i < highScoreStorage.highScoreUsers.Count; i++)
		{ 
			if (highScoreStorage.highScoreUsers.ElementAt(i).Id == userId)
			{
				profileRankTxt.text = "Your Rank: #" + (i + 1);
				break;
			}
		}
	}
}