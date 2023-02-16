using UnityEngine;

namespace EasyCharacterMovement.CharacterMovementDemo
{
    public class DemoController : MonoBehaviour
    {
        public GameObject firstPerson;
        public GameObject thirdPerson;

        private bool _firstPersonMode;

        public void EnableFirstPerson()
        {
            thirdPerson.SetActive(false);
            firstPerson.SetActive(true);
        }

        public void EnableThirdPerson()
        {
            firstPerson.SetActive(false);
            thirdPerson.SetActive(true);
        }

        public void ExitDemo()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
            string webplayerQuitURL = "http://google.com";
            Application.OpenURL(webplayerQuitURL);
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Tab))
                return;

            _firstPersonMode = !_firstPersonMode;

            if (_firstPersonMode)
                EnableFirstPerson();
            else
                EnableThirdPerson();


        }
    }
}
