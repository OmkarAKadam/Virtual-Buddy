using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MateEngine
{
    /// <summary>
    /// Manages hologram prop GameObjects that appear in front of the avatar when performing actions.
    /// This script controls the visibility and fading of keyboard and mouse hologram props.
    /// </summary>
    public class AvatarActionAnimationController : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance for easy access from other scripts
        /// </summary>
        public static AvatarActionAnimationController Instance { get; private set; }

        [Header("Hologram Props")]
        public GameObject keyboardHologram;
        public GameObject mouseHologram;

        [Header("Animation Settings")]
        public float fadeSpeed = 2f;

        private bool isTyping = false;
        private bool isUsingMouse = false;
        private CanvasGroup keyboardCanvas;
        private CanvasGroup mouseCanvas;

        private void Awake()
        {
            // Ensure only one instance exists
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            // Get or add CanvasGroup components to the hologram props
            if (keyboardHologram != null)
            {
                keyboardCanvas = keyboardHologram.GetComponent<CanvasGroup>() ?? keyboardHologram.AddComponent<CanvasGroup>();
                keyboardCanvas.alpha = 0f;
                keyboardHologram.SetActive(false);
            }

            if (mouseHologram != null)
            {
                mouseCanvas = mouseHologram.GetComponent<CanvasGroup>() ?? mouseHologram.AddComponent<CanvasGroup>();
                mouseCanvas.alpha = 0f;
                mouseHologram.SetActive(false);
            }
        }

        private void Update()
        {
            // Handle keyboard hologram fading
            if (keyboardHologram != null && keyboardCanvas != null)
            {
                if (isTyping && !keyboardHologram.activeSelf)
                {
                    keyboardHologram.SetActive(true);
                }

                if (keyboardCanvas != null)
                {
                    keyboardCanvas.alpha = Mathf.Lerp(keyboardCanvas.alpha, isTyping ? 1f : 0f, Time.deltaTime * fadeSpeed);

                    if (keyboardCanvas.alpha < 0.01f && !isTyping)
                    {
                        keyboardHologram.SetActive(false);
                    }

                    if (keyboardCanvas.alpha > 0.99f && isTyping)
                    {
                        keyboardCanvas.alpha = 1f;
                    }
                }
            }

            // Handle mouse hologram fading
            if (mouseHologram != null && mouseCanvas != null)
            {
                if (isUsingMouse && !mouseHologram.activeSelf)
                {
                    mouseHologram.SetActive(true);
                }

                if (mouseCanvas != null)
                {
                    mouseCanvas.alpha = Mathf.Lerp(mouseCanvas.alpha, isUsingMouse ? 1f : 0f, Time.deltaTime * fadeSpeed);

                    if (mouseCanvas.alpha < 0.01f && !isUsingMouse)
                    {
                        mouseHologram.SetActive(false);
                    }

                    if (mouseCanvas.alpha > 0.99f && isUsingMouse)
                    {
                        mouseCanvas.alpha = 1f;
                    }
                }
            }
        }

        /// <summary>
        /// Starts the typing animation by making the keyboard hologram visible
        /// </summary>
        public void StartTyping()
        {
            isTyping = true;
        }

        /// <summary>
        /// Stops the typing animation and hides the keyboard hologram
        /// </summary>
        public void StopTyping()
        {
            isTyping = false;
        }

        /// <summary>
        /// Starts the mouse action animation by making the mouse hologram visible
        /// </summary>
        public void StartMouseAction()
        {
            isUsingMouse = true;
        }

        /// <summary>
        /// Stops the mouse action animation and hides the mouse hologram
        /// </summary>
        public void StopMouseAction()
        {
            isUsingMouse = false;
        }

        /// <summary>
        /// Stops all animations and hides both holograms
        /// </summary>
        public void StopAll()
        {
            isTyping = false;
            isUsingMouse = false;
        }
    }
}