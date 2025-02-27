using TMPro;
using UnityEngine;

namespace Game.Player
{
    public class FlagBearer : MonoBehaviour
    {
        public TMP_Text playerNameText;
        
        public void Refresh(PlayerFlagBearerData data)
        {
            playerNameText.text = data.FlagName;
        }
    }
}
