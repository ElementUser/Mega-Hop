using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.HighScoreSystem;
using EventSystem;
using UnityEngine;
using Zenject;

public class HighScoreStorage
{
	[Inject] private GameEvents gameEvents;
	public List<HighscoreUser> highScoreUsers = new List<HighscoreUser>();

	/// <summary>
	/// Sorts highScoreUsers in-place (memory optimization) in descending order, based on the difficulty received
	/// </summary>
	/// <param name="difficulty"></param>
	public void UpdateHighscoreDisplayOrder(int difficulty)
	{
		switch (difficulty)
		{
			case 0: // Easy
				highScoreUsers.Sort((x, y) => y.EasyScore.CompareTo(x.EasyScore));
				break;

			case 1: // Medium
				highScoreUsers.Sort((x, y) => y.MediumScore.CompareTo(x.MediumScore));
				break;

			case 2: // Hard
				highScoreUsers.Sort((x, y) => y.HardScore.CompareTo(x.HardScore));
				break;

			default: // Easy
				highScoreUsers.Sort((x, y) => y.EasyScore.CompareTo(x.EasyScore));
				break;
		}
	}
}
