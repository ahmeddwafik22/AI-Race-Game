using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace AirCraft
{
    public class mainmenu : MonoBehaviour
    {

        public List<string> levels;

        public TMP_Dropdown level_Dropdown;

        public TMP_Dropdown difficulty_Dropdown;

        private string selectedlevel;
        private GameDifficulty selecteddifficulty;


        private void Start()
        {
            level_Dropdown.ClearOptions();
            level_Dropdown.AddOptions(levels);
           
            selectedlevel = levels[0];

            difficulty_Dropdown.ClearOptions();
            difficulty_Dropdown.AddOptions(Enum.GetNames(typeof(GameDifficulty)).ToList());
            selecteddifficulty = GameDifficulty.Normal;

        }


        public void SetLevel(int levelIndex)
        {
            selectedlevel = levels[levelIndex];
        }

        public void SetDifficulty(int difficultyIndex)
        {
            selecteddifficulty = (GameDifficulty)difficultyIndex;
        }

        public void StartButtonClicked()
        {
            // Set game difficulty
            GameManager.Instance.GameDifficulty = selecteddifficulty;

            // Load the level in 'Preparing' mode
            GameManager.Instance.LoadLevel(selectedlevel, GameState.Preparing);
        }

        
        public void QuitButtonClicked()
        {
            Application.Quit();
        }


    }
}