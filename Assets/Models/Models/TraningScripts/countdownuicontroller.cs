using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace AirCraft
{
    public class countdownuicontroller : MonoBehaviour
    {
        public TextMeshProUGUI countdownText;

        public IEnumerator StartCountdown()
        {
            // 3, 2, 1, GO!
            countdownText.text = "3";
            yield return new WaitForSeconds(1f);
            countdownText.text = string.Empty;
            yield return new WaitForSeconds(.5f);

            countdownText.text = "2";
            yield return new WaitForSeconds(1f);
            countdownText.text = string.Empty;
            yield return new WaitForSeconds(.5f);

            countdownText.text = "1";
            yield return new WaitForSeconds(1f);
            countdownText.text = string.Empty;
            yield return new WaitForSeconds(.5f);

            countdownText.text = "GO!";
            yield return new WaitForSeconds(1f);
            countdownText.text = string.Empty;
        }
    }
}