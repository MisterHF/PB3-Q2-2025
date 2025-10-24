using System.Collections;
using TMPro;
using UnityEngine;

namespace Script.UI
{
   public class CopyButton : MonoBehaviour
   {
      private Coroutine clickEffectCoroutine;
      private TextMeshProUGUI lobbyText;
      private string gameID;
      private void Start()
      {
         lobbyText = GetComponentInChildren<TextMeshProUGUI>();
      }

      public void SetText(string _Message)
      {
         lobbyText.text = _Message;
         gameID = _Message;
      }

      public void CopyCode()
      {
         GUIUtility.systemCopyBuffer = lobbyText.text;
         if (clickEffectCoroutine != null)
         {
            StopCoroutine(clickEffectCoroutine);
         }

         clickEffectCoroutine = StartCoroutine(ClickEffect());
      }
      
      private WaitForSeconds _wait = new (0.13f);
      private WaitForSeconds _waitToReturn = new (1f);
      private IEnumerator ClickEffect()
      {
         lobbyText.text = "Copied!"; 

         yield return _waitToReturn;
            
         lobbyText.text = "";
         for (int i = 0; i < gameID.Length; i++)
         {
            lobbyText.text += gameID[i];
            yield return _wait;
         }
      }
   }
}
