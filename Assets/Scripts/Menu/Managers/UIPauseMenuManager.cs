using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyUI.ModernMenu
{
    public class UIPauseMenuManager : MonoBehaviour
    {
        [Header("MENUS")]
        public GameObject pauseMenu;
        public GameObject loadingMenu;

        [Header("PANELS")]
        public GameObject playCanvas;
        public GameObject settingsCanvas;
        public GameObject exitCanvas;

        [Header("LOADING SCREEN")]
        public Slider loadingBar;
        

        public static bool isPaused = false;
        private bool exitGame = true;
        private void Start()
        {
            playCanvas.SetActive(false);
            settingsCanvas.SetActive(false);
            exitCanvas.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyBind.keys.pauseKey))
            {
                isPaused = !isPaused;
                playCanvas.SetActive(isPaused);
                settingsCanvas.SetActive(isPaused);

                if (isPaused)
                {
                    Time.timeScale = 0;
                    CameraController.MouseUnlocked();
                }
                else
                {
                    Time.timeScale = 1;
                    CameraController.MouseLocked();
                }
            }
        }

        public void Resume()
        {
            ResetVariable();
            CameraController.MouseLocked();

            playCanvas.SetActive(isPaused);
            settingsCanvas.SetActive(isPaused);
        }

        public void AreYouSure(bool exitGame)
        {
            playCanvas.SetActive(false);
            settingsCanvas.SetActive(false);
            exitCanvas.SetActive(true);

            this.exitGame = exitGame;
        }

        public void Exit()
        {
            if (this.exitGame)
            {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
			        Application.Quit();
                #endif
            }
            else
            {
                ResetVariable();
                CameraController.MouseUnlocked();

                StartCoroutine(LoadAsynchronously("Menu"));
            }
        }
        public void ReturnMenu()
        {
            playCanvas.SetActive(true);
            settingsCanvas.SetActive(true);
            exitCanvas.SetActive(false);
            isPaused = false;
        }
        IEnumerator LoadAsynchronously(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;
            exitCanvas.SetActive(false);
            loadingMenu.SetActive(true);

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / .95f);
                loadingBar.value = progress;

                if (operation.progress >= 0.9f)
                {
                    loadingBar.value = 1;
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }
        }
        private void ResetVariable()
        {
            isPaused = false;
            Time.timeScale = 1;
        }
    }
}