using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Utils
{
    [RequireComponent(typeof(Image))]
    public class FadeScenes : MonoBehaviour
    {
        public static FadeScenes Instance { get; private set; } // Singleton

        private Image _fadeImage;
        private int time = 1;

        private void Awake()
        {
            // Singleton implementation
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            _fadeImage = GetComponent<Image>();
        }

        private void Start()
        {
            _fadeImage.CrossFadeAlpha(0, time, true);
        }

        public async Task PlayFade() // Async method
        {
            _fadeImage.raycastTarget = true;
            _fadeImage.CrossFadeAlpha(1.1f, time, true);
            await Task.Delay(time * 1000); // await added, time is in milliseconds
            _fadeImage.CrossFadeAlpha(-0.1f, time, true);
            await Task.Delay(time * 1000); // await added, time is in milliseconds
            Deactivate();
        }

        public void Deactivate()
        {
            _fadeImage.raycastTarget = false;
        }
    }
}