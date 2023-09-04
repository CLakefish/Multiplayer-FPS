using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Button : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    private Image image;
    private TMPro.TMP_Text buttonText;

    [Header("Selection Parameters")]
    [SerializeField] private float smoothTime;
    [SerializeField] private Color selectedColor, deselectColor;
    [SerializeField] private Color selectedTextColor, deselectTextColor;
    private Vector3 originalScale;
    private Vector3 sizeVelocity;
    private Coroutine Scale;

    private void Start()
    {
        image = GetComponent<Image>();
        image.color = deselectColor;
        originalScale = image.rectTransform.localScale;

        buttonText = GetComponentInChildren<TMPro.TMP_Text>();
        buttonText.color = deselectTextColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Scale != null) StopCoroutine(Scale);
        Scale = StartCoroutine(SmoothScale(originalScale * 1.15f));
        image.color = selectedColor;
        buttonText.color = selectedTextColor;
        Debug.Log(2);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Scale != null) StopCoroutine(Scale);
        Scale = StartCoroutine(SmoothScale(originalScale));
        image.color = deselectColor;
        buttonText.color = deselectTextColor;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (Scale != null) StopCoroutine(Scale);
        transform.localScale = originalScale;
        Scale = StartCoroutine(SmoothScale(originalScale * 1.15f));
    }

    private IEnumerator SmoothScale(Vector3 newSize)
    {
        while (transform.localScale != newSize)
        {
            transform.localScale = Vector3.SmoothDamp(transform.localScale, newSize, ref sizeVelocity, smoothTime);

            yield return new WaitForEndOfFrame();
        }

        transform.localScale = newSize;
    }
}
