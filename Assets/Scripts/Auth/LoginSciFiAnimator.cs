using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DontDiePlease.Auth
{
    public class LoginSciFiAnimator : MonoBehaviour
    {
        private RectTransform scanLine;
        private RectTransform[] rotatingTargets;
        private Graphic[] pulseTargets;
        private Color[] baseColors;

        public void SetTargets(RectTransform scanLineTarget, RectTransform[] rotatingTargets, Graphic[] pulseTargets)
        {
            scanLine = scanLineTarget;
            this.rotatingTargets = rotatingTargets;
            this.pulseTargets = pulseTargets;
            baseColors = new Color[pulseTargets != null ? pulseTargets.Length : 0];

            for (var i = 0; i < baseColors.Length; i++)
            {
                baseColors[i] = pulseTargets[i] != null ? pulseTargets[i].color : Color.white;
            }
        }

        private void Update()
        {
            var time = Time.unscaledTime;

            if (scanLine != null)
            {
                var y = Mathf.Lerp(-210f, 210f, Mathf.PingPong(time * 0.22f, 1f));
                scanLine.anchoredPosition = new Vector2(scanLine.anchoredPosition.x, y);
            }

            if (rotatingTargets != null)
            {
                for (var i = 0; i < rotatingTargets.Length; i++)
                {
                    if (rotatingTargets[i] != null)
                    {
                        rotatingTargets[i].Rotate(0f, 0f, (i % 2 == 0 ? 6f : -8f) * Time.unscaledDeltaTime);
                    }
                }
            }

            if (pulseTargets == null || baseColors == null)
            {
                return;
            }

            var pulse = 0.86f + Mathf.Sin(time * 2.1f) * 0.12f;
            for (var i = 0; i < pulseTargets.Length; i++)
            {
                if (pulseTargets[i] == null)
                {
                    continue;
                }

                var color = baseColors[i];
                color.a = Mathf.Clamp01(baseColors[i].a * pulse);
                pulseTargets[i].color = color;
            }
        }
    }

    public class LoginButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform rectTransform;
        private Graphic graphic;
        private Vector3 baseScale;
        private Color baseColor;
        private Color hoverColor;
        private float targetScale = 1f;

        public void Configure(Color hoverColor)
        {
            rectTransform = GetComponent<RectTransform>();
            graphic = GetComponent<Graphic>();
            baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
            baseColor = graphic != null ? graphic.color : Color.white;
            this.hoverColor = hoverColor;
        }

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (graphic == null)
            {
                graphic = GetComponent<Graphic>();
            }

            baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
            baseColor = graphic != null ? graphic.color : Color.white;
            hoverColor = baseColor;
        }

        private void Update()
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, baseScale * targetScale, Time.unscaledDeltaTime * 14f);
            }

            if (graphic != null)
            {
                var targetColor = targetScale > 1f ? hoverColor : baseColor;
                graphic.color = Color.Lerp(graphic.color, targetColor, Time.unscaledDeltaTime * 12f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = 1.035f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = 1f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = 0.985f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = 1.035f;
        }
    }
}
