using DG.Tweening;
using TMPro;
using UnityEngine;

namespace View
{
    public class ShuffleWidget : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        
        private Transform _transform;
        
        private void Awake()
        {
            _transform = transform;
            _transform.localScale = Vector3.one * 0.5f;
        }

        private void Start()
        {
            ShowAnimation();
        }

        private void ShowAnimation()
        {
            transform.DOMove(Vector3.zero, 0.2f).OnComplete(HideAnimation);
            var scaleSequence = DOTween.Sequence();
            scaleSequence.Append(transform.DOScale(Vector3.one, 0.1f));
            scaleSequence.Append(transform.DOScale(Vector3.one, 0.05f));
            text.DOFade(1, 0.2f);
        }

        private void HideAnimation()
        {
            text.DOFade(0, 0.4f);
            var fadeSequence = DOTween.Sequence();
            fadeSequence.AppendInterval(0.2f);
            fadeSequence.Append(transform.DOMove(Vector3.up * 3, 0.2f));
            fadeSequence.OnComplete(KillSelf);
        }

        private void KillSelf()
        {
            Destroy(gameObject);
        }
    }
}