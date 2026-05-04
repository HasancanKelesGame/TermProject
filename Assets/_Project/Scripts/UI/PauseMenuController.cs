using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TermProject.Game;

namespace TermProject.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 0.7f;

        private GameManager gameManager;

        private void Awake()
        {
            if (panel == null)
            {
                panel = gameObject;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            Hide();
        }

        private void OnEnable()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(Resume);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(Restart);
            }
        }

        private void OnDisable()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(Resume);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
            }
        }

        public void SetGameManager(GameManager manager)
        {
            gameManager = manager;
        }

        public void Show()
        {
            EnsurePanel();

            if (titleText != null)
            {
                titleText.text = "PAUSED";
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        public void Hide()
        {
            EnsurePanel();

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void Resume()
        {
            PlayButtonClick();
            GetGameManager()?.ResumeGame();
        }

        public void Restart()
        {
            if (buttonClickSound != null && audioSource != null)
            {
                StartCoroutine(RestartAfterButtonSound());
                return;
            }

            GetGameManager()?.RestartCurrentScene();
        }

        private IEnumerator RestartAfterButtonSound()
        {
            PlayButtonClick();

            float delay = Mathf.Clamp(buttonClickSound.length, 0.05f, 0.25f);
            yield return new WaitForSecondsRealtime(delay);

            GetGameManager()?.RestartCurrentScene();
        }

        private void PlayButtonClick()
        {
            if (buttonClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
            }
        }

        private GameManager GetGameManager()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            return gameManager;
        }

        private void EnsurePanel()
        {
            if (panel == null)
            {
                panel = gameObject;
            }
        }
    }
}
