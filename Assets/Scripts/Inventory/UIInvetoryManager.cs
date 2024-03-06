using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyUI.ModernMenu
{
    public class UIInvetoryManager : MonoBehaviour
    {
        [Header("MENUS")]
        public GameObject mainMenu;
        public GameObject subMenu;

        //[Header("PANELS")]
        //public GameObject playCanvas;
        //public GameObject settingsCanvas;
        //public GameObject exitCanvas;

        [Header("OTHERS")]
        public Texture defaultIcon;


        private bool isActive = false;
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyBind.keys.inventoryKey))
            {
                isActive = !isActive;
                mainMenu.SetActive(isActive);

                if (isActive)
                {
                    CameraController.MouseUnlocked();
                    Time.timeScale = .5f;
                }
                else
                {
                    CameraController.MouseLocked();
                    Time.timeScale = 1;
                }
            }

            if (UIPauseMenuManager.isPaused)
            {
                mainMenu.SetActive(false);
                subMenu.SetActive(false);
            }
            else
            {
                subMenu.SetActive(true);
            }
        }
    }
}
