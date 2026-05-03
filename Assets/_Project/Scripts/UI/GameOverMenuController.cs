using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TermProject.Game;

namespace TermProject.UI
{
    [DisallowMultipleComponent]
    public sealed class GameOverMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text finalStatsText;
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

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            Hide();
        }

        private void OnEnable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(Restart);
            }
        }

        private void OnDisable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
            }
        }

        public void SetGameManager(GameManager manager)
        {
            gameManager = manager;
        }

        public void Show(int wave, int kills, int score)
        {
            EnsurePanel();

            if (titleText != null)
            {
                titleText.text = "GAME OVER";
            }

            if (finalStatsText != null)
            {
                finalStatsText.text = $"WAVE {wave}\nKILLS {kills}\nSCORE {score}";
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

        public void Restart()
        {
            if (buttonClickSound != null && audioSource != null)
            {
                StartCoroutine(RestartAfterButtonSound());
                return;
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            gameManager?.RestartCurrentScene();
        }

        private IEnumerator RestartAfterButtonSound()
        {
            audioSource.PlayOneShot(buttonClickSound, buttonClickVolume);

            float delay = Mathf.Clamp(buttonClickSound.length, 0.05f, 0.25f);
            yield return new WaitForSecondsRealtime(delay);

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            gameManager?.RestartCurrentScene();
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
