using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TermProject.Game;

namespace TermProject.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button crazyModeButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Toggle crazyModeToggle;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 0.7f;

        [Header("Scenes")]
        [SerializeField] private string arenaSceneName = "Arena_Main";

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

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

            if (titleText != null && string.IsNullOrWhiteSpace(titleText.text))
            {
                titleText.text = "FPS ARENA";
            }

            if (crazyModeToggle != null)
            {
                crazyModeToggle.isOn = GameModeSettings.HasSelection && GameModeSettings.CrazyModeEnabled;
            }
        }

        private void OnEnable()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(Play);
            }

            if (crazyModeButton != null)
            {
                crazyModeButton.onClick.AddListener(PlayCrazyMode);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(Quit);
            }
        }

        private void OnDisable()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(Play);
            }

            if (crazyModeButton != null)
            {
                crazyModeButton.onClick.RemoveListener(PlayCrazyMode);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(Quit);
            }
        }

        public void Play()
        {
            bool crazyMode = crazyModeToggle != null && crazyModeToggle.isOn;
            StartGame(crazyMode);
        }

        public void PlayNormal()
        {
            StartGame(false);
        }

        public void PlayCrazyMode()
        {
            StartGame(true);
        }

        private void StartGame(bool crazyMode)
        {
            GameModeSettings.SetCrazyMode(crazyMode);

            if (buttonClickSound != null && audioSource != null)
            {
                StartCoroutine(PlayAfterButtonSound());
                return;
            }

            LoadArena();
        }

        public void Quit()
        {
            PlayButtonClick();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator PlayAfterButtonSound()
        {
            PlayButtonClick();

            float delay = Mathf.Clamp(buttonClickSound.length, 0.05f, 0.25f);
            yield return new WaitForSecondsRealtime(delay);

            LoadArena();
        }

        private void LoadArena()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(arenaSceneName);
        }

        private void PlayButtonClick()
        {
            if (buttonClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
            }
        }
    }
}
